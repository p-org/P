using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Statements;

namespace Microsoft.Pc.TypeChecker
{
    public class FunctionBodyListener : PParserBaseListener
    {
        private readonly ITranslationErrorHandler handler;
        private readonly ParseTreeProperty<IPDecl> nodesToDeclarations;
        private readonly ParseTreeProperty<Scope> nodesToScopes;

        public FunctionBodyListener(
            ITranslationErrorHandler handler,
            ParseTreeProperty<Scope> nodesToScopes,
            ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            this.handler = handler;
            this.nodesToDeclarations = nodesToDeclarations;
            this.nodesToScopes = nodesToScopes;
        }

        public override void EnterFunctionBody(PParser.FunctionBodyContext context)
        {
            var fun = (Function) nodesToDeclarations.Get(context.Parent);
            Scope table = nodesToScopes.Get(context.Parent);
            Debug.Assert(table != null);
            var statementVisitor = new StatementVisitor(table, fun.Owner, handler);
            fun.Body = new CompoundStmt(context.statement().Select(s => statementVisitor.Visit(s)).ToList());
        }
    }
}