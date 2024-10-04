using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Z3;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.PInfer
{
    class Z3Wrapper
    {
        private readonly Solver solver;
        private readonly Context context;
        private readonly Dictionary<string, Dictionary<string, Expr>> Enums;
        private readonly Dictionary<string, EnumSort> EnumSorts;
        public Z3Wrapper(Scope globalScope)
        {
            context = new Context();
            solver = context.MkSolver();
            Enums = [];
            EnumSorts = [];
            foreach (var enumDecls in globalScope.Enums)
            {
                var name = context.MkSymbol(enumDecls.Name);
                List<Symbol> symbols = [];
                foreach (var enumElem in enumDecls.Values)
                {
                    symbols.Add(context.MkSymbol(enumElem.Name));
                }
                EnumSort sort = context.MkEnumSort(name, [.. symbols]);
                Enums[enumDecls.Name] = [];
                EnumSorts[enumDecls.Name] = sort;
                foreach (var (val, i) in enumDecls.Values.Select((x, i) => (x, i)))
                {
                    Enums[enumDecls.Name][val.Name] = sort.Consts[i];
                }
            }
        }

        private Sort ToZ3Sort(PLanguageType type)
        {
            switch (type.Canonicalize())
            {
                case EnumType enumType:
                {
                    return EnumSorts[enumType.EnumDecl.Name];
                }
                case PrimitiveType primitiveType:
                {
                    if (primitiveType == PrimitiveType.Bool)
                    {
                        return context.BoolSort;
                    }
                    else if (primitiveType == PrimitiveType.String || primitiveType == PrimitiveType.Machine)
                    {
                        // machines are converted to their unique names
                        return context.StringSort;
                    }
                    else if (primitiveType == PrimitiveType.Float)
                    {
                        return context.RealSort;
                    }
                    else if (primitiveType == PrimitiveType.Int)
                    {
                        return context.IntSort;
                    }
                    break;
                }
            }
            throw new Exception($"Unsupported type: {type.CanonicalRepresentation}");
        }

        private Expr IPExprToSMT(string repr, IPExpr e, Dictionary<IPExpr, Expr> compiled)
        {
            if (compiled.TryGetValue(e, out Expr value))
            {
                return value;
            }
            switch (e)
            {
                case EnumElemRefExpr enumRef:
                {
                    return Enums[enumRef.Value.ParentEnum.Name][enumRef.Value.Name];
                }
                case VariableAccessExpr varAccess:
                {
                    var sort = ToZ3Sort(varAccess.Variable.Type);
                    var v = context.MkConst(varAccess.Variable.Name, sort);
                    compiled[varAccess] = v;
                    return v;
                }
                case BoolLiteralExpr boolLit:
                {
                    return context.MkBool(boolLit.Value);
                }
                case IntLiteralExpr intLit:
                {
                    return context.MkInt(intLit.Value);
                }
                case FloatLiteralExpr floatLit:
                {
                    return context.MkReal(floatLit.Value.ToString());
                }
                case NamedTupleAccessExpr namedTupleAccessExpr:
                {
                    var tupVar = context.MkConst(repr, ToZ3Sort(namedTupleAccessExpr.Type));
                    compiled[e] = tupVar;
                    return tupVar;
                }
                case TupleAccessExpr tupleAccessExpr:
                {
                    var tupVar = context.MkConst(repr, ToZ3Sort(tupleAccessExpr.Type));
                    compiled[e] = tupVar;
                    return tupVar;
                }
                case UnaryOpExpr unaryOpExpr:
                {
                    var arg = IPExprToSMT(repr, unaryOpExpr.SubExpr, compiled);
                    switch (unaryOpExpr.Operation)
                    {
                        case UnaryOpType.Not:
                        {
                            return context.MkNot((BoolExpr)arg);
                        }
                        case UnaryOpType.Negate:
                        {
                            return context.MkUnaryMinus((ArithExpr)arg);
                        }
                    }
                    break;
                }
                case BinOpExpr binOpExpr:
                {
                    var lhs = IPExprToSMT(repr, binOpExpr.Lhs, compiled);
                    var rhs = IPExprToSMT(repr, binOpExpr.Rhs, compiled);
                    switch (binOpExpr.Operation)
                    {
                        case BinOpType.Add:
                        {
                            return context.MkAdd((ArithExpr)lhs, (ArithExpr)rhs);
                        }
                        case BinOpType.Sub:
                        {
                            return context.MkSub((ArithExpr)lhs, (ArithExpr)rhs);
                        }
                        case BinOpType.Mul:
                        {
                            return context.MkMul((ArithExpr)lhs, (ArithExpr)rhs);
                        }
                        case BinOpType.Div:
                        {
                            return context.MkDiv((ArithExpr)lhs, (ArithExpr)rhs);
                        }
                        case BinOpType.Mod:
                        {
                            return context.MkMod((IntExpr)lhs, (IntExpr)rhs);
                        }
                        case BinOpType.And:
                        {
                            return context.MkAnd((BoolExpr)lhs, (BoolExpr)rhs);
                        }
                        case BinOpType.Or:
                        {
                            return context.MkOr((BoolExpr)lhs, (BoolExpr)rhs);
                        }
                        case BinOpType.Eq:
                        {
                            return context.MkEq(lhs, rhs);
                        }
                        case BinOpType.Neq:
                        {
                            return context.MkNot(context.MkEq(lhs, rhs));
                        }
                        case BinOpType.Lt:
                        {
                            return context.MkLt((ArithExpr)lhs, (ArithExpr)rhs);
                        }
                        case BinOpType.Le:
                        {
                            return context.MkLe((ArithExpr)lhs, (ArithExpr)rhs);
                        }
                        case BinOpType.Gt:
                        {
                            return context.MkGt((ArithExpr)lhs, (ArithExpr)rhs);
                        }
                        case BinOpType.Ge:
                        {
                            return context.MkGe((ArithExpr)lhs, (ArithExpr)rhs);
                        }
                    }
                    break;
                }
            }
            throw new Exception($"Unsupported expression: {e}");
        }

        public bool CheckImplies(IEnumerable<string> lhs, IEnumerable<string> rhs, Dictionary<string, IPExpr> parsedP, Dictionary<string, IPExpr> parsedQ)
        {
            Dictionary<IPExpr, Expr> compiled = new(new ASTComparer());
            // var lhsZ3 = (BoolExpr) IPExprToSMT(lhs, parsedP[lhs], compiled);
            // var rhsZ3 = (BoolExpr) IPExprToSMT(rhs, parsedQ[rhs], compiled);
            var lhsClauses = lhs.Select(x => IPExprToSMT(x, parsedP[x], compiled)).Cast<BoolExpr>().ToArray();
            var rhsClauses = rhs.Select(x => IPExprToSMT(x, parsedQ[x], compiled)).Cast<BoolExpr>().ToArray();
            var lhsZ3 = context.MkAnd(lhsClauses);
            var rhsZ3 = context.MkAnd(rhsClauses);
            solver.Push();
            // check lhs -> rhs is a tautology
            var obj = context.MkNot(context.MkImplies(lhsZ3, rhsZ3));
            solver.Assert(obj);
            var result = solver.Check();
            // should be UNSAT
            bool r = result == Status.UNSATISFIABLE;
            solver.Pop();
            return r;
        } 
    }
}