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
/// Phase 2 acceptance tests with **pinned error counts**. Each `.p` file
/// under <c>RegressionTests/Feature3Exprs/StaticError/</c> listed below
/// is curated to exercise a specific cascade-suppression rule, and the
/// strict / collecting counts are hard-coded so a regression to either
/// the throw-site conversion or the cascade rules fires a clear failure.
///
/// **When a count change is intentional, update the row AND add a comment
/// at the top of the corresponding `.p` file explaining what changed.**
///
/// Diagnostic guidance when a test here fails:
///   - Strict count grew: a Report(...) was missed in some throw site, OR
///     a child visitor was reordered to visit something earlier.
///   - Collecting count shrank: a recovery path is swallowing diagnostics
///     (likely an early-return that skips a sibling sub-tree).
///   - Collecting count grew: a cascade-suppression rule was loosened,
///     letting one upstream error generate downstream "incompatible type"
///     noise.
/// </summary>
[TestFixture]
public class MultiErrorAcceptanceTest
{
    /// <summary>
    /// Pinned-count test cases. Format: (relative path under Tst/,
    /// strict-mode count, collecting-mode count, comment).
    /// </summary>
    private static IEnumerable<TestCaseData> PinnedCases()
    {
        yield return new TestCaseData(
            "RegressionTests/Feature3Exprs/StaticError/MultipleErrors/MultipleErrors.p",
            1, 4,
            "4 sibling statements, each one independent error")
            .SetName("MultipleErrors (4 sibling stmts)");

        yield return new TestCaseData(
            "RegressionTests/Feature3Exprs/StaticError/NestedExprErrors/NestedExprErrors.p",
            1, 2,
            "single expression, 2 nested missing-declarations. Validates combiner rule — " +
            "`undeclaredA.foo + undeclaredB.bar * \"str\"` must NOT produce extra " +
            "'incompatible operand' cascades on `+` or `*`, because cascade suppression " +
            "makes those parents propagate ErrorType silently.")
            .SetName("NestedExprErrors (combiner rule)");

        yield return new TestCaseData(
            "RegressionTests/Feature3Exprs/StaticError/ForeachBodyErrors/ForeachBodyErrors.p",
            1, 2,
            "Foreach with two independent body errors when iterator is declared. " +
            "Validates that VisitForeachStmt's happy path actually visits the body " +
            "(not just iterator/collection) and that body errors surface independently. " +
            "Note: the audit-flagged invariant-visiting fix is also in StatementVisitor " +
            "but is PVerifier-only syntax — covered by a separate PVerifier test suite.")
            .SetName("ForeachBodyErrors (body visit)");

        yield return new TestCaseData(
            "RegressionTests/Feature3Exprs/StaticError/SpecReceiveBodyError/SpecReceiveBodyError.p",
            1, 3,
            "Receive on spec machine: IllegalMonitorOperation + undeclared event + body " +
            "type mismatch. Validates the multi-agent-audit fix that added event-id lookups " +
            "to the spec-machine recovery (Copilot's first fix only added handler bodies).")
            .SetName("SpecReceiveBodyError (spec recovery)");
    }

    [Test]
    [TestCaseSource(nameof(PinnedCases))]
    public void StrictMode_PinnedErrorCount(string relPath, int expectedStrict, int expectedCollecting, string description)
    {
        // expectedCollecting is read implicitly via the test parameters; the
        // collecting-mode assertion is in the parallel test below.
        _ = expectedCollecting;
        _ = description;
        var pFile = Path.Combine(Constants.TestDirectory, relPath);
        var (exitCode, stderr, errorCount) = CompileFile(pFile, continueOnError: false);
        Assert.AreEqual(1, exitCode,
            $"Strict mode must exit 1 on {relPath}. Stderr:\n{stderr}");
        Assert.AreEqual(
            expectedStrict, errorCount,
            $"Strict mode is expected to report exactly {expectedStrict} error on {relPath} " +
            $"(strict aborts on first throw). Phase 2 must not change strict-mode behavior.\n" +
            $"Captured stderr:\n{stderr}");
    }

    [Test]
    [TestCaseSource(nameof(PinnedCases))]
    public void CollectingMode_PinnedErrorCount(string relPath, int expectedStrict, int expectedCollecting, string description)
    {
        _ = expectedStrict;
        _ = description;
        var pFile = Path.Combine(Constants.TestDirectory, relPath);
        var (exitCode, stderr, errorCount) = CompileFile(pFile, continueOnError: true);
        Assert.AreEqual(1, exitCode,
            $"Collecting mode must still exit 1 when errors are present in {relPath}.\n{stderr}");
        Assert.AreEqual(
            expectedCollecting, errorCount,
            $"Collecting mode is expected to report exactly {expectedCollecting} errors on " +
            $"{relPath} with no spurious cascade diagnostics. See the test's class doc for " +
            $"how to diagnose a count change.\nCaptured stderr:\n{stderr}");
    }

    /// <summary>
    /// Compile a single .p file in the requested mode. Mirrors the helper
    /// in Phase1DormancyTest but kept local so this fixture can evolve
    /// independently as Phase 2 / 3 stabilise.
    /// </summary>
    private static (int exitCode, string stderr, int errorCount) CompileFile(
        string pFile, bool continueOnError)
    {
        var stdoutWriter = new StringWriter();
        var stderrWriter = new StringWriter();
        var output = new CapturingOutput(stdoutWriter, stderrWriter);

        var scratchDir = Directory.CreateDirectory(Path.Combine(
            Constants.ScratchParentDirectory,
            "MultiErrorAcceptance",
            (continueOnError ? "collecting_" : "strict_") + Path.GetFileNameWithoutExtension(pFile)));

        var config = new CompilerConfiguration(
            output,
            scratchDir,
            new List<CompilerOutput> { CompilerOutput.PChecker },
            new List<string> { pFile },
            Path.GetFileNameWithoutExtension(pFile),
            // See Phase1DormancyTest for why projectRoot must be non-null —
            // PCheckerCodeGenerator.Compile NREs otherwise. Multi-error
            // tests stop at type-check (HasErrors is set, Compiler.cs
            // returns early before backend), so this is defence in depth.
            projectRoot: scratchDir);

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
            stderrWriter.WriteLine($"[Test harness caught uncaught {e.GetType().Name}:] {e.Message}");
            stderrWriter.WriteLine(e.StackTrace);
            exitCode = -1;
        }

        var stderr = stderrWriter.ToString();
        var errorCount = CountOccurrences(stderr, "[Error:]") + CountOccurrences(stderr, "[Parser Error:]");
        return (exitCode, stderr, errorCount);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(needle)) return 0;
        var count = 0;
        var idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) != -1)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }

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

        public void WriteFile(CompiledFile file) { /* drop */ }
        public void WriteError(string msg) => stderr.WriteLine(msg);
        public void WriteInfo(string msg) => stdout.WriteLine(msg);
        public void WriteWarning(string msg) => stderr.WriteLine(msg);
    }
}
