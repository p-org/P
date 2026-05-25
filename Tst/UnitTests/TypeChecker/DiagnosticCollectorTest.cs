using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using NUnit.Framework;
using Plang.Compiler;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace UnitTests.TypeChecker;

/// <summary>
/// Smoke tests for the Phase-1 diagnostic-collection scaffolding. None of
/// these exercise the type checker — they verify the contract of
/// <see cref="IDiagnosticCollector"/>, <see cref="ErrorType"/>, and
/// <see cref="ErrorExpr"/> directly. Phase 2 / 3 will add tests that
/// actually drive the compiler in collecting mode.
/// </summary>
[TestFixture]
public class DiagnosticCollectorTest
{
    [Test]
    public void StrictMode_RethrowsImmediately()
    {
        var collector = new DefaultDiagnosticCollector(continueOnError: false);
        Assert.IsFalse(collector.ContinueOnError);
        Assert.IsFalse(collector.HasErrors);

        var ex = new TranslationException("boom");
        var thrown = Assert.Throws<TranslationException>(() => collector.Report(ex));
        Assert.AreSame(ex, thrown);
        // Strict mode never accumulates.
        Assert.IsFalse(collector.HasErrors);
        Assert.AreEqual(0, collector.Diagnostics.Count);
    }

    [Test]
    public void CollectingMode_AppendsInOrder()
    {
        var collector = new DefaultDiagnosticCollector(continueOnError: true);
        Assert.IsTrue(collector.ContinueOnError);
        Assert.IsFalse(collector.HasErrors);

        var e1 = new TranslationException("first");
        var e2 = new TranslationException("second");
        collector.Report(e1);
        collector.Report(e2);

        Assert.IsTrue(collector.HasErrors);
        Assert.AreEqual(2, collector.Diagnostics.Count);
        Assert.AreSame(e1, collector.Diagnostics[0]);
        Assert.AreSame(e2, collector.Diagnostics[1]);
    }

    [Test]
    public void Report_NullThrowsArgumentNull()
    {
        var collector = new DefaultDiagnosticCollector(continueOnError: true);
        Assert.Throws<ArgumentNullException>(() => collector.Report(null));
    }

    [Test]
    public void ErrorType_IsAssignableFromEverything_SuppressesCascade()
    {
        // ErrorType claims compatibility on its own IsAssignableFrom override
        // (covers the asymmetric LHS-is-error case).
        Assert.IsTrue(ErrorType.Instance.IsAssignableFrom(PrimitiveType.Int));
        Assert.IsTrue(ErrorType.Instance.IsAssignableFrom(PrimitiveType.Bool));
        Assert.IsTrue(ErrorType.Instance.IsAssignableFrom(PrimitiveType.String));
        // And Canonicalize()s to itself (no infinite recursion).
        Assert.AreSame(ErrorType.Instance, ErrorType.Instance.Canonicalize());
    }

    [Test]
    public void IsSameTypeAs_IsSymmetricForErrorType()
    {
        // Symmetric cascade suppression: PLanguageType.IsSameTypeAs has an
        // explicit short-circuit that fires regardless of which side holds
        // the sentinel. The naive IsAssignableFrom-and-IsAssignableFrom
        // composition would only handle one direction (since e.g.
        // PrimitiveType.Int.IsAssignableFrom(ErrorType) returns false), so
        // this test guards against regressions in that base-class short-circuit.
        Assert.IsTrue(ErrorType.Instance.IsSameTypeAs(PrimitiveType.Int));
        Assert.IsTrue(PrimitiveType.Int.IsSameTypeAs(ErrorType.Instance));
        Assert.IsTrue(ErrorType.Instance.IsSameTypeAs(PrimitiveType.Bool));
        Assert.IsTrue(PrimitiveType.Bool.IsSameTypeAs(ErrorType.Instance));
        Assert.IsTrue(ErrorType.Instance.IsSameTypeAs(ErrorType.Instance));
    }

    [Test]
    public void ErrorExpr_TypeIsErrorType()
    {
        var expr = new ErrorExpr(ParserRuleContext.EmptyContext);
        Assert.AreSame(ErrorType.Instance, expr.Type);
        Assert.AreSame(ParserRuleContext.EmptyContext, expr.SourceLocation);
    }

    [Test]
    public void ErrorExpr_DoesNotImplementIExprTerm()
    {
        // Phase-1 invariant: ErrorExpr must NOT satisfy IExprTerm, so the
        // IR transformer's post-typecheck stage trips a clear cast failure
        // rather than silently corrupting backends if one ever leaks past
        // type-checking.
        var expr = new ErrorExpr(ParserRuleContext.EmptyContext);
        Assert.IsNotInstanceOf<IExprTerm>(expr);
    }

    [Test]
    public void Handler_ExposesSameCollectorInstance()
    {
        // ICompilerConfiguration.Diagnostics and Handler.Diagnostics must
        // be the same instance so visitors that hold either reference see
        // the same collected diagnostics.
        var config = new CompilerConfiguration();
        Assert.IsNotNull(config.Diagnostics);
        Assert.IsNotNull(config.Handler.Diagnostics);
        Assert.AreSame(config.Diagnostics, config.Handler.Diagnostics);
    }

    [Test]
    public void Handler_TwoArgCtor_ThrowsOnNullCollector()
    {
        // Guards the shared-instance invariant: silently substituting a fresh
        // collector here would mean ICompilerConfiguration.Diagnostics and
        // Handler.Diagnostics end up as different instances, and visitors
        // reaching the handler-side collector would never have their reports
        // surfaced by Compiler.cs's flush pass.
        Assert.Throws<ArgumentNullException>(
            () => new DefaultTranslationErrorHandler(new DefaultLocationResolver(), null));
    }

    [Test]
    public void Diagnostics_IsReadOnlyWrapper_DowncastMutationFails()
    {
        // Even though the property type is IReadOnlyList, the returned wrapper
        // must reject mutation if a caller downcasts. Otherwise the collector's
        // ordering / HasErrors guarantees can be violated externally.
        var collector = new DefaultDiagnosticCollector(continueOnError: true);
        collector.Report(new TranslationException("first"));

        var diagnostics = collector.Diagnostics;
        if (diagnostics is IList<Exception> mutable)
        {
            // ReadOnlyCollection<T> implements IList<T> but throws on mutators.
            Assert.Throws<NotSupportedException>(
                () => mutable.Add(new TranslationException("snuck-in")));
            Assert.Throws<NotSupportedException>(() => mutable.Clear());
        }
        // Either way, the underlying state stayed at 1.
        Assert.AreEqual(1, collector.Diagnostics.Count);
    }

    [Test]
    public void Diagnostics_IsLiveView_NotSnapshot()
    {
        // The ReadOnlyCollection<T> wrapper from List.AsReadOnly() is a live
        // view of the underlying list: callers holding a reference see items
        // appended after the property read. Compiler.cs's flush pass at end
        // of type-checking depends on this — it reads Diagnostics once and
        // iterates, expecting to see everything reported up to that point.
        // A future "defensive copy" change here would silently lose late
        // diagnostics, so guard explicitly.
        var collector = new DefaultDiagnosticCollector(continueOnError: true);
        var liveView = collector.Diagnostics;
        Assert.AreEqual(0, liveView.Count);

        collector.Report(new TranslationException("late arrival"));

        Assert.AreEqual(1, liveView.Count);
        Assert.AreEqual("late arrival", liveView[0].Message);
    }

    // Env var parsing — see CompilerConfiguration.ReadContinueOnErrorEnvVar.
    // Tested via the public ContinueOnError property after construction so we
    // don't have to mark the private helper internal. Each case saves and
    // restores the var to keep test isolation.
    [TestCase("1", true)]
    [TestCase("true", true)]
    [TestCase("True", true)]
    [TestCase("TRUE", true)]
    [TestCase("yes", true)]            // any non-"0"/"false" non-empty value wins
    [TestCase("anything-else", true)]
    [TestCase("0", false)]
    [TestCase("false", false)]
    [TestCase("False", false)]
    [TestCase("FALSE", false)]
    [TestCase("", false)]              // empty string treated as unset
    [TestCase("   ", false)]           // whitespace-only treated as unset
    [TestCase(null, false)]            // unset
    public void ContinueOnError_ReadsEnvVar(string envValue, bool expected)
    {
        const string envName = "P_COMPILER_COLLECT_ERRORS";
        var saved = Environment.GetEnvironmentVariable(envName);
        try
        {
            Environment.SetEnvironmentVariable(envName, envValue);
            var config = new CompilerConfiguration();
            Assert.AreEqual(expected, config.ContinueOnError,
                $"P_COMPILER_COLLECT_ERRORS={envValue ?? "(unset)"}");
            // The collector's mode must agree with the config's flag.
            Assert.AreEqual(expected, config.Diagnostics.ContinueOnError);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envName, saved);
        }
    }
}
