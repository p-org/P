using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Plang.Compiler;
using Plang.Compiler.Backend;
using UnitTests.Core;

namespace UnitTests
{
    /// <summary>
    /// Golden / snapshot tests for the imperative backends. Compiles a small fixed P program
    /// (front-end + code generation, no external build) and compares the generated files to a
    /// committed snapshot. Catches *any* unintended change to generated output - a complement
    /// to <see cref="AstEmitterExhaustivenessTests"/> (which only checks node coverage).
    ///
    /// To refresh the snapshots after an intended codegen change, run with the environment
    /// variable UPDATE_GOLDEN=1.
    /// </summary>
    [TestFixture]
    public class GoldenCodegenTests
    {
        private static readonly string GoldenDir =
            Path.Combine(Constants.SolutionDirectory, "Tst", "UnitTests", "GoldenTests");

        [TestCase(CompilerOutput.PChecker)]
        [TestCase(CompilerOutput.PEx)]
        [TestCase(CompilerOutput.PObserve)]
        public void GeneratedCodeMatchesSnapshot(CompilerOutput backend)
        {
            var inputFile = Path.Combine(GoldenDir, "Input", "golden.p");
            var job = new CompilerConfiguration(
                new DiscardOutput(),
                new DirectoryInfo(Path.GetTempPath()),
                new[] { backend },
                new[] { inputFile },
                "Golden");

            var files = new Compiler().GenerateCodeInMemory(job);
            var actual = Normalize(string.Join(
                "\n",
                files.OrderBy(f => f.FileName, StringComparer.Ordinal)
                    .Select(f => $"==== {f.FileName} ====\n{f.Contents}")));

            var snapshotPath = Path.Combine(GoldenDir, "Expected", $"{backend}.txt");

            if (Environment.GetEnvironmentVariable("UPDATE_GOLDEN") == "1")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
                File.WriteAllText(snapshotPath, actual);
                Assert.Pass($"Updated snapshot {snapshotPath}");
            }

            Assert.IsTrue(File.Exists(snapshotPath),
                $"Missing snapshot {snapshotPath}. Generate it with UPDATE_GOLDEN=1.");
            var expected = Normalize(File.ReadAllText(snapshotPath));
            Assert.AreEqual(expected, actual,
                $"Generated {backend} code changed. If intended, refresh with UPDATE_GOLDEN=1.");
        }

        // Normalize line endings and PObserve's per-run "auto-generated on <date>" header so the
        // snapshot is deterministic.
        private static string Normalize(string s)
        {
            s = s.Replace("\r\n", "\n");
            // PObserve embeds a per-run timestamp in its header.
            s = Regex.Replace(s, @"auto-generated on .*", "auto-generated on <DATE>");
            // Embedded source locations (e.g. in assert messages) carry a path that depends on
            // the output directory / checkout location; keep the stable line:col, drop the path.
            s = Regex.Replace(s, @"[^""\s]*golden\.p:", "golden.p:");
            return s;
        }

        private sealed class DiscardOutput : ICompilerOutput
        {
            public void WriteMessage(string msg, SeverityKind severity) { }
            public void WriteFile(CompiledFile file) { }
            public void WriteError(string msg) { }
            public void WriteInfo(string msg) { }
            public void WriteWarning(string msg) { }
        }
    }
}
