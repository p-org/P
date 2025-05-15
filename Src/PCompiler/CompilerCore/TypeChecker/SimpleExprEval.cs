using System;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;

namespace Plang.Compiler.TypeChecker;

public abstract class SimpleExprEval
{
    private static int ForceInt(IPExpr expr)
    {
        return expr switch
        {
            IntLiteralExpr intLiteralExpr => intLiteralExpr.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(expr))
        };
    }

    public static bool ForceBool(IPExpr expr)
    {
        return expr switch
        {
            BoolLiteralExpr boolLiteralExpr => boolLiteralExpr.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(expr))
        };
    }

    private static IPExpr EvalEq(IPExpr lhs, IPExpr rhs)
    {
        return (lhs, rhs) switch
        {
            (BoolLiteralExpr, BoolLiteralExpr) =>
                new BoolLiteralExpr(ForceBool(lhs) == ForceBool(rhs)),
            (IntLiteralExpr, IntLiteralExpr) =>
                new BoolLiteralExpr(ForceInt(lhs) == ForceInt(rhs)),
            _ => throw new ArgumentOutOfRangeException(nameof(lhs))
        };
    }

    private static IPExpr EvalNEq(IPExpr lhs, IPExpr rhs)
    {
        return (lhs, rhs) switch
        {
            (BoolLiteralExpr, BoolLiteralExpr) =>
                new BoolLiteralExpr(ForceBool(lhs) != ForceBool(rhs)),
            (IntLiteralExpr, IntLiteralExpr) =>
                new BoolLiteralExpr(ForceInt(lhs) != ForceInt(rhs)),
            _ => throw new ArgumentOutOfRangeException(nameof(lhs))
        };
    }

    public static IPExpr Eval(Dictionary<Variable, IPExpr> store, IPExpr expr)
    {
        switch (expr)
        {
            case BoolLiteralExpr:
            case IntLiteralExpr:
                return expr;
            case UnaryOpExpr unaryOpExpr:
            {
                var subExpr = Eval(store, unaryOpExpr.SubExpr);
                return unaryOpExpr.Operation switch
                {
                    UnaryOpType.Negate => new IntLiteralExpr(-ForceInt(subExpr)),
                    UnaryOpType.Not => new BoolLiteralExpr(!ForceBool(subExpr)),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            case BinOpExpr binOpExpr:
            {
                var lhs = Eval(store, binOpExpr.Lhs);
                var rhs = Eval(store, binOpExpr.Rhs);
                return binOpExpr.Operation switch
                {
                    BinOpType.Add => new IntLiteralExpr(ForceInt(lhs) + ForceInt(rhs)),
                    BinOpType.Sub => new IntLiteralExpr(ForceInt(lhs) - ForceInt(rhs)),
                    BinOpType.Mul => new IntLiteralExpr(ForceInt(lhs) + ForceInt(rhs)),
                    BinOpType.Div => new IntLiteralExpr(ForceInt(lhs) / ForceInt(rhs)),
                    BinOpType.Mod => new IntLiteralExpr(ForceInt(lhs) % ForceInt(rhs)),
                    BinOpType.Eq => EvalEq(lhs, rhs),
                    BinOpType.Neq => EvalNEq(lhs, rhs),
                    BinOpType.Lt => new BoolLiteralExpr(ForceInt(lhs) < ForceInt(rhs)),
                    BinOpType.Le => new BoolLiteralExpr(ForceInt(lhs) <= ForceInt(rhs)),
                    BinOpType.Gt => new BoolLiteralExpr(ForceInt(lhs) > ForceInt(rhs)),
                    BinOpType.Ge => new BoolLiteralExpr(ForceInt(lhs) >= ForceInt(rhs)),
                    BinOpType.And => new BoolLiteralExpr(ForceBool(lhs) && ForceBool(rhs)),
                    BinOpType.Or => new BoolLiteralExpr(ForceBool(lhs) || ForceBool(rhs)),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            case IVariableRef variableRef:
                return store[variableRef.Variable];
            default:
                throw new ArgumentOutOfRangeException(nameof(expr));
        }
    }
}