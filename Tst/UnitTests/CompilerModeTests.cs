using NUnit.Framework;
using Plang.Compiler;
using Plang.Options;
using PChecker;

namespace UnitTests
{
    /// <summary>
    /// Guards the CLI <c>--mode</c> handling. The bug these protect against: an `--mode`
    /// value that the parser accepts (in AllowedValues) but the handler doesn't map, which
    /// makes a documented mode crash at runtime (regression introduced in the P 3.0 rename).
    /// </summary>
    [TestFixture]
    public class CompilerModeTests
    {
        [Test]
        public void EveryAllowedCompilerModeMapsWithoutThrowing()
        {
            foreach (var mode in PCompilerOptions.CompilerModes)
            {
                Assert.DoesNotThrow(() => PCompilerOptions.ParseCompilerMode(mode),
                    $"--mode {mode} is in AllowedValues but is not handled.");
            }
        }

        [Test]
        public void CompilerModeMappings()
        {
            Assert.AreEqual(CompilerOutput.PChecker, PCompilerOptions.ParseCompilerMode("bugfinding"));
            Assert.AreEqual(CompilerOutput.PChecker, PCompilerOptions.ParseCompilerMode("pchecker"));
            Assert.AreEqual(CompilerOutput.PEx, PCompilerOptions.ParseCompilerMode("pex"));
            Assert.AreEqual(CompilerOutput.PObserve, PCompilerOptions.ParseCompilerMode("pobserve"));
            Assert.AreEqual(CompilerOutput.PVerifier, PCompilerOptions.ParseCompilerMode("verification"));
            Assert.AreEqual(CompilerOutput.PVerifier, PCompilerOptions.ParseCompilerMode("pverifier"));
        }

        [Test]
        public void CompilerModeIsCaseInsensitive()
        {
            Assert.AreEqual(CompilerOutput.PChecker, PCompilerOptions.ParseCompilerMode("BugFinding"));
        }

        [Test]
        public void UnknownCompilerModeThrows()
        {
            Assert.Throws<System.Exception>(() => PCompilerOptions.ParseCompilerMode("nonsense"));
        }

        [Test]
        public void EveryAllowedCheckerModeMapsWithoutThrowing()
        {
            foreach (var mode in PCheckerOptions.CheckerModes)
            {
                Assert.DoesNotThrow(() => PCheckerOptions.ParseCheckerMode(mode),
                    $"checker --mode {mode} is in AllowedValues but is not handled.");
            }
        }

        [Test]
        public void CheckerModeMappings()
        {
            Assert.AreEqual(CheckerMode.BugFinding, PCheckerOptions.ParseCheckerMode("bugfinding"));
            Assert.AreEqual(CheckerMode.PEx, PCheckerOptions.ParseCheckerMode("pex"));
        }
    }
}
