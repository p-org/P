using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;

namespace Plang.Compiler.TypeChecker;

public class ConstraintVariableCollector
{

    public static HashSet<Variable> FindVariablesRecursive(IPExpr expr)
    {
        switch (expr)
        {
            case BinOpExpr binOp:
                return FindVariablesRecursive(binOp.Lhs).Union(FindVariablesRecursive(binOp.Rhs)).ToHashSet();
            case NamedTupleAccessExpr namedTupleAccessExpr:
                return FindVariablesRecursive(namedTupleAccessExpr.SubExpr);
            case VariableAccessExpr variableAccessExpr:
                return new HashSet<Variable>() { variableAccessExpr.Variable };
            case EnumElemRefExpr:
            case StringExpr:
            case IntLiteralExpr:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(expr));
        }

        return new HashSet<Variable>();
    }
}