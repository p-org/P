using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Plang.Compiler;
using Plang.Compiler.Backend;
using UnitTests.Core;

namespace UnitTests.TypeChecker;

/// <summary>
/// Phase 1 dormancy regression suite. For every leaf <c>Correct/</c> and
/// <c>StaticError/</c> test directory under <c>RegressionTests/</c>, compile
/// the inputs twice — once in strict mode (today's behavior, default) and
/// once in collecting mode (<c>ContinueOnError = true</c>) — and assert the
/// exit code and the entire error-stream output match.
///
/// Phase 1 promises the collector flag is dormant: no visitor reports through
/// it yet, so flipping the flag must produce bit-identical user-visible
/// output. This fixture turns that promise into an enforceable invariant.
///
/// When Phase 2 starts converting throw sites to record-and-continue, this
/// fixture WILL begin failing — which is the correct signal. At that point
/// the fixture should either be deleted (if the dormancy promise is being
/// retired) or split so the StaticError subset tolerates more errors per file
/// while the Correct subset still demands identical output.
/// </summary>
[TestFixture]
public class Phase1DormancyTest
{
    /// <summary>
    /// Source of test cases. Reuses the existing TestCaseLoader so we
    /// automatically pick up any new <c>Correct/</c> or <c>StaticError/</c>
    /// directory added under the discovered feature folders. No need to
    /// hand-maintain a list.
    /// </summary>
    private static IEnumerable<TestCaseData> DiscoverInputs()
    {
        return TestCaseLoader.FindTestCasesInDirectory(
            Constants.TestDirectory,
            new[] { "Correct", "StaticError" });
    }

    [TestCaseSource(nameof(DiscoverInputs))]
    [Category("Phase1Dormancy")]
    public void StrictAndCollectingModesAgree(DirectoryInfo testDir)
    {
        var inputFiles = testDir.GetFiles("*.p").Select(f => f.FullName).ToList();
        if (inputFiles.Count == 0)
        {
            // Some sub-directories in the regression tree don't contain .p
            // files directly (e.g. dependency holders). Nothing to compare.
            Assert.Ignore("no .p files in directory");
            return;
        }

        var rootScratch = Path.Combine(
            Constants.ScratchParentDirectory,
            "Phase1Dormancy",
            testDir.FullName.GetHashCode().ToString("X8"));
        var scratchStrict = Directory.CreateDirectory(Path.Combine(rootScratch, "strict"));
        var scratchCollecting = Directory.CreateDirectory(Path.Combine(rootScratch, "collecting"));

        var (codeStrict, stderrStrict) = RunOnce(inputFiles, scratchStrict, continueOnError: false);
        var (codeCollecting, stderrCollecting) = RunOnce(inputFiles, scratchCollecting, continueOnError: true);

        // Exit code is the primary contract: strict and collecting modes must
        // succeed or fail in lockstep until Phase 2 lights up the flag.
        Assert.AreEqual(
            codeStrict, codeCollecting,
            $"Exit code differs for {testDir.FullName}.\n" +
            $"  strict     = {codeStrict}\n" +
            $"  collecting = {codeCollecting}\n" +
            $"  stderr (strict)     = {stderrStrict}\n" +
            $"  stderr (collecting) = {stderrCollecting}");

        // Error stream must match verbatim. If a visitor accidentally starts
        // reporting through the collector, output ordering or wording will
        // diverge here even if the exit code happens to agree.
        Assert.AreEqual(
            stderrStrict, stderrCollecting,
            $"Error output differs for {testDir.FullName} — a visitor may be " +
            "reporting through the collector when it shouldn't be.");
    }

    /// <summary>
    /// Run the compiler against <paramref name="inputFiles"/> with the
    /// requested mode and capture exit code + stderr. Builds the
    /// <see cref="CompilerConfiguration"/> directly so we can flip
    /// <see cref="CompilerConfiguration.ContinueOnError"/> without touching
    /// the <c>P_COMPILER_COLLECT_ERRORS</c> env var (which would leak
    /// between parallel test fixtures).
    /// </summary>
    private static (int exitCode, string stderr) RunOnce(
        IList<string> inputFiles, DirectoryInfo scratchDir, bool continueOnError)
    {
        var stdoutWriter = new StringWriter();
        var stderrWriter = new StringWriter();
        var output = new CapturingOutput(stdoutWriter, stderrWriter);

        var config = new CompilerConfiguration(
            output,
            scratchDir,
            new List<CompilerOutput> { CompilerOutput.PChecker },
            inputFiles,
            Path.GetFileNameWithoutExtension(inputFiles.First()));

        // Override after construction. The order matters: collector first,
        // then handler (which holds a reference to the collector), then
        // the flag itself.
        config.Diagnostics = new DefaultDiagnosticCollector(continueOnError);
        config.Handler = new DefaultTranslationErrorHandler(config.LocationResolver, config.Diagnostics);
        config.ContinueOnError = continueOnError;

        int exitCode;
        try
        {
            exitCode = new Compiler().Compile(config);
        }
        catch (Exception e)
        {
            // CompileOnlyRunner converts uncaught exceptions to a test failure;
            // we capture them as a synthetic stderr line + sentinel exit code
            // so the two runs can still be compared. If one mode crashes and
            // the other doesn't, the assertion will fire with the diff.
            stderrWriter.WriteLine($"[Test harness caught uncaught exception:] {e.Message}");
            exitCode = -1;
        }

        return (exitCode, stderrWriter.ToString());
    }

    /// <summary>
    /// Minimal <see cref="ICompilerOutput"/> that splits messages into the
    /// captured stdout / stderr writers based on severity. Generated files
    /// are intentionally dropped: the fixture only cares about the
    /// diagnostic stream, and including generated code would add noise
    /// without strengthening the dormancy assertion.
    /// </summary>
    private sealed class CapturingOutput : ICompilerOutput
    {
        private readonly TextWriter stdout;
        private readonly TextWriter stderr;

        public CapturingOutput(TextWriter stdout, TextWriter stderr)
        {
            this.stdout = stdout;
            this.stderr = stderr;
        }

        public void WriteMessage(string msg, SeverityKind severity)
        {
            (severity == SeverityKind.Info ? stdout : stderr).WriteLine(msg);
        }

        public void WriteFile(CompiledFile file)
        {
            // Drop on the floor — see class doc.
        }

        public void WriteError(string msg) => stderr.WriteLine(msg);

        public void WriteInfo(string msg) => stdout.WriteLine(msg);

        public void WriteWarning(string msg) => stderr.WriteLine(msg);
    }
}
