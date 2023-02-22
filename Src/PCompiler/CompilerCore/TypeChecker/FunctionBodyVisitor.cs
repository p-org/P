using System.Diagnostics.Contracts;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Statements;

namespace Plang.Compiler.TypeChecker
{
    public class FunctionBodyVisitor : PParserBaseVisitor<object>
    {
        private readonly ITranslationErrorHandler handler;
        private readonly Machine machine;
        private readonly Function method;

        private FunctionBodyVisitor(ITranslationErrorHandler handler, Machine machine, Function method)
        {
            this.handler = handler;
            this.machine = machine;
            this.method = method;
        }

        public static void PopulateMethod(ITranslationErrorHandler handler, Function fun)
        {
            Contract.Requires(fun.Body == null);
            var visitor = new FunctionBodyVisitor(handler, fun.Owner, fun);
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
            var statementVisitor = new StatementVisitor(handler, machine, method);
            method.Body = (CompoundStmt)statementVisitor.Visit(context);
            return null;
        }

        public override object VisitVarDecl(PParser.VarDeclContext context)
        {
            foreach (var varName in context.idenList()._names)
            {
                var variable = method.Scope.Put(varName.GetText(), varName, VariableRole.Local);
                variable.Type = TypeResolver.ResolveType(context.type(), method.Scope, handler);
                method.AddLocalVariable(variable);
            }

            return null;
        }
    }
}