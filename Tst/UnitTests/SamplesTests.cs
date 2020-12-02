namespace UnitTests
{
    /*
    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    internal class SamplesTests
    {
        [Test]
        public void TestFailureDetector()
        {
            DirectoryInfo tempDir =
                Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, nameof(TestForeignTypes)));
            FileInfo driverPath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Src", "Samples", "FailureDetector",
                "Driver.p"));
            FileInfo failureDetectorPath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Src", "Samples",
                "FailureDetector",
                "FailureDetector.p"));
            FileInfo prtDistPath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Src", "Samples",
                "FailureDetector",
                "PrtDistHelp.p"));
            FileInfo timerPath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Src", "Samples", "Timer",
                "TimerHeader.p"));
            CompilerTestCase testCase = new CompilerTestCase(
                tempDir,
                new CompileOnlyRunner(CompilerOutput.C,
                    new[] { driverPath, failureDetectorPath, prtDistPath, timerPath }),
                new CompileSuccessValidator());

            TestAssertions.AssertTestCase(testCase);
        }

        [Test]
        public void TestForeignTypes()
        {
            DirectoryInfo tempDir =
                Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, nameof(TestForeignTypes)));
            FileInfo tempFilePath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Src", "Samples", "ForeignTypes",
                "ForeignStringType.p"));

            CompilerTestCase testCase = new CompilerTestCase(
                tempDir,
                new CompileOnlyRunner(CompilerOutput.C, new[] { tempFilePath }),
                new CompileSuccessValidator());

            TestAssertions.AssertTestCase(testCase);
        }

        [Test]
        public void TestPingPong()
        {
            DirectoryInfo tempDir =
                Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, nameof(TestForeignTypes)));
            FileInfo pingPongPath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Src", "Samples", "PingPong",
                "PingPong.p"));
            FileInfo prtDistPath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Src", "Samples", "PingPong",
                "PrtDistHelp.p"));
            CompilerTestCase testCase = new CompilerTestCase(
                tempDir,
                new CompileOnlyRunner(CompilerOutput.C, new[] { pingPongPath, prtDistPath }),
                new CompileSuccessValidator());

            TestAssertions.AssertTestCase(testCase);
        }

        [Test]
        public void TestTimer()
        {
            DirectoryInfo tempDir =
                Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, nameof(TestForeignTypes)));
            FileInfo tempFilePath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Src", "Samples", "Timer",
                "TimerHeader.p"));

            CompilerTestCase testCase = new CompilerTestCase(
                tempDir,
                new CompileOnlyRunner(CompilerOutput.C, new[] { tempFilePath }),
                new CompileSuccessValidator());

            TestAssertions.AssertTestCase(testCase);
        }
    }*/
}