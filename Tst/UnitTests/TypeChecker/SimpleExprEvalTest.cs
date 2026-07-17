using System.Collections.Generic;
using NUnit.Framework;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;

namespace UnitTests.TypeChecker;

[TestFixture]
[TestOf(typeof(SimpleExprEval))]
public class SimpleExprEvalTest
{
    private static readonly Dictionary<Variable, IPExpr> EmptyStore = new();

    private static int EvalInt(BinOpType op, int lhs, int rhs)
    {
        var expr = new BinOpExpr(null, op, new IntLiteralExpr(lhs), new IntLiteralExpr(rhs));
        return ((IntLiteralExpr)SimpleExprEval.Eval(EmptyStore, expr)).Value;
    }

    private static bool EvalBool(BinOpType op, int lhs, int rhs)
    {
        var expr = new BinOpExpr(null, op, new IntLiteralExpr(lhs), new IntLiteralExpr(rhs));
        return ((BoolLiteralExpr)SimpleExprEval.Eval(EmptyStore, expr)).Value;
    }

    // 6 and 7 are chosen so every arithmetic operator yields a distinct result
    // (Add=13, Sub=-1, Mul=42, Div=0, Mod=6). This is what makes the test able to
    // catch a copy-paste operator swap such as Mul folding to lhs + rhs (=13).
    [Test]
    public void TestArithmeticOperators()
    {
        Assert.AreEqual(13, EvalInt(BinOpType.Add, 6, 7));
        Assert.AreEqual(-1, EvalInt(BinOpType.Sub, 6, 7));
        Assert.AreEqual(42, EvalInt(BinOpType.Mul, 6, 7), "Mul must multiply, not add");
        Assert.AreEqual(3, EvalInt(BinOpType.Div, 7, 2));
        Assert.AreEqual(1, EvalInt(BinOpType.Mod, 7, 2));
    }

    [Test]
    public void TestComparisonOperators()
    {
        Assert.IsTrue(EvalBool(BinOpType.Eq, 5, 5));
        Assert.IsFalse(EvalBool(BinOpType.Neq, 5, 5));
        Assert.IsTrue(EvalBool(BinOpType.Lt, 3, 4));
        Assert.IsTrue(EvalBool(BinOpType.Le, 4, 4));
        Assert.IsTrue(EvalBool(BinOpType.Gt, 5, 4));
        Assert.IsTrue(EvalBool(BinOpType.Ge, 4, 4));
    }

    [Test]
    public void TestNestedArithmetic()
    {
        // (2 * 3) + 4 == 10  — guards the Mul path when nested under another op.
        var mul = new BinOpExpr(null, BinOpType.Mul, new IntLiteralExpr(2), new IntLiteralExpr(3));
        var add = new BinOpExpr(null, BinOpType.Add, mul, new IntLiteralExpr(4));
        Assert.AreEqual(10, ((IntLiteralExpr)SimpleExprEval.Eval(EmptyStore, add)).Value);
    }
}
