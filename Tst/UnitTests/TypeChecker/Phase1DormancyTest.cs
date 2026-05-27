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
/// Strict-vs-collecting consistency suite. For every leaf <c>Correct/</c> or
/// <c>StaticError/</c> test directory under <c>RegressionTests/</c>, compile
/// twice — once strict (today's behavior, default) and once collecting
/// (<c>ContinueOnError = true</c>) — and assert mode-appropriate invariants.
///
/// Phase 1 had a single fixture asserting bit-identical stderr in both modes,
/// since no visitor reported through the collector. Phase 2 lights up the
/// collector in ExprVisitor + StatementVisitor, so collecting mode can now
/// report MORE errors per file when independent issues exist. The fixture is
/// split accordingly:
///
///   - <see cref="StrictAndCollectingAgreeOnValidPrograms"/> covers
///     <c>Correct/</c> only — valid programs must produce no diagnostics in
///     either mode, so identical stderr remains the right invariant.
///   - <see cref="CollectingReportsAtLeastAsManyErrorsAsStrict"/> covers
///     <c>StaticError/</c> only — strict aborts on the first error;
///     collecting accumulates. We assert exit code parity (both fail) and
///     that collecting's error count is &gt;= strict's count, which holds
///     by construction unless the collector also suppresses errors that
///     strict would surface (a regression we want to catch).
/// </summary>
[TestFixture]
public class Phase1DormancyTest
{
    private static IEnumerable<TestCaseData> DiscoverCorrectInputs()
    {
        return TestCaseLoader.FindTestCasesInDirectory(
            Constants.TestDirectory,
            new[] { "Correct" });
    }

    private static IEnumerable<TestCaseData> DiscoverStaticErrorInputs()
    {
        return TestCaseLoader.FindTestCasesInDirectory(
            Constants.TestDirectory,
            new[] { "StaticError" });
    }

    [TestCaseSource(nameof(DiscoverCorrectInputs))]
    [Category("Phase1Dormancy")]
    public void StrictAndCollectingAgreeOnValidPrograms(DirectoryInfo testDir)
    {
        var inputFiles = testDir.GetFiles("*.p").Select(f => f.FullName).ToList();
        if (inputFiles.Count == 0)
        {
            Assert.Ignore("no .p files in directory");
            return;
        }

        var (codeStrict, stderrStrict, _) = RunOnce(inputFiles, "valid_strict", testDir, continueOnError: false);
        var (codeCollecting, stderrCollecting, _) = RunOnce(inputFiles, "valid_collecting", testDir, continueOnError: true);

        // Both modes must succeed on a valid program. If either mode errors,
        // we have a regression — strict had a bug that collecting reproduces,
        // or collecting introduced a spurious diagnostic that strict avoids.
        Assert.AreEqual(0, codeStrict,
            $"Strict mode unexpectedly failed on valid program {testDir.FullName}:\n{stderrStrict}");
        Assert.AreEqual(0, codeCollecting,
            $"Collecting mode unexpectedly failed on valid program {testDir.FullName}:\n{stderrCollecting}");

        // And the diagnostic streams must be empty (or identical, since both
        // should be empty). This catches a spurious warning that only one mode
        // emits.
        Assert.AreEqual(
            stderrStrict, stderrCollecting,
            $"Diagnostic output differs on valid program {testDir.FullName}:\n" +
            $"  strict:     {stderrStrict}\n" +
            $"  collecting: {stderrCollecting}");
    }

    [TestCaseSource(nameof(DiscoverStaticErrorInputs))]
    [Category("Phase1Dormancy")]
    public void CollectingReportsAtLeastAsManyErrorsAsStrict(DirectoryInfo testDir)
    {
        var inputFiles = testDir.GetFiles("*.p").Select(f => f.FullName).ToList();
        if (inputFiles.Count == 0)
        {
            Assert.Ignore("no .p files in directory");
            return;
        }

        var (codeStrict, stderrStrict, errorsStrict) = RunOnce(inputFiles, "err_strict", testDir, continueOnError: false);
        var (codeCollecting, stderrCollecting, errorsCollecting) = RunOnce(inputFiles, "err_collecting", testDir, continueOnError: true);

        // Both modes must fail (the existing StaticErrorValidator asserts
        // exit code == 1; preserve that contract on both sides).
        Assert.AreEqual(1, codeStrict,
            $"Strict mode unexpectedly succeeded on {testDir.FullName}:\n{stderrStrict}");
        Assert.AreEqual(1, codeCollecting,
            $"Collecting mode unexpectedly succeeded on {testDir.FullName}:\n{stderrCollecting}");

        // Collecting must surface at least as many errors as strict. The
        // converse — collecting silently suppressing a strict error — would
        // be a cascade-suppression bug; this is the most important invariant
        // this fixture guards.
        Assert.GreaterOrEqual(
            errorsCollecting, errorsStrict,
            $"Collecting mode reported fewer errors than strict on {testDir.FullName}: " +
            $"strict={errorsStrict}, collecting={errorsCollecting}.\n" +
            $"  stderr (strict):\n{stderrStrict}\n" +
            $"  stderr (collecting):\n{stderrCollecting}");
    }

    /// <summary>
    /// Run the compiler against <paramref name="inputFiles"/> with the
    /// requested mode and capture exit code + stderr + a coarse error count
    /// (number of "[Error:" / "[Parser Error:" markers in stderr, which is
    /// how Compiler.cs prefixes every diagnostic it emits).
    /// </summary>
    private static (int exitCode, string stderr, int errorCount) RunOnce(
        IList<string> inputFiles, string scratchTag, DirectoryInfo testDir, bool continueOnError)
    {
        var stdoutWriter = new StringWriter();
        var stderrWriter = new StringWriter();
        var output = new CapturingOutput(stdoutWriter, stderrWriter);

        var scratchDir = Directory.CreateDirectory(Path.Combine(
            Constants.ScratchParentDirectory,
            "Phase1Dormancy",
            scratchTag,
            testDir.FullName.GetHashCode().ToString("X8")));

        var config = new CompilerConfiguration(
            output,
            scratchDir,
            new List<CompilerOutput> { CompilerOutput.PChecker },
            inputFiles,
            Path.GetFileNameWithoutExtension(inputFiles.First()),
            // PChecker backend has a dotnet-build compilation stage that
            // dereferences ProjectRootPath. Mirror PCheckerRunner.cs by
            // passing the scratch directory as the project root, otherwise
            // valid programs NRE at PCheckerCodeGenerator.Compile during the
            // second stage of Compiler.Compile.
            projectRoot: scratchDir);

        // Override after construction. The order matters: collector first,
        // then handler (which holds a reference to the collector), then
        // the flag itself. This bypasses the env var so parallel test
        // fixtures don't interfere.
        config.Diagnostics = new DefaultDiagnosticCollector(continueOnError);
        config.Handler = new DefaultTranslationErrorHandler(config.LocationResolver, config.Diagnostics);
        config.ContinueOnError = continueOnError;

        int exitCode;
        try
        {
            exitCode = new Compiler().Compile(config);
        }
        catch (TranslationException e)
        {
            // ONLY catch TranslationException — NREs and other unexpected
            // runtime exceptions must propagate so they surface as bugs to
            // fix, not get silently folded into "exit -1 with stderr noise".
            // Phase 2's MultiAgent audit specifically called out catch(Exception)
            // as masking real bugs (e.g., two parallel NREs in this test would
            // produce identical stderr and pass the equality check green).
            stderrWriter.WriteLine($"[Test harness caught uncaught TranslationException:] {e.Message}");
            stderrWriter.WriteLine(e.StackTrace);
            exitCode = -1;
        }

        var stderr = stderrWriter.ToString();
        var errorCount = CountErrorMarkers(stderr);
        return (exitCode, stderr, errorCount);
    }

    // Cached compiled regexes (multi-agent perf finding): called ~1k+ times
    // per suite run (one per dir × two modes). RegexOptions.Compiled gets
    // JIT'd once and reused; the previous `new Regex(...)` per call paid
    // ~50-200µs per compile.
    //
    // Pattern intent: match every diagnostic marker Compiler.cs emits on
    // stderr — originally just `[Error:]` and `[Parser Error:]`, but
    // Compiler.cs also emits `[NotSupportedError:]`, `[NotImplementedError:]`,
    // and per-backend `[X Compiling Generated Code:]`. A counter that misses
    // these silently treats a regression-that-throws-NotSupportedException
    // as "zero errors" (we already hit this once on the PVerifier `invariant`
    // keyword in ForeachInvariantError.p). NOTE: the harness's own marker
    // `[Test harness caught uncaught TranslationException:]` is intentionally
    // substring-free of "Error" so it does NOT match — keeps test-exception
    // output from inflating pinned counts.
    private static readonly System.Text.RegularExpressions.Regex ErrorMarkerPattern =
        new System.Text.RegularExpressions.Regex(
            @"\[[^\]]*Error[^\]]*:\]",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex GenCodeMarkerPattern =
        new System.Text.RegularExpressions.Regex(
            @"\[\S+ Compiling Generated Code:\]",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static int CountErrorMarkers(string stderr)
    {
        if (string.IsNullOrEmpty(stderr)) return 0;
        return ErrorMarkerPattern.Matches(stderr).Count
             + GenCodeMarkerPattern.Matches(stderr).Count;
    }

    /// <summary>
    /// Minimal <see cref="ICompilerOutput"/> that splits messages into the
    /// captured stdout / stderr writers based on severity. Generated files
    /// are intentionally dropped: the fixture only cares about the
    /// diagnostic stream.
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
