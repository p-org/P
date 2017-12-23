using System.Linq;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Statements;

namespace Microsoft.Pc.TypeChecker
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
            if (fun.Body != null)
            {
                return;
            }
            
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

        public override object VisitPFunDecl(PParser.PFunDeclContext context) { return Visit(context.functionBody()); }

        public override object VisitForeignFunDecl(PParser.ForeignFunDeclContext context) { return null; }

        public override object VisitFunctionBody(PParser.FunctionBodyContext context)
        {
            // TODO: check that parameters have been added to internal scope?

            // Add all local variables to scope.
            foreach (PParser.VarDeclContext varDeclContext in context.varDecl())
            {
                Visit(varDeclContext);
            }

            // Build the statement trees
            var statementVisitor = new StatementVisitor(handler, machine, method);
            method.Body = new CompoundStmt(context, context.statement().Select(statementVisitor.Visit).ToList());
            return null;
        }

        public override object VisitVarDecl(PParser.VarDeclContext context)
        {
            foreach (PParser.IdenContext varName in context.idenList()._names)
            {
                Variable variable = method.Scope.Put(varName.GetText(), varName, VariableRole.Local);
                variable.Type = TypeResolver.ResolveType(context.type(), method.Scope, handler);
                method.AddLocalVariable(variable);
            }
            return null;
        }
    }
}