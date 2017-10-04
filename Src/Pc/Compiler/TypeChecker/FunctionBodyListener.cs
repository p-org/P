using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST;

namespace Microsoft.Pc.TypeChecker
{
    public class FunctionBodyListener : PParserBaseListener
    {
        private readonly ITranslationErrorHandler handler;
        private readonly ParseTreeProperty<IPDecl> nodesToDeclarations;
        private readonly ParseTreeProperty<Scope> nodesToScopes;

        public FunctionBodyListener(
            ITranslationErrorHandler handler,
            ParseTreeProperty<IPDecl> nodesToDeclarations,
            ParseTreeProperty<Scope> nodesToScopes)
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
            fun.Body = context.statement().SelectMany(stmt => statementVisitor.Visit(stmt)).ToList();
        }
    }
}