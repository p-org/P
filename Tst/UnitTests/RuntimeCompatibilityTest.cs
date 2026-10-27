using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using PChecker;
using PChecker.Runtime.Events;
using PChecker.Runtime.StateMachines;
using PChecker.Runtime.Values;
using PChecker.SystematicTesting;
using UnitTests.Core;
using UnitTests.Runners;
using TestAttribute = NUnit.Framework.TestAttribute;

namespace UnitTests
{
    /// <summary>
    /// Code compiled against another version of the P runtime must be reported as a tool error that
    /// asks to recompile, never as a bug in the program under test (or silently run with different
    /// semantics). Also covers the compatibility shims kept for foreign and C# test code.
    /// </summary>
    [TestFixture]
    public class RuntimeCompatibilityTest
    {
        private static int pingsReceived;
        private static ControlledRuntime currentRuntime;

        private class Ping : Event
        {
        }

        private class Receiver : StateMachine
        {
            public Receiver()
            {
                receives.Add(nameof(Ping));
            }

            private void OnPing()
            {
                pingsReceived++;
            }

            [Start]
            [OnEventDoAction(typeof(Ping), nameof(OnPing))]
            private class Init : State
            {
            }
        }

        /// <summary>
        /// Sends a ping to the machine given by the payload of its initial event, through the
        /// obsolete ControlledRuntime.SendEvent, like C# code written for an older runtime.
        /// </summary>
        private class LegacySender : StateMachine
        {
            private void Entry(Event e)
            {
#pragma warning disable CS0618
                currentRuntime.SendEvent(((PMachineValue)e.Payload).Id, new Ping());
#pragma warning restore CS0618
            }

            [Start]
            [OnEntry(nameof(Entry))]
            private class Init : State
            {
            }
        }

        private static TestReport Run(Action<ControlledRuntime> test, uint iterations, out TestingEngine engine)
        {
            var configuration = CheckerConfiguration.Create()
                .WithTestingIterations(iterations)
                .WithMaxSchedulingSteps(1000)
                .WithRandomGeneratorSeed(1)
                .WithRandomStrategy();
            engine = TestingEngine.Create(configuration, test);
            engine.Run();
            return engine.TestReport;
        }

        [Test]
        public void BindingFailureInForeignCodeIsAnInternalError()
        {
            var testDir = new DirectoryInfo(Path.Combine(Constants.TestDirectory, "RegressionTests", "Integration",
                "InternalError", "BindingFailure"));
            var scratchDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory,
                nameof(BindingFailureInForeignCodeIsAnInternalError)));

            var runner = new PCheckerRunner(testDir.GetFiles("*.p"), testDir.GetFiles("*.cs"));
            var exitCode = runner.RunTest(scratchDir, out var stdout, out var stderr);

            Assert.That(exitCode, Is.EqualTo(2), $"expected an internal error\nstdout: {stdout}\nstderr: {stderr}");
            Assert.That(stdout, Is.Empty, "no bug must be reported");
            Assert.That(stderr, Does.Contain("MissingMethodException"));
            Assert.That(stderr, Does.Contain("re-run 'p compile'"));
        }

        [Test]
        public void BindingFailureInTestMethodIsAnInternalError()
        {
            TestReport report;
            lock (PCheckerRunner.CheckerLock)
            {
                report = Run(_ => throw new MissingFieldException("Field not found: 'PTuple.fieldValues'."), 5,
                    out _);
            }

            Assert.That(report.BugReports, Is.Empty, "a binding failure is not a bug in the program");
            Assert.That(report.InternalErrors, Has.Count.EqualTo(1));
            Assert.That(report.InternalErrors.Single(), Does.Contain("re-run 'p compile'"));
        }

        [Test]
        public void ObsoleteRuntimeSendEventSendsFromAStateMachine()
        {
            const uint iterations = 20;
            TestReport report;
            int received;
            lock (PCheckerRunner.CheckerLock)
            {
                pingsReceived = 0;
                report = Run(runtime =>
                {
                    currentRuntime = runtime;
                    var receiver = runtime.CreateStateMachine(typeof(Receiver));
                    var target = new PMachineValue(receiver, new List<string> { nameof(Ping) });
                    runtime.CreateStateMachine(typeof(LegacySender), new Event(target));
                }, iterations, out _);
                received = pingsReceived;
            }

            Assert.That(report.BugReports, Is.Empty);
            Assert.That(report.InternalErrors, Is.Empty);
            Assert.That(received, Is.EqualTo((int)iterations), "every send must be delivered");
        }

        [Test]
        public void ObsoleteRuntimeSendEventFromTestMethodFailsClearly()
        {
            TestReport report;
            lock (PCheckerRunner.CheckerLock)
            {
                report = Run(runtime =>
                {
                    var receiver = runtime.CreateStateMachine(typeof(Receiver));
#pragma warning disable CS0618
                    runtime.SendEvent(receiver, new Ping());
#pragma warning restore CS0618
                }, 1, out _);
            }

            Assert.That(report.BugReports.Single(), Does.Contain("not from the test method"));
        }

        private static Assembly DefineAssembly(bool generatedByP, int? stamp)
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Compat" + Guid.NewGuid().ToString("N")),
                AssemblyBuilderAccess.Run);
            if (stamp.HasValue)
            {
                var ctor = typeof(GeneratedCodeVersionAttribute).GetConstructor(new[] { typeof(int) });
                assembly.SetCustomAttribute(new CustomAttributeBuilder(ctor, new object[] { stamp.Value }));
            }

            var module = assembly.DefineDynamicModule("Main");
            var typeName = generatedByP ? "PImplementation.GlobalConfig" : "Harness.Tests";
            module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class).CreateType();
            return assembly;
        }

        [Test]
        public void GeneratedCodeOfThisVersionIsAccepted()
        {
            var assembly = DefineAssembly(true, GeneratedCodeVersionAttribute.Current);
            Assert.That(GeneratedCodeVersionAttribute.GetIncompatibilityError(assembly), Is.Null);
        }

        [Test]
        public void UnstampedCodeNotGeneratedByPIsAccepted()
        {
            var assembly = DefineAssembly(false, null);
            Assert.That(GeneratedCodeVersionAttribute.GetIncompatibilityError(assembly), Is.Null);
        }

        [Test]
        public void UnstampedGeneratedCodeIsRejected()
        {
            var assembly = DefineAssembly(true, null);
            var error = GeneratedCodeVersionAttribute.GetIncompatibilityError(assembly);
            Assert.That(error, Does.Contain("an older version of P"));
            Assert.That(error, Does.Contain("re-run 'p compile'"));
        }

        [Test]
        public void GeneratedCodeOfAnotherVersionIsRejected()
        {
            var assembly = DefineAssembly(true, GeneratedCodeVersionAttribute.Current + 1);
            Assert.That(GeneratedCodeVersionAttribute.GetIncompatibilityError(assembly),
                Does.Contain("a newer version of P"));
        }

        [Test]
        public void TestDiscoveryExplainsMissingTaskTypes()
        {
            var ex = new ReflectionTypeLoadException(new Type[] { null }, new Exception[]
            {
                new TypeLoadException("Could not load type 'PChecker.Tasks.Task' from assembly 'PCheckerCore'.")
            });
            var hint = TestMethodInfo.GetDiscoveryFailureHint(ex);
            Assert.That(hint, Does.Contain("PChecker.Tasks.Task"));
            Assert.That(hint, Does.Contain("recompile"));
            Assert.That(hint, Does.Contain("asynchronous test methods"));
            Assert.That(TestMethodInfo.GetDiscoveryFailureHint(new InvalidOperationException()), Is.Empty);
        }
    }
}
