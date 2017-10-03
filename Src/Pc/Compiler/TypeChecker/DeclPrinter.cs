using System;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Microsoft.Pc.TypeChecker
{
    public class DeclPrinter : IParseTreeListener
    {
        private readonly ParseTreeProperty<DeclarationTable> programDeclarations;

        public DeclPrinter(ParseTreeProperty<DeclarationTable> programDeclarations)
        {
            this.programDeclarations = programDeclarations;
        }

        public void VisitTerminal(ITerminalNode node) { }

        public void VisitErrorNode(IErrorNode node) { throw new NotImplementedException(); }

        public void EnterEveryRule(ParserRuleContext ctx)
        {
            string padding = "".PadLeft(ctx.Depth());
            string output = ctx.GetType().Name;
            DeclarationTable decls = programDeclarations.Get(ctx);
            if (decls != null)
            {
                string declList = string.Join(", ",
                                              decls.AllDecls.Select(decl => $"{decl.Name}: {decl.GetType().Name}"));
                output = $"{output} [{declList}]";
            }
            Console.WriteLine($"{padding}{output}");
        }

        public void ExitEveryRule(ParserRuleContext ctx) { }
    }
}
