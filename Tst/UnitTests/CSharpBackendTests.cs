using System.IO;
using NUnit.Framework;
using UnitTests.Core;
using UnitTests.Runners;
using UnitTests.Validators;

namespace UnitTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    public class CSharpBackendTests
    {
        [Test]
        public void TestCompileCoyoteTemp()
        {
            var tempDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, "TestPCheckerTemp"));
            var tempFilePath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "tmp", "test.p"));
            //var foreignFilePath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "tmp", "Foreign.cs"));

            if (!tempFilePath.Exists)
            {
                return;
            }

            var testCase = new CompilerTestCase(
                tempDir,
                new PCheckerRunner(new[] { tempFilePath }),
                new CompileSuccessValidator());

            TestAssertions.AssertTestCase(testCase);
        }

        /*
        [Test]
        public void TestModuleSystem()
        {
            var tempDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, "TestCoyoteTemp"));
            var allFiles = new[]
            {
                new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tutorial", "PingPong", "Main.p")),
                new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tutorial", "PingPong", "PingPong.p")),
                new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tutorial", "PingPong", "Safety.p")),
                new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tutorial", "PingPong", "Liveness.p")),
                new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tutorial", "PingPong", "Testscript.p")),
                new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tutorial", "Env", "Env.p")),
                new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tutorial", "Timer", "Timer.p"))
            };

            var testCase = new CompilerTestCase(
                tempDir,
                new PCheckerRunner(allFiles),
                new CompileSuccessValidator());

            TestAssertions.AssertTestCase(testCase);
        }

        [Test]
        public void TestTwoPhaseCommit()
        {
            var tempDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, "TestTwoPhaseCommit"));
            var allPFiles = new[]
            {
                new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tutorial", "TwoPhaseCommit", "Client.p")),
                new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tutorial", "TwoPhaseCommit", "Coordinator.p")),
                new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tutorial", "TwoPhaseCommit", "Events.p")),
                new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tutorial", "TwoPhaseCommit", "Participant.p")),
                new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tutorial", "TwoPhaseCommit", "TestDriver.p")),
                new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tutorial", "TwoPhaseCommit", "Timer.p")),
                new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tutorial", "TwoPhaseCommit", "Spec.p"))
            };

            var foreignCode = new[]
            {
                new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tutorial", "TwoPhaseCommit", "ForeignCode.cs"))
            };

            var testCase = new CompilerTestCase(
                tempDir,
                new PCheckerRunner(allPFiles, foreignCode),
                new CompileSuccessValidator());

            TestAssertions.AssertTestCase(testCase);
        }
        */
    }
}