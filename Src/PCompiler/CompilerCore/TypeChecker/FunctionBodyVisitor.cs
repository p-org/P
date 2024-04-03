using System.Collections.Generic;
using System.Diagnostics.Contracts;
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

        public override object VisitPFunDecl(PParser.PFunDeclContext context)
        {
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

        public override object VisitScenarioDecl(PParser.ScenarioDeclContext context)
        {
            return Visit(context.scenarioBody());
        }

        public override object VisitScenarioBody(PParser.ScenarioBodyContext context)
        {
            var exprVisitor = new ExprVisitor(method, config.Handler);
            // var compound = new CompoundStmt(context, new IPStmt[0]);
            var stmts = new List<IPStmt>();
            foreach (var exprContext in context.expr())
            {
                var constraint = exprVisitor.Visit(exprContext);
                if (!Equals(constraint.Type, PrimitiveType.Bool))
                {
                    throw config.Handler.TypeMismatch(exprContext, constraint.Type, PrimitiveType.Bool);
                }
                stmts.Add( new ConstraintStmt(exprContext, constraint));
            }

            method.Body = new CompoundStmt(context, stmts);
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