using System.IO;
using NUnit.Framework;
using UnitTests.Core;
using UnitTests.Runners;

namespace UnitTests
{
    /// <summary>
    /// Foreign code that misuses the runtime must stop the checker with an internal error, instead
    /// of corrupting the scheduler or reporting a bug in the (correct) P program.
    /// </summary>
    [TestFixture]
    public class ForeignCodeMisuseTest
    {
        /// <summary>
        /// Sending through a machine other than the executing one is reported before the send is
        /// announced, so no monitor observes it.
        /// </summary>
        [Test]
        public void SendOnAnotherMachineIsAnInternalError()
        {
            var (exitCode, stdout, stderr) = Run("ForeignSendOnOtherMachine");

            Assert.That(exitCode, Is.EqualTo(2), $"expected an internal error\nstdout: {stdout}\nstderr: {stderr}");
            Assert.That(stdout, Is.Empty, "no bug must be reported");
            Assert.That(stderr, Does.Contain("invoked SendEvent while Other(3) is executing"));
        }

        /// <summary>
        /// A P function that foreign code calls back cannot wait for an event.
        /// </summary>
        [Test]
        public void ReceiveInACallbackIsAnInternalError()
        {
            var (exitCode, stdout, stderr) = Run("ForeignCallbackReceives");

            Assert.That(exitCode, Is.EqualTo(2), $"expected an internal error\nstdout: {stdout}\nstderr: {stderr}");
            Assert.That(stdout, Is.Empty, "no bug must be reported");
            Assert.That(stderr, Does.Contain("tried to receive an event in a P function called from foreign code"));
        }

        private static (int? exitCode, string stdout, string stderr) Run(string testName)
        {
            var testDir = new DirectoryInfo(Path.Combine(Constants.TestDirectory, "RegressionTests", "Integration",
                "InternalError", testName));
            var scratchDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory,
                nameof(ForeignCodeMisuseTest), testName));

            var runner = new PCheckerRunner(testDir.GetFiles("*.p"), testDir.GetFiles("*.cs"));
            var exitCode = runner.RunTest(scratchDir, out var stdout, out var stderr);
            return (exitCode, stdout, stderr);
        }
    }
}
