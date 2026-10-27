using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using PChecker;
using PChecker.SystematicTesting;
using UnitTests.Core;
using UnitTests.Runners;
using TestAttribute = NUnit.Framework.TestAttribute;

namespace UnitTests
{
    /// <summary>
    /// Code compiled against another version of the P runtime must be reported as a tool error that
    /// asks to recompile, never as a bug in the program under test (or silently run with different
    /// semantics).
    /// </summary>
    [TestFixture]
    public class RuntimeCompatibilityTest
    {
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
    }
}
