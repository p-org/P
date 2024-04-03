// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using PChecker.Actors;
using PChecker.IO.Debugging;
using PChecker.Runtime;
using PChecker.Tasks;

namespace PChecker.SystematicTesting
{
    /// <summary>
    /// Maintains information about a method to be tested.
    /// </summary>
    internal sealed class TestMethodInfo
    {
        /// <summary>
        /// The assembly that contains the test method.
        /// </summary>
        internal readonly Assembly Assembly;

        /// <summary>
        /// The method to be tested.
        /// </summary>
        internal readonly Delegate Method;

        /// <summary>
        /// The name of the test method.
        /// </summary>
        internal readonly string Name;

        /// <summary>
        /// The test initialization method.
        /// </summary>
        private readonly MethodInfo InitMethod;

        /// <summary>
        /// The test dispose method.
        /// </summary>
        private readonly MethodInfo DisposeMethod;

        /// <summary>
        /// The test dispose method per schedule.
        /// </summary>
        private readonly MethodInfo IterationDisposeMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestMethodInfo"/> class.
        /// </summary>
        internal TestMethodInfo(Delegate method)
        {
            Method = method;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestMethodInfo"/> class.
        /// </summary>
        private TestMethodInfo(Assembly assembly, Delegate method, string name, MethodInfo initMethod,
            MethodInfo disposeMethod, MethodInfo iterationDisposeMethod)
        {
            Assembly = assembly;
            Method = method;
            Name = name;
            InitMethod = initMethod;
            DisposeMethod = disposeMethod;
            IterationDisposeMethod = iterationDisposeMethod;
        }

        /// <summary>
        /// Invokes the user-specified initialization method for all schedules executing this test.
        /// </summary>
        internal void InitializeAllIterations() => InitMethod?.Invoke(null, Array.Empty<object>());

        /// <summary>
        /// Invokes the user-specified disposal method for the schedule currently executing this test.
        /// </summary>
        internal void DisposeCurrentIteration() => IterationDisposeMethod?.Invoke(null, null);

        /// <summary>
        /// Invokes the user-specified disposal method for all schedules executing this test.
        /// </summary>
        internal void DisposeAllIterations() => DisposeMethod?.Invoke(null, Array.Empty<object>());

        /// <summary>
        /// Returns the list of <see cref="MethodInfo"/> containing all test methods in the specified assembly.
        /// </summary>
        internal static List<MethodInfo> GetAllTestMethodsFromAssembly(Assembly assembly)
        {
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            return FindTestMethodsWithAttribute(typeof(TestAttribute), flags, assembly);
        }

        /// <summary>
        /// Returns the <see cref="TestMethodInfo"/> with the given name in the specified assembly.
        /// </summary>
        internal static TestMethodInfo GetFromAssembly(Assembly assembly, string methodName)
        {
            var (testMethod, testName) = GetTestMethod(assembly, methodName);
            var initMethod = GetTestSetupMethod(assembly, typeof(TestInitAttribute));
            var disposeMethod = GetTestSetupMethod(assembly, typeof(TestDisposeAttribute));
            var iterationDisposeMethod = GetTestSetupMethod(assembly, typeof(TestIterationDisposeAttribute));

            return new TestMethodInfo(assembly, testMethod, testName, initMethod, disposeMethod, iterationDisposeMethod);
        }

        /// <summary>
        /// Returns the test method with the specified name. A test method must
        /// be annotated with the <see cref="TestAttribute"/> attribute.
        /// </summary>
        private static (Delegate testMethod, string testName) GetTestMethod(Assembly assembly, string methodName)
        {
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            var testMethods = FindTestMethodsWithAttribute(typeof(TestAttribute), flags, assembly);

            // Filter by test method name
            // find the test case with exact match
            var findExactMatch = testMethods.FindAll(mi => mi.DeclaringType.Name == (methodName));
            var filteredTestMethods =
                findExactMatch.Any()? findExactMatch: testMethods.FindAll(mi => mi.DeclaringType.Name.Contains(methodName));


            if (filteredTestMethods.Count == 0)
            {
                if (testMethods.Count > 0)
                {
                    var msg = "Cannot find a P test case that contains " + methodName +
                              ". Possible options are: " + Environment.NewLine;
                    foreach (var mi in testMethods)
                    {
                        msg += $"{mi.DeclaringType.Name}{Environment.NewLine}";
                    }

                    Error.ReportAndExit(msg);
                }
                else
                {
                    Error.ReportAndExit("Cannot detect a P test case to check.\n -- If you have a test case specified and P checker is not finding it, please report this error to the P team --");
                }
            }
            else if (filteredTestMethods.Count > 1)
            {
                var msg = $"We found '{testMethods.Count}' test cases. Please provide " +
                          $"a more precise name of the test case you wish to check using (--testcase | -tc).\n" +
                          "Possible options are: " + Environment.NewLine;

                foreach (var mi in testMethods)
                {
                    msg += $"{mi.DeclaringType.Name}{Environment.NewLine}";
                }

                Error.ReportAndExit(msg);
            }

            var testMethod = filteredTestMethods[0];
            var testParams = testMethod.GetParameters();

            var hasVoidReturnType = testMethod.ReturnType == typeof(void) &&
                                    testMethod.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) == null;
            var hasAsyncReturnType = testMethod.ReturnType == typeof(Task) &&
                                     testMethod.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null;

            var hasNoInputParameters = testParams.Length is 0;
            var hasActorInputParameters = testParams.Length is 1 && testParams[0].ParameterType == typeof(IActorRuntime);
            var hasTaskInputParameters = testParams.Length is 1 && testParams[0].ParameterType == typeof(ICoyoteRuntime);

            if (!((hasVoidReturnType || hasAsyncReturnType) && (hasNoInputParameters || hasActorInputParameters || hasTaskInputParameters) &&
                  !testMethod.IsAbstract && !testMethod.IsVirtual && !testMethod.IsConstructor &&
                  !testMethod.ContainsGenericParameters && testMethod.IsPublic && testMethod.IsStatic))
            {
                Error.ReportAndExit("Incorrect test method declaration. Please " +
                                    "use one of the following supported declarations:\n\n" +
                                    $"  [{typeof(TestAttribute).FullName}]\n" +
                                    $"  public static void {testMethod.Name}() {{ ... }}\n\n" +
                                    $"  [{typeof(TestAttribute).FullName}]\n" +
                                    $"  public static void {testMethod.Name}(ICoyoteRuntime runtime) {{ ... }}\n\n" +
                                    $"  [{typeof(TestAttribute).FullName}]\n" +
                                    $"  public static void {testMethod.Name}(IActorRuntime runtime) {{ ... }}\n\n" +
                                    $"  [{typeof(TestAttribute).FullName}]\n" +
                                    $"  public static async {typeof(Task).FullName} {testMethod.Name}() {{ ... await ... }}\n\n" +
                                    $"  [{typeof(TestAttribute).FullName}]\n" +
                                    $"  public static async {typeof(Task).FullName} {testMethod.Name}(ICoyoteRuntime runtime) {{ ... await ... }}\n\n" +
                                    $"  [{typeof(TestAttribute).FullName}]\n" +
                                    $"  public static async {typeof(Task).FullName} {testMethod.Name}(IActorRuntime runtime) {{ ... await ... }}");
            }

            Delegate test;
            if (hasAsyncReturnType)
            {
                if (hasActorInputParameters)
                {
                    test = Delegate.CreateDelegate(typeof(Func<IActorRuntime, Task>), testMethod);
                }
                else if (hasTaskInputParameters)
                {
                    test = Delegate.CreateDelegate(typeof(Func<ICoyoteRuntime, Task>), testMethod);
                }
                else
                {
                    test = Delegate.CreateDelegate(typeof(Func<Task>), testMethod);
                }
            }
            else
            {
                if (hasActorInputParameters)
                {
                    test = Delegate.CreateDelegate(typeof(Action<IActorRuntime>), testMethod);
                }
                else if (hasTaskInputParameters)
                {
                    test = Delegate.CreateDelegate(typeof(Action<ICoyoteRuntime>), testMethod);
                }
                else
                {
                    test = Delegate.CreateDelegate(typeof(Action), testMethod);
                }
            }

            return (test, $"{testMethod.DeclaringType.Name}");
        }

        /// <summary>
        /// Returns the test method with the specified attribute.
        /// Returns null if no such method is found.
        /// </summary>
        private static MethodInfo GetTestSetupMethod(Assembly assembly, Type attribute)
        {
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            var testMethods = FindTestMethodsWithAttribute(attribute, flags, assembly);

            if (testMethods.Count == 0)
            {
                return null;
            }
            else if (testMethods.Count > 1)
            {
                Error.ReportAndExit("Only one test case in the program can " +
                                    $"be declared with the attribute '{attribute.FullName}'. " +
                                    $"'{testMethods.Count}' test methods were found instead.");
            }

            if (testMethods[0].ReturnType != typeof(void) ||
                testMethods[0].ContainsGenericParameters ||
                testMethods[0].IsAbstract || testMethods[0].IsVirtual ||
                testMethods[0].IsConstructor ||
                !testMethods[0].IsPublic || !testMethods[0].IsStatic ||
                testMethods[0].GetParameters().Length != 0)
            {
                Error.ReportAndExit("Incorrect test method declaration. Please " +
                                    "declare the test method as follows:\n" +
                                    $"  [{attribute.FullName}] public static void " +
                                    $"{testMethods[0].Name}() {{ ... }}");
            }

            return testMethods[0];
        }

        /// <summary>
        /// Finds the test methods with the specified attribute in the given assembly.
        /// Returns an empty list if no such methods are found.
        /// </summary>
        private static List<MethodInfo> FindTestMethodsWithAttribute(Type attribute, BindingFlags bindingFlags, Assembly assembly)
        {
            List<MethodInfo> testMethods = null;

            try
            {
                testMethods = assembly.GetTypes().SelectMany(t => t.GetMethods(bindingFlags)).
                    Where(m => m.GetCustomAttributes(attribute, false).Length > 0).ToList();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var le in ex.LoaderExceptions)
                {
                    Debug.WriteLine(le.Message);
                }

                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }

            return testMethods;
        }
    }
}