using System.IO;
using Microsoft.Pc;
using NUnit.Framework;
using UnitTests.Core;
using UnitTests.Runners;
using UnitTests.Validators;

namespace UnitTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    internal class SamplesTests
    {
        [Test]
        public void TestForeignTypes()
        {
            var tempDir =
                Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, nameof(TestForeignTypes)));
            var tempFilePath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Src", "Samples", "ForeignTypes",
                "ForeignStringType.p"));

            var testCase = new CompilerTestCase(
                tempDir,
                new CompileOnlyRunner(CompilerOutput.C, new[] {tempFilePath}),
                new CompileSuccessValidator());

            TestAssertions.AssertTestCase(testCase);
        }

        [Test]
        public void TestTimer()
        {
            var tempDir =
                Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, nameof(TestForeignTypes)));
            var tempFilePath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Src", "Samples", "Timer",
                "TimerHeader.p"));

            var testCase = new CompilerTestCase(
                tempDir,
                new CompileOnlyRunner(CompilerOutput.C, new[] { tempFilePath }),
                new CompileSuccessValidator());

            TestAssertions.AssertTestCase(testCase);
        }
    }
}