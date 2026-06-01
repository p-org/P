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

        /// <summary>
        /// Default behavior: no flag passed → CompilerConfiguration defaults
        /// to collecting mode. Regression guard: if someone refactors
        /// CompilerConfiguration's ctor and accidentally sets
        /// ContinueOnError=false, the entire multi-error UX silently regresses.
        /// </summary>
        [Test]
        public void StrictErrorsFlag_NotPassed_CollectingModeDefault()
        {
            var config = new CompilerConfiguration();
            Assert.IsTrue(config.ContinueOnError,
                "Without --strict-errors, ContinueOnError must default to true");
            Assert.IsTrue(config.Diagnostics.ContinueOnError,
                "Collector mode must agree with the config flag");
        }

        /// <summary>
        /// --strict-errors opt-out: when the parser dispatches the flag, it
        /// must flip ContinueOnError to false AND reconstruct Diagnostics +
        /// Handler around a fresh strict collector. The Handler must point at
        /// the SAME collector instance as the config (the shared-instance
        /// invariant enforced by DefaultTranslationErrorHandler's null-throw).
        ///
        /// Tested via UpdateConfigurationWithParsedArgument (the per-flag
        /// dispatch entry point) rather than the full Parse() pipeline, which
        /// would Environment.Exit on missing files.
        /// </summary>
        [Test]
        public void StrictErrorsFlag_Dispatched_StrictModeAndCollectorRebuilt()
        {
            var config = new CompilerConfiguration();
            var option = new Plang.Parser.CommandLineArgument
            {
                LongName = "strict-errors",
                DataType = typeof(bool),
                Value = true,
            };
            var originalCollector = config.Diagnostics;

            PCompilerOptions.UpdateConfigurationWithParsedArgument(config, option);

            Assert.IsFalse(config.ContinueOnError,
                "--strict-errors must set ContinueOnError to false");
            Assert.IsFalse(config.Diagnostics.ContinueOnError,
                "Diagnostics collector must be rebuilt with continueOnError=false");
            Assert.AreNotSame(originalCollector, config.Diagnostics,
                "A fresh collector must be allocated — reusing the existing " +
                "(collecting-mode) collector would silently keep appending instead of throwing");
            Assert.AreSame(config.Diagnostics, config.Handler.Diagnostics,
                "Handler.Diagnostics must be the same instance as config.Diagnostics " +
                "after the rebuild — otherwise Compiler.cs's flush sees a different collector");
        }

        /// <summary>
        /// `strict-errors` must appear as a registered CLI argument with the
        /// `se` short alias. Catches a typo in the AddArgument's short-name
        /// param without depending on the full Parse() pipeline.
        /// </summary>
        [Test]
        public void StrictErrorsFlag_RegisteredWithShortAlias()
        {
            var options = new PCompilerOptions();
            Assert.IsTrue(options.Parser.Arguments.TryGetValue("strict-errors", out var arg),
                "--strict-errors must be registered with the parser");
            Assert.AreEqual("se", arg.ShortName,
                "Short alias `-se` must map to the same flag — otherwise the " +
                "convention `--strict-errors`/`-se` documented in README and CLAUDE.md is broken");
            Assert.AreEqual(typeof(bool), arg.DataType,
                "--strict-errors must be a boolean flag");
        }
    }
}
