using System.IO;
using Plang.Compiler;
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
        public void TestFailureDetector()
        {
            var tempDir =
                Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, nameof(TestForeignTypes)));
            var driverPath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Src", "Samples", "FailureDetector",
                "Driver.p"));
            var failureDetectorPath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Src", "Samples",
                "FailureDetector",
                "FailureDetector.p"));
            var prtDistPath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Src", "Samples",
                "FailureDetector",
                "PrtDistHelp.p"));
            var timerPath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Src", "Samples", "Timer",
                "TimerHeader.p"));
            var testCase = new CompilerTestCase(
                tempDir,
                new CompileOnlyRunner(CompilerOutput.C,
                    new[] {driverPath, failureDetectorPath, prtDistPath, timerPath}),
                new CompileSuccessValidator());

            TestAssertions.AssertTestCase(testCase);
        }

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
        public void TestPingPong()
        {
            var tempDir =
                Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, nameof(TestForeignTypes)));
            var pingPongPath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Src", "Samples", "PingPong",
                "PingPong.p"));
            var prtDistPath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Src", "Samples", "PingPong",
                "PrtDistHelp.p"));
            var testCase = new CompilerTestCase(
                tempDir,
                new CompileOnlyRunner(CompilerOutput.C, new[] {pingPongPath, prtDistPath}),
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
                new CompileOnlyRunner(CompilerOutput.C, new[] {tempFilePath}),
                new CompileSuccessValidator());

            TestAssertions.AssertTestCase(testCase);
        }
    }
}