using System;
using System.Diagnostics.Contracts;
using System.Linq;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker
{
    public class FunctionBodyVisitor : PParserBaseVisitor<object>
    {
        private readonly ICompilerConfiguration config;
        private readonly Machine machine;
        private readonly Function method;

        private FunctionBodyVisitor(ICompilerConfiguration config, Machine machine, Function method)
        {
            this.config = config;
            this.machine = machine;
            this.method = method;
        }

        public static void PopulateMethod(ICompilerConfiguration config, Function fun)
        {
            Contract.Requires(fun.Body == null);
            var visitor = new FunctionBodyVisitor(config, fun.Owner, fun);
            visitor.Visit(fun.SourceLocation);
        }

        public override object VisitAnonEventHandler(PParser.AnonEventHandlerContext context)
        {
            return Visit(context.functionBody());
        }

        public override object VisitNoParamAnonEventHandler(PParser.NoParamAnonEventHandlerContext context)
        {
            return Visit(context.functionBody());
        }

        public void populateFunctionProperty(PParser.RvalueContext ctx)
        {
            if (ctx.expr() is PParser.PrimitiveExprContext primCtx)
            {
                var idenCtx = primCtx.primitive().iden();
                if (idenCtx != null)
                {
                    switch (idenCtx.GetText())
                    {
                        case "refl": method.Property |= FunctionProperty.Reflexive; return;
                        case "trans": method.Property |= FunctionProperty.Transitive; return;
                        case "sym": method.Property |= FunctionProperty.Symmetric; return;
                        case "antisym": method.Property |= FunctionProperty.AntiSymmetric; return;
                        case "idempotent": method.Property |= FunctionProperty.Idempotent; return;
                        default: break;
                    }
                }
            }
            throw config.Handler.InternalError(ctx,
                        new System.Exception("Expecting one of `refl`, `trans`, `sym`, `antisym`, `idempotent`"));
        }

        public void populateProps(PParser.FunPropContext[] ctxs)
        {
            ExprVisitor exprVisitor = new(method, config.Handler);
            foreach (var propCtx in ctxs)
            {
                PParser.RvalueListContext propVals = propCtx.rvalueList();
                if (propCtx.decorator.GetText() == "prop")
                {
                    foreach (var prop in propVals.rvalue())
                    {
                        populateFunctionProperty(prop);
                    }
                }
                else
                {
                    if (!method.Signature.ReturnType.Canonicalize().IsAssignableFrom(PrimitiveType.Bool))
                    {
                        throw config.Handler.TypeMismatch(method.SourceLocation, method.Signature.ReturnType, PrimitiveType.Bool);
                    }
                    foreach (IPExpr e in propVals.rvalue().Select(exprVisitor.Visit))
                    {
                        switch (propCtx.decorator.GetText())
                        {
                            case "iff":     method.AddEquiv(e); break;
                            case "contra":  method.AddContradiction(e); break;
                            case "implied": method.AddImpliedBy(e); break;
                            default:
                                throw config.Handler.InternalError(propCtx, new System.Exception($"Expecting one of `@prop, @iff, @contra, @implied`"));
                        }
                    }
                }
            }
        }

        public override object VisitPFunDecl(PParser.PFunDeclContext context)
        {
            if (context.funProp() != null && context.funProp().Length > 0)
            {
                populateProps(context.funProp());
            }
            if (method.Property.HasFlag(FunctionProperty.Reflexive) 
                || method.Property.HasFlag(FunctionProperty.Symmetric)
                || method.Property.HasFlag(FunctionProperty.Transitive)
                || method.Property.HasFlag(FunctionProperty.AntiSymmetric))
            {
                if (method.Signature.Parameters.Count != 2)
                {
                    config.Output.WriteError($"refl, sym, trans, antisym should work on binary predicates, whereas `{method.Name}` has {method.Signature.Parameters.Count} parameter(s)");
                    Environment.Exit(1);
                }
            }
            return Visit(context.functionBody());
        }

        public override object VisitForeignFunDecl(PParser.ForeignFunDeclContext context)
        {
            return null;
        }

        public override object VisitFunctionBody(PParser.FunctionBodyContext context)
        {
            // TODO: check that parameters have been added to internal scope?

            // Add all local variables to scope.
            foreach (var varDeclContext in context.varDecl())
            {
                Visit(varDeclContext);
            }

            // Build the statement trees
            var statementVisitor = new StatementVisitor(config, machine, method);
            method.Body = (CompoundStmt)statementVisitor.Visit(context);
            return null;
        }

        public override object VisitVarDecl(PParser.VarDeclContext context)
        {
            foreach (var varName in context.idenList()._names)
            {
                var variable = method.Scope.Put(varName.GetText(), varName, VariableRole.Local);
                variable.Type = TypeResolver.ResolveType(context.type(), method.Scope, config.Handler);
                method.AddLocalVariable(variable);
            }

            return null;
        }
    }
}