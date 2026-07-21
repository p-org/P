using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Plang.Compiler;
using Plang.Compiler.Backend;

namespace UnitTests
{
    /// <summary>
    /// Guards the two advisory warnings <c>MachineChecker</c> emits for scenario (coverage)
    /// monitors: a scenario with no accepting (cold) state, and a scenario with a <c>cold</c>
    /// START state. Both are WARNINGS (not errors) — compilation must still succeed — and a
    /// well-formed scenario (hot start → cold accept) must produce neither. A regression that
    /// dropped a warning, fired it on a well-formed scenario, or (worst) turned it into a compile
    /// error would otherwise pass CI silently.
    /// </summary>
    [TestFixture]
    public class ScenarioWarningsTest
    {
        private const string Header = "event e1: int;\nmachine Main { start state Init { entry { } } }\n";

        private static (string warnings, bool compiled) Compile(string scenario)
        {
            var warnings = new StringBuilder();
            var dir = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), $"ScenWarn_{Guid.NewGuid():N}"));
            try
            {
                var pfile = Path.Combine(dir.FullName, "model.p");
                File.WriteAllText(pfile, Header + scenario);
                var job = new CompilerConfiguration(
                    new WarningCapture(warnings), dir, new[] { CompilerOutput.PChecker },
                    new[] { pfile }, "ScenWarn");
                // Front-end + type-check run before codegen, so warnings are emitted; warnings are
                // non-fatal (ContinueOnError defaults true), so a warning still yields output files.
                var files = new Compiler().GenerateCodeInMemory(job);
                return (warnings.ToString(), files.Any());
            }
            finally
            {
                try { dir.Delete(recursive: true); } catch (IOException) { }
            }
        }

        [NUnit.Framework.Test]
        public void NoColdState_Warns_ButCompiles()
        {
            var (warnings, compiled) = Compile(
                "scenario NoCold observes e1 { start hot state A { on e1 do (k: int) { } } }");
            StringAssert.Contains("scenario 'NoCold' has no accepting (cold) state", warnings);
            Assert.IsTrue(compiled, "a no-cold scenario is a warning, not an error");
        }

        [NUnit.Framework.Test]
        public void ColdStartState_Warns_ButCompiles()
        {
            var (warnings, compiled) = Compile(
                "scenario ColdStart observes e1 { start cold state A { on e1 goto B; } hot state B { } }");
            StringAssert.Contains("scenario 'ColdStart' has a 'cold' (accepting) start state", warnings);
            Assert.IsTrue(compiled, "a cold-start scenario is a warning, not an error");
        }

        [NUnit.Framework.Test]
        public void WellFormed_HotStartColdAccept_NoWarnings()
        {
            var (warnings, compiled) = Compile(
                "scenario Good observes e1 { start hot state A { on e1 goto B; } cold state B { } }");
            Assert.IsTrue(compiled);
            StringAssert.DoesNotContain("scenario 'Good'", warnings);
            StringAssert.DoesNotContain("accepting (cold) state", warnings);
        }

        /// <summary>Captures only warning messages into a buffer; drops everything else.</summary>
        private sealed class WarningCapture : ICompilerOutput
        {
            private readonly StringBuilder _warnings;
            public WarningCapture(StringBuilder warnings) => _warnings = warnings;
            public void WriteMessage(string msg, SeverityKind severity)
            {
                if (severity == SeverityKind.Warning) _warnings.AppendLine(msg);
            }
            public void WriteFile(CompiledFile file) { }
            public void WriteError(string msg) { }
            public void WriteInfo(string msg) { }
            public void WriteWarning(string msg) => _warnings.AppendLine(msg);
        }
    }
}
