using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private Dictionary<string, Dictionary<IPExpr, Expr>> compiled = [];
        private Dictionary<string, Sort> sorts = [];
        private int numQueries = 0;
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
                case SequenceType seqType:
                {
                    return context.MkArraySort(context.IntSort, ToZ3Sort(seqType.ElementType));
                }
                case NamedTupleType namedTupleType:
                {
                    if (sorts.TryGetValue(namedTupleType.OriginalRepresentation, out Sort result))
                    {
                        return result;
                    }
                    List<Symbol> fieldNames = [];
                    List<Sort> fieldSorts = [];
                    foreach (var field in namedTupleType.Fields)
                    {
                        fieldNames.Add(context.MkSymbol(field.Name));
                        fieldSorts.Add(ToZ3Sort(field.Type));
                    }
                    var sort = context.MkTupleSort(context.MkSymbol(namedTupleType.OriginalRepresentation), fieldNames.ToArray(), fieldSorts.ToArray());
                    sorts[namedTupleType.OriginalRepresentation] = sort;
                    return sort;
                }
            }
            throw new Exception($"Unsupported type: {type.CanonicalRepresentation} ({type})");
        }

        private Expr IPExprToSMT(string key, IPExpr e)
        {
            if (compiled[key].TryGetValue(e, out Expr value))
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
                    if (compiled[key].TryGetValue(varAccess, out Expr varExpr))
                    {
                        return varExpr;
                    }
                    var sort = ToZ3Sort(varAccess.Variable.Type);
                    var v = context.MkConst(varAccess.Variable.Name, sort);
                    compiled[key][varAccess] = v;
                    return v;
                }
                case FunCallExpr funCall:
                {
                    if (funCall.Function.Name != "index" && funCall.Function.Name != "size")
                    {
                        throw new Exception($"Unsupported function call: {funCall.Function.Name}");
                    }
                    var arg = funCall.Arguments[0].GetHashCode();
                    var name = $"{funCall.Function.Name}_of_{arg}";
                    var indexVar = context.MkConst(name, context.IntSort);
                    // indices and sizes should be >= 0
                    solver.Assert(context.MkGe((IntExpr)indexVar, context.MkInt(0)));
                    compiled[key][funCall] = indexVar;
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
                    if (compiled[key].TryGetValue(namedTupleAccessExpr, out Expr tupAccessVar))
                    {
                        return tupAccessVar;
                    }
                    var tupVar = context.MkConst(codegen.GetRepr(e), ToZ3Sort(namedTupleAccessExpr.Type));
                    compiled[key][e] = tupVar;
                    return tupVar;
                }
                case TupleAccessExpr tupleAccessExpr:
                {
                    var tupVar = context.MkConst(codegen.GetRepr(e), ToZ3Sort(tupleAccessExpr.Type));
                    compiled[key][e] = tupVar;
                    return tupVar;
                }
                case UnaryOpExpr unaryOpExpr:
                {
                    var arg = IPExprToSMT(key, unaryOpExpr.SubExpr);
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
                    var lhs = IPExprToSMT(key, binOpExpr.Lhs);
                    var rhs = IPExprToSMT(key, binOpExpr.Rhs);
                    // Console.WriteLine($"lhs: {lhs}, rhs: {rhs}");
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

        private bool CheckImpliesZ3(BoolExpr lhs, BoolExpr rhs)
        {
            // check lhs -> rhs is a tautology
            var obj = context.MkNot(context.MkImplies(lhs, rhs));
            solver.Assert(obj);
            var result = solver.Check();
            bool r = result == Status.UNSATISFIABLE;
            return r;
        }

        public bool CheckImplies(string k, IEnumerable<IPExpr> lhs, IEnumerable<IPExpr> rhs)
        {
            solver.Push();
            if (!compiled.ContainsKey(k))
            {
                compiled[k] = new(new ASTComparer());
            }
            var lhsClauses = lhs.Select(x => IPExprToSMT(k, x)).Cast<BoolExpr>().ToArray();
            var rhsClauses = rhs.Select(x => IPExprToSMT(k, x)).Cast<BoolExpr>().ToArray();
            var lhsZ3 = context.MkAnd(lhsClauses);
            var rhsZ3 = context.MkAnd(rhsClauses);
            var r = CheckImpliesZ3(lhsZ3, rhsZ3);
            solver.Pop();
            return r;
        }

        public bool CheckImplies(string k, IEnumerable<string> lhs, IEnumerable<string> rhs, Dictionary<string, IPExpr> parsedP, Dictionary<string, IPExpr> parsedQ)
        {
            if (CheckCache(k, lhs, rhs, out bool cachedResult))
            {
                return cachedResult;
            }
            numQueries++;
            if (numQueries > 1000)
            {
                solver.Reset();
                numQueries = 0;
            }
            if (!compiled.ContainsKey(k))
            {
                compiled[k] = new(new ASTComparer());
            }
            solver.Push();
            IPExpr getExpr(string repr) => parsedP.TryGetValue(repr, out IPExpr value) ? value : parsedQ[repr];
            var lhsClauses = lhs.Select(x => IPExprToSMT(k, getExpr(x))).Cast<BoolExpr>().ToArray();
            var rhsClauses = rhs.Select(x => IPExprToSMT(k, getExpr(x))).Cast<BoolExpr>().ToArray();
            var lhsZ3 = context.MkAnd(lhsClauses);
            var rhsZ3 = context.MkAnd(rhsClauses);
            var r = CheckImpliesZ3(lhsZ3, rhsZ3);
            if (!cachedQueries.ContainsKey(k))
            {
                cachedQueries[k] = [];
            }
            cachedQueries[k].Add((new HashSet<string>(lhs), new HashSet<string>(rhs), r));
            solver.Pop();
            return r;
        } 
    }
}