using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.PInfer
{

    internal class DropException(string msg) : Exception($"[Drop] {msg}")
    {
    }
    internal class StepbackException(string msg) : Exception($"[StepBack] {msg}")
    {
    }

    internal class InvExprVisitor : PParserBaseVisitor<IPExpr>
    {
        readonly ICompilerConfiguration config;
        readonly Dictionary<string, IPExpr> ReprToTerm;
        readonly Scope GlobalScope;
        readonly CongruenceClosure CC;
        readonly Dictionary<string, VariableAccessExpr> SpeicalConstants;

        public InvExprVisitor(ICompilerConfiguration cfg, Scope globalScope, CongruenceClosure cc, PEvent configEvent, Dictionary<string, IPExpr> reprToTerm)
        {
            config = cfg;
            ReprToTerm = reprToTerm;
            GlobalScope = globalScope;
            CC = cc;
            SpeicalConstants = [];
            SpeicalConstants.Add("_num_e_exists_", new(null, new Variable("_num_e_exists_", null, VariableRole.Temp)));
            if (configEvent != null)
            {
                foreach (var p in ((NamedTupleType) configEvent.PayloadType).Fields)
                {
                    SpeicalConstants.Add(p.Name, new(null, new Variable(p.Name, null, VariableRole.Field)));
                }
            }
        }

        public void CheckNumExistsConstants(PParser.BinExprContext ctx, IPExpr lhs, BinOpType op, IPExpr rhs)
        {
            if (rhs is VariableAccessExpr exp)
            {
                if (exp.Variable.Name == "_num_e_exists_")
                {
                    CheckNumExistsConstants(ctx, rhs, op, lhs);
                }
            }
            if (TryGetSpecialConstant(lhs, out var lhsExpr))
            {
                if (lhsExpr.Variable.Name == "_num_e_exists_")
                {
                    // rhs must be a special constant or `size`
                    // moreover, ops can only be comparisons
                    if (op.GetKind() == BinOpKind.Comparison || op.GetKind() == BinOpKind.Equality)
                    {
                        if (rhs is IntLiteralExpr lit)
                        {
                            throw new DropException($"_num_e_exists_ compared with plain constants: {ctx.GetText()}");
                        }
                        if (rhs is VariableAccessExpr rhsExpr && !SpeicalConstants.ContainsKey(rhsExpr.Variable.Name))
                        {
                            throw new DropException($"_num_e_exists_ compared with unknown variable: {ctx.GetText()}");
                        }
                        if (rhs is not VariableAccessExpr && rhs.Type != PInferBuiltinTypes.CollectionSize)
                        {
                            throw new DropException($"_num_e_exists_ compared with non-size type: {ctx.GetText()}");
                        }
                    }
                    else
                    {
                        throw new DropException($"_num_e_exists_ on lhs used for non-comparisons: {ctx.GetText()}");
                    }
                }
            }
        }

        public bool TryGetSpecialConstant(IPExpr expr, out VariableAccessExpr accessExpr)
        {
            if (expr is VariableAccessExpr e)
            {
                return SpeicalConstants.TryGetValue(e.Variable.Name, out accessExpr);
            }
            accessExpr = null;
            return false;
        }

        public override IPExpr VisitBinExpr(PParser.BinExprContext ctx)
        {
            var lhs = Visit(ctx.lhs);
            var rhs = Visit(ctx.rhs);
            var op = ctx.op.Text;
            if (lhs == null || rhs == null)
            {
                throw new DropException($"failed to parse lhs/rhs: {ctx.GetText()}");
            }
            var binOps = new Dictionary<string, BinOpType>
            {
                {"*", BinOpType.Mul},
                {"/", BinOpType.Div},
                {"%", BinOpType.Mod},
                {"+", BinOpType.Add},
                {"-", BinOpType.Sub},
                {"<", BinOpType.Lt},
                {"<=", BinOpType.Le},
                {">", BinOpType.Gt},
                {">=", BinOpType.Ge},
                {"==", BinOpType.Eq},
                {"!=", BinOpType.Neq}
            };
            if (!binOps.TryGetValue(op, out var binOp)) {
                throw new DropException($"Op {op} not supported: `{ctx.GetText()}`");
            }
            // first, check whether is in CC
            // Daikon guarantees ths expr is well-typed (over-approximation)
            BinOpExpr expr = new(ctx, binOp, lhs, rhs);
            IPExpr cano = CC.Canonicalize(expr);
            if (cano != null)
            {
               return cano;
            }
            // Next, check for comparison with special constants
            CheckNumExistsConstants(ctx, lhs, binOp, rhs);
            VariableAccessExpr lhsExpr = null;
            VariableAccessExpr rhsExpr = null;
            if (TryGetSpecialConstant(lhs, out lhsExpr) || TryGetSpecialConstant(rhs, out rhsExpr))
            {
                if (binOp.GetKind() == BinOpKind.Numeric)
                {
                    throw new StepbackException($"Special constants used for arithmetics: {ctx.GetText()}");
                }
                if (lhsExpr != null && rhsExpr != null)
                {
                    if (lhsExpr.Variable.Name == "_num_e_exists_" || rhsExpr.Variable.Name == "_num_e_exists_")
                    {
                        return expr;
                    }
                    throw new DropException($"Special constants are all from config events: {ctx.GetText()}");
                }
                if (lhs == null)
                {
                    (lhs, rhs) = (rhs, lhs);
                }
                if (lhsExpr.Variable.Name == "_num_e_exists_")
                {
                    if (rhs.Type == PInferBuiltinTypes.CollectionSize)
                    {
                        return expr;
                    }
                    throw new DropException($"_num_e_exists_ compared with non-size type: {ctx.GetText()}");
                }
                return expr;
            }
            // let constants go
            if ((rhs is IntLiteralExpr || rhs is FloatLiteralExpr) && binOp.GetKind() == BinOpKind.Comparison)
            {
                if (lhs.Type == PInferBuiltinTypes.Index)
                {
                    throw new StepbackException($"Comparing an index with a constant");
                }
                return expr;
            }
            // Next, check whether such comparison is allowed
            if (op == "==" && PInferPredicateGenerator.IsAssignableFrom(lhs.Type, rhs.Type)
                           && PInferPredicateGenerator.IsAssignableFrom(rhs.Type, lhs.Type))
            {
                return expr;
            }
            if (GlobalScope.AllowedBinOps.TryGetValue(binOp, out var allowedOps))
            {
                foreach (var types in allowedOps)
                {
                    if (types.Item1 == lhs.Type && types.Item2 == rhs.Type)
                    {
                        return expr;
                    }
                }
            }
            // if it's a comp op, then check whether there are other comp op defined
            if (GlobalScope.AllowedBinOpsByKind.TryGetValue(binOp.GetKind(), out var allowedOpsByKind))
            {
                // Console.WriteLine($"Look for {lhs.Type} {rhs.Type}");
                foreach (var types in allowedOpsByKind)
                {
                    // Console.WriteLine($"{types.Item1} {types.Item2}");
                    if (PInferPredicateGenerator.SameType(types.Item1, lhs.Type) && PInferPredicateGenerator.SameType(types.Item2, rhs.Type))
                    {
                        return expr;
                    }
                }
            }
            throw new DropException($"BinOp not allowed: lhs=({ctx.lhs.GetText()}) op=({op}) rhs=({ctx.rhs.GetText()})");
        }

        public override IPExpr VisitMapOrSeqLvalue(PParser.MapOrSeqLvalueContext ctx)
        {
            var lvalue = Visit(ctx.lvalue());
            var index = Visit(ctx.expr());
            if (lvalue.Type.Canonicalize() is SequenceType sequenceType)
            {
                return new SeqAccessExpr(ctx, lvalue, index, sequenceType.ElementType);
            }
            throw new DropException($"Unsupported indexing: {ctx.GetText()}");
        }

        public override IPExpr VisitPrimitiveExpr(PParser.PrimitiveExprContext ctx)
        {
            return Visit(ctx.primitive());
        }

        public override IPExpr VisitNamedTupleAccessExpr(PParser.NamedTupleAccessExprContext ctx)
        {
            if (ReprToTerm.TryGetValue(ctx.GetText(), out var expr))
            {
                return expr;
            }
            throw new DropException($"Undefined term: `{ctx.GetText()}`");
        }

        public override IPExpr VisitDecimalFloat(PParser.DecimalFloatContext context)
        {
            var value = double.Parse($"{context.pre?.Text ?? ""}.{context.post.Text}");
            return new FloatLiteralExpr(context, value);
        }

        public override IPExpr VisitPrimitive(PParser.PrimitiveContext ctx)
        {
            if (ctx.iden() != null)
            {
                if (ReprToTerm.TryGetValue(ctx.iden().GetText(), out var expr))
                {
                    return expr;
                }
                if (SpeicalConstants.TryGetValue(ctx.iden().GetText(), out var c))
                {
                    return c;
                }
                throw new DropException($"Undefined variable: `{ctx.iden().GetText()}`");
            }
            if (ctx.floatLiteral() != null)
            {
                return Visit(ctx.floatLiteral());
            }

            if (ctx.BoolLiteral() != null)
            {
                return new BoolLiteralExpr(ctx, ctx.BoolLiteral().GetText().Equals("true"));
            }

            if (ctx.IntLiteral() != null)
            {
                return new IntLiteralExpr(ctx, int.Parse(ctx.IntLiteral().GetText()));
            }
            throw new DropException($"Unsupported primitives: `{ctx.GetText()}`");
        }

        public override IPExpr VisitFunCallExpr(PParser.FunCallExprContext ctx)
        {
            // fun calls should be already known
            if (ReprToTerm.TryGetValue(ctx.GetText(), out var expr))
            {
                return expr;
            }
            throw new DropException($"Unknown fun call: {ctx.GetText()}");
        }

        public override IPExpr VisitRvalue(PParser.RvalueContext context)
        {
            return Visit(context.expr());
        }

        public override IPExpr VisitKeywordExpr(PParser.KeywordExprContext ctx)
        {
            switch (ctx.fun.Text)
            {
                case "size":
                {
                    var expr = Visit(ctx.expr());
                    if (expr.Type.Canonicalize() is not SequenceType
                    && expr.Type.Canonicalize() is not MapType
                    && expr.Type.Canonicalize() is not SetType)
                    {
                        throw new DropException($"`{ctx.expr().GetText()}` is not a sequence");
                    }
                    return new SizeofExpr(ctx, expr) { Type = PInferBuiltinTypes.CollectionSize };
                }
                case "indexof":
                {
                    var repr = ctx.GetText();
                    if (ReprToTerm.TryGetValue(repr, out var term))
                    {
                        return term;
                    }
                    throw new DropException($"Indexing not found: `{ctx.GetText()}`");
                }
            }
            throw new DropException($"Unsupported KwExpr: {ctx.fun.Text}");
        }
    }
}