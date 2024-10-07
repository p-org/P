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
    public class Z3Wrapper
    {
        private readonly Solver solver;
        private readonly Context context;
        private readonly PInferPredicateGenerator codegen;
        private readonly Dictionary<string, Dictionary<string, Expr>> Enums;
        private readonly Dictionary<string, EnumSort> EnumSorts;
        private Dictionary<string, List<(HashSet<string>, HashSet<string>, bool)>> cachedQueries = [];
        public Z3Wrapper(Scope globalScope, PInferPredicateGenerator codegen)
        {
            context = new Context();
            solver = context.MkSolver();
            this.codegen = codegen;
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
                case PermissionType _:
                {
                    return context.IntSort;
                }
            }
            throw new Exception($"Unsupported type: {type.CanonicalRepresentation} ({type})");
        }

        private Expr IPExprToSMT(IPExpr e, Dictionary<IPExpr, Expr> compiled)
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
                    if (compiled.TryGetValue(varAccess, out Expr varExpr))
                    {
                        return varExpr;
                    }
                    Console.WriteLine($"Creating variable: {varAccess.Variable.Name}");
                    var sort = ToZ3Sort(varAccess.Variable.Type);
                    var v = context.MkConst(varAccess.Variable.Name, sort);
                    compiled[varAccess] = v;
                    return v;
                }
                case FunCallExpr funCall:
                {
                    if (funCall.Function.Name != "index")
                    {
                        throw new Exception($"Unsupported function call: {funCall.Function.Name}");
                    }
                    var arg = (VariableAccessExpr) funCall.Arguments[0];
                    var name = $"index_of_{arg.Variable.Name}";
                    if (compiled.TryGetValue(arg, out Expr v))
                    {
                        return v;
                    }
                    var indexVar = context.MkConst(name, context.IntSort);
                    compiled[arg] = indexVar;
                    return indexVar;
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
                    if (compiled.TryGetValue(namedTupleAccessExpr, out Expr tupAccessVar))
                    {
                        return tupAccessVar;
                    }
                    var tupVar = context.MkConst(codegen.GetRepr(e), ToZ3Sort(namedTupleAccessExpr.Type));
                    compiled[e] = tupVar;
                    return tupVar;
                }
                case TupleAccessExpr tupleAccessExpr:
                {
                    var tupVar = context.MkConst(codegen.GetRepr(e), ToZ3Sort(tupleAccessExpr.Type));
                    compiled[e] = tupVar;
                    return tupVar;
                }
                case UnaryOpExpr unaryOpExpr:
                {
                    var arg = IPExprToSMT(unaryOpExpr.SubExpr, compiled);
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
                    var lhs = IPExprToSMT(binOpExpr.Lhs, compiled);
                    var rhs = IPExprToSMT(binOpExpr.Rhs, compiled);
                    Console.WriteLine($"lhs: {lhs}, rhs: {rhs}");
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

        private bool CheckCache(string k, IEnumerable<string> lhs, IEnumerable<string> rhs, out bool result)
        {
            if (cachedQueries.TryGetValue(k, out List<(HashSet<string>, HashSet<string>, bool)> queries))
            {
                foreach (var (lhsSet, rhsSet, r) in queries)
                {
                    if (lhsSet.SetEquals(lhs) && rhsSet.SetEquals(rhs))
                    {
                        result = r;
                        return true;
                    }
                }
            }
            result = false;
            return false;
        }

        public bool CheckImplies(string k, IEnumerable<string> lhs, IEnumerable<string> rhs, Dictionary<string, IPExpr> parsedP, Dictionary<string, IPExpr> parsedQ)
        {
            if (CheckCache(k, lhs, rhs, out bool cachedResult))
            {
                return cachedResult;
            }
            Dictionary<IPExpr, Expr> compiled = new(new ASTComparer());
            // var lhsZ3 = (BoolExpr) IPExprToSMT(lhs, parsedP[lhs], compiled);
            // var rhsZ3 = (BoolExpr) IPExprToSMT(rhs, parsedQ[rhs], compiled);
            IPExpr getExpr(string repr) => parsedP.TryGetValue(repr, out IPExpr value) ? value : parsedQ[repr];
            var lhsClauses = lhs.Select(x => IPExprToSMT(getExpr(x), compiled)).Cast<BoolExpr>().ToArray();
            var rhsClauses = rhs.Select(x => IPExprToSMT(getExpr(x), compiled)).Cast<BoolExpr>().ToArray();
            var lhsZ3 = context.MkAnd(lhsClauses);
            var rhsZ3 = context.MkAnd(rhsClauses);
            solver.Push();
            // check lhs -> rhs is a tautology
            var obj = context.MkNot(context.MkImplies(lhsZ3, rhsZ3));
            // Console.WriteLine($"P: {string.Join(", ", lhs)}");
            // Console.WriteLine($"Q: {string.Join(", ", rhs)}");
            // Console.WriteLine($"Checking: {obj}");
            solver.Assert(obj);
            var result = solver.Check();
            // Console.WriteLine($"Result: {result}");
            // should be UNSAT
            bool r = result == Status.UNSATISFIABLE;
            solver.Pop();
            if (!cachedQueries.ContainsKey(k))
            {
                cachedQueries[k] = [];
            }
            cachedQueries[k].Add((new HashSet<string>(lhs), new HashSet<string>(rhs), r));
            return r;
        } 
    }
}