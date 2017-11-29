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

        public FunctionBodyListener(
            ITranslationErrorHandler handler,
            ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            this.handler = handler;
            this.nodesToDeclarations = nodesToDeclarations;
        }

        public override void EnterFunctionBody(PParser.FunctionBodyContext context)
        {
            if (nodesToDeclarations.Get(context.Parent) is Function fun)
            {
                var statementVisitor = new StatementVisitor(handler, fun.Owner, fun);
                fun.Body = new CompoundStmt(context.statement().Select(s => statementVisitor.Visit(s)).ToList());
            }
        }
    }
}