using System.IO;
using NUnit.Framework;
using UnitTests.Core;
using UnitTests.Runners;

namespace UnitTests
{
    /// <summary>
    /// Foreign code that drops the awaitable of a scheduling point (here StateMachine.SendEventAsync)
    /// keeps running after the scheduler has switched to another operation. The checker must detect
    /// this and stop with an internal error, instead of losing the parked operation or reporting a
    /// bug in the (correct) P program.
    /// </summary>
    [TestFixture]
    public class ForeignUnawaitedSchedulingPointTest
    {
        [Test]
        public void UnawaitedSendFromForeignCodeIsAnInternalError()
        {
            var testDir = new DirectoryInfo(Path.Combine(Constants.TestDirectory, "RegressionTests", "Integration",
                "InternalError", "ForeignUnawaitedSend"));
            var scratchDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory,
                nameof(ForeignUnawaitedSchedulingPointTest)));

            var runner = new PCheckerRunner(testDir.GetFiles("*.p"), testDir.GetFiles("*.cs"));
            var exitCode = runner.RunTest(scratchDir, out var stdout, out var stderr);

            Assert.That(exitCode, Is.EqualTo(2), $"expected an internal error\nstdout: {stdout}\nstderr: {stderr}");
            Assert.That(stdout, Is.Empty, "no bug must be reported");
            Assert.That(stderr, Does.Contain("SendEventAsync"));
        }
    }
}
