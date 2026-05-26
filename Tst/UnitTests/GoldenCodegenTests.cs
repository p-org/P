using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
            // Snapshots are byte-exact and are generated/maintained on the Linux toolchain
            // (the Ubuntu CI job is the canonical drift gate). Generated code carries
            // unavoidable platform-specific noise on other OSes (line endings, embedded
            // source-path separators), so only assert on Linux rather than chase normalization
            // for every platform.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Ignore("Golden snapshots are pinned to the Linux toolchain.");
            }

            var inputFile = Path.Combine(GoldenDir, "Input", "golden.p");

            // Some backends (e.g. PObserve) write incidental scaffolding such as pom.xml during
            // GenerateCode, so give each test run an isolated output directory and clean it up.
            var outputDir = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), $"GoldenCodegen_{backend}_{Guid.NewGuid():N}"));

            string actual;
            try
            {
                var job = new CompilerConfiguration(
                    new DiscardOutput(),
                    outputDir,
                    new[] { backend },
                    new[] { inputFile },
                    "Golden");

                var files = new Compiler().GenerateCodeInMemory(job);
                actual = Normalize(string.Join(
                    "\n",
                    files.OrderBy(f => f.FileName, StringComparer.Ordinal)
                        .Select(f => $"==== {f.FileName} ====\n{f.Contents}")));
            }
            finally
            {
                try { outputDir.Delete(recursive: true); } catch (IOException) { }
            }

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
            // These locations sit inside string literals, so match any non-quote prefix - this
            // also tolerates paths containing spaces (common on Windows user profiles).
            s = Regex.Replace(s, @"[^""]*golden\.p:", "golden.p:");
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
