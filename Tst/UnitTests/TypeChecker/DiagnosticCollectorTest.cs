using System;
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
        // The cascade-suppression contract: ErrorType claims compatibility
        // with every other type, so downstream IsAssignableFrom/IsSameTypeAs
        // checks transparently pass and don't emit new diagnostics.
        Assert.IsTrue(ErrorType.Instance.IsAssignableFrom(PrimitiveType.Int));
        Assert.IsTrue(ErrorType.Instance.IsAssignableFrom(PrimitiveType.Bool));
        Assert.IsTrue(ErrorType.Instance.IsAssignableFrom(PrimitiveType.String));
        Assert.IsTrue(ErrorType.Instance.IsSameTypeAs(PrimitiveType.Int));
        // And it Canonicalize()s to itself (no infinite recursion).
        Assert.AreSame(ErrorType.Instance, ErrorType.Instance.Canonicalize());
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
}
