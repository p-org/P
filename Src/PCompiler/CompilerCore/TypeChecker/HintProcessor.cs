using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker
{
    public class HintProcessor : PParserBaseVisitor<object>
    {
        private ICompilerConfiguration config;
        private Hint hint;
        private Function stubFunc;
        private ExprVisitor exprVisitor;

        public HintProcessor(ICompilerConfiguration config, Hint hint)
        {
            this.config = config;
            this.hint = hint;
            stubFunc = new(null)
            {
                Scope = hint.Scope
            };
            exprVisitor = new(stubFunc, config.Handler);
        }

        public static void ParsePredicateExprs(ICompilerConfiguration config, Hint hint)
        {
            HintProcessor processor = new(config, hint);
            processor.Visit(hint.SourceLocation);
        }

        private int GetIntLiteral(PParser.HintItemContext ctx)
        {
            var rvalues = TypeCheckingUtils.VisitRvalueList(ctx.value, exprVisitor);
            if (rvalues.Count() > 1)
            {
                throw config.Handler.IncorrectArgumentCount(ctx, 1, rvalues.Count());
            }
            var car = rvalues.First();
            if (car is not IntLiteralExpr expr)
            {
                throw config.Handler.InternalError(ctx, new Exception("Expecting an integer literal here"));
            }
            return expr.Value;
        }

        private IPExpr GetBoolExpr(PParser.HintItemContext ctx)
        {
            var rvalues = TypeCheckingUtils.VisitRvalueList(ctx.value, exprVisitor);
            if (rvalues.Count() > 1)
            {
                throw config.Handler.IncorrectArgumentCount(ctx, 1, rvalues.Count());
            }
            var car = rvalues.First();
            if (!car.Type.IsAssignableFrom(PrimitiveType.Bool))
            {
                throw config.Handler.TypeMismatch(ctx.value, car.Type, PrimitiveType.Bool);
            }
            return car;
        }

        private List<Function> GetFunctions(PParser.HintItemContext ctx)
        {
            List<Function> result = [];
            foreach (PParser.RvalueContext rvalue in ctx.value.rvalue())
            {
                var exprCtx = rvalue.expr();
                if (exprCtx is PParser.PrimitiveExprContext primitiveCtx)
                {
                    var context = primitiveCtx.primitive();
                    if (context.iden() == null)
                    {
                        throw config.Handler.InternalError(context, new Exception($"Expecting function name(s) here"));
                    }
                    string funName = context.iden().GetText();
                    if (hint.Scope.Lookup(funName, out Function f))
                    {
                        result.Add(f);
                    }
                    else
                    {
                        throw config.Handler.MissingDeclaration(context, "function", funName);
                    }
                }
            }
            return result;
        }

        public PEvent GetSingleEvent(PParser.HintItemContext ctx)
        {
            if (ctx.value.rvalue().Count() > 1)
            {
                throw config.Handler.InternalError(ctx, new Exception(
                    $"Only 1 config event can be specified, got {ctx.value.rvalue().Count()}"
                ));
            }
            var primCtx = ctx.value.rvalue().First().expr();
            if (primCtx is PParser.PrimitiveExprContext p)
            {
                var context = p.primitive();
                if (context.iden() != null)
                {
                    var eventName = context.iden().GetText();
                    if (hint.Scope.Lookup(eventName, out PEvent e))
                    {
                        return e;
                    }
                    else
                    {
                        foreach (var ev in hint.Scope.Events)
                        {
                            Console.WriteLine(ev.Name);
                        }
                        throw config.Handler.MissingDeclaration(ctx, "config event", eventName);
                    }
                }
            }
            throw config.Handler.InternalError(ctx, new Exception($"Expecting an event name here"));
        }

        private List<IPExpr> Explicate(PParser.HintItemContext ctx, string loc, IPExpr expr)
        {
            switch (expr)
            {
                case BinOpExpr binOpExpr: {
                    if (binOpExpr.Operation == BinOpType.And)
                    {
                        List<IPExpr> lhs = Explicate(ctx, loc, binOpExpr.Lhs);
                        List<IPExpr> rhs = Explicate(ctx, loc, binOpExpr.Rhs);
                        return [.. lhs, .. rhs];
                    }
                    return [expr];
                }
                default:
                    if (expr.Type.IsAssignableFrom(PrimitiveType.Bool))
                    {
                        return [expr];
                    }
                    throw config.Handler.TypeMismatch(ctx, expr.Type, PrimitiveType.Bool);
            }
            throw config.Handler.InternalError(ctx, 
                    new Exception($"Unsupported expression in {loc}: {expr}"));
        }

        public override object VisitFuzzHintDecl(PParser.FuzzHintDeclContext context)
        {
            if (hint.Exact)
            {
                throw config.Handler.InternalError(context, new Exception($"Hint `{hint.Name}` is an exact hint, but currently processing a fuzzy hint"));
            }
            return populateHintFields(context.hintBody());
        }

        public override object VisitExactHintDecl(PParser.ExactHintDeclContext context)
        {
            if (!hint.Exact)
            {
                throw config.Handler.InternalError(context, new Exception($"Hint `{hint.Name}` is a fuzzy hint, but currently processing an exact hint"));
            }
            return populateHintFields(context.hintBody());
        }

        public Hint populateHintFields(PParser.HintBodyContext[] hintBody)
        {
            
            foreach (var bodyCtx in hintBody)
            {
                var bodyItemCtx = bodyCtx.hintItem();
                if (bodyItemCtx != null)
                {
                    switch (bodyItemCtx.field.GetText())
                    {
                        case "exists":
                            hint.ExistentialQuantifiers = GetIntLiteral(bodyItemCtx);
                            break;
                        case "arity":
                            hint.Arity = GetIntLiteral(bodyItemCtx);
                            break;
                        case "term_depth":
                            hint.TermDepth = GetIntLiteral(bodyItemCtx);
                            break;
                        case "config_event":
                            hint.ConfigEvent = GetSingleEvent(bodyItemCtx);
                            break;
                        case "num_guards":
                            var ng = GetIntLiteral(bodyItemCtx);
                            if (ng < hint.NumGuardPredicates)
                            {
                                config.Output.WriteWarning($"`num_guards` of {hint.Name} <= inferred, ignoring ...");
                            }
                            else
                            {
                                hint.NumGuardPredicates = ng;
                            }
                            break;
                        case "num_filters":
                            var nf = GetIntLiteral(bodyItemCtx);
                            if (nf < hint.NumFilterPredicates)
                            {
                                config.Output.WriteWarning($"`num_filters` of {hint.Name} <= inferred, ignoring ...");
                            }
                            else
                            {
                                hint.NumFilterPredicates = nf;
                            }
                            break;
                        case "include_guards":
                            hint.GuardPredicates = Explicate(bodyItemCtx, "guards", GetBoolExpr(bodyItemCtx));
                            hint.NumGuardPredicates = Math.Max(hint.GuardPredicates.Count, hint.NumFilterPredicates);
                            break;
                        case "include_filters":
                            hint.FilterPredicates = Explicate(bodyItemCtx, "filters", GetBoolExpr(bodyItemCtx));
                            hint.NumFilterPredicates = Math.Max(hint.NumFilterPredicates, hint.NumFilterPredicates);
                            break;
                        case "functions":
                            hint.CustomFunctions = GetFunctions(bodyItemCtx);
                            foreach (var f in hint.CustomFunctions)
                            {
                                if (f.Signature.ReturnType.IsAssignableFrom(PrimitiveType.Bool))
                                {
                                    throw config.Handler.InternalError(bodyItemCtx,
                                        new Exception($"Custom functions should return non-boolean types, but `{f.Name}` returns bool"));
                                }
                            }
                            break;
                        case "predicates":
                            hint.CustomPredicates = GetFunctions(bodyItemCtx);
                            foreach (var f in hint.CustomPredicates)
                            {
                                if (!f.Signature.ReturnType.IsAssignableFrom(PrimitiveType.Bool))
                                {
                                    throw config.Handler.InternalError(bodyItemCtx,
                                        new Exception($"Custom predicates should return bool, but `{f.Name}` returns {f.Signature.ReturnType}"));
                                }
                            }
                            break;
                        default:
                            throw config.Handler.InternalError(bodyItemCtx, new Exception(
                                @$"Unknown Hint field: `{bodyItemCtx.field.GetText()}`,
                                expecting one of exists, arity, term_depth, config_event, include_guards, include_filters, functions, predicates"
                            ));
                    }
                }
            }
            return hint;
        }
    }
}