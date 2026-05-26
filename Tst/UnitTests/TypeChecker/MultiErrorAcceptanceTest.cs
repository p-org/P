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
/// Phase 2 acceptance test: a P file with multiple independent errors must
/// surface ALL of them in collecting mode, with no spurious cascade
/// diagnostics. Pins the strict / collecting error counts on a curated
/// input.
///
/// If this test fails after a change to ExprVisitor / StatementVisitor /
/// the TypeCheckingUtils helpers, the most likely cause is one of:
///   - A throw site was added without record-and-continue conversion
///     (collecting count regressed to strict count).
///   - A cascade-suppression check was loosened, letting one upstream
///     error generate downstream "incompatible type" noise (collecting
///     count exceeds the expected).
///   - A new sentinel propagation path was missed (collecting count is
///     between expected and strict; some errors got swallowed).
///
/// When expected counts change intentionally, update the constants below
/// AND add a comment in MultipleErrors.p explaining what changed.
/// </summary>
[TestFixture]
public class MultiErrorAcceptanceTest
{
    // See MultipleErrors.p for the 4 independent errors this test asserts.
    private const int ExpectedStrictErrorCount = 1;
    private const int ExpectedCollectingErrorCount = 4;

    private static string FindMultipleErrorsFile()
    {
        return Path.Combine(
            Constants.TestDirectory,
            "RegressionTests", "Feature3Exprs", "StaticError",
            "MultipleErrors", "MultipleErrors.p");
    }

    [Test]
    public void StrictMode_ReportsExactlyOneErrorOnMultiErrorFile()
    {
        var (exitCode, _, errorCount) = CompileFile(FindMultipleErrorsFile(), continueOnError: false);
        Assert.AreEqual(1, exitCode, "strict mode must exit with code 1 on a static error");
        Assert.AreEqual(
            ExpectedStrictErrorCount, errorCount,
            $"Strict mode is expected to report exactly {ExpectedStrictErrorCount} error (it aborts on the " +
            "first throw). Phase 2 must not change this behavior in the default mode.");
    }

    [Test]
    public void CollectingMode_ReportsAllIndependentErrors()
    {
        var (exitCode, stderr, errorCount) = CompileFile(FindMultipleErrorsFile(), continueOnError: true);
        Assert.AreEqual(1, exitCode, "collecting mode must still exit with code 1 when errors are present");
        Assert.AreEqual(
            ExpectedCollectingErrorCount, errorCount,
            $"Collecting mode is expected to report exactly {ExpectedCollectingErrorCount} errors with no " +
            "spurious cascade diagnostics. See the test's class doc for how to diagnose a count change.\n" +
            $"Captured stderr:\n{stderr}");
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
            Path.GetFileNameWithoutExtension(pFile));

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
            stderrWriter.WriteLine($"[Test harness caught uncaught exception:] {e.Message}");
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
