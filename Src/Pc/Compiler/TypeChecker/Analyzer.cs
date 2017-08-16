using System;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    public class Analyzer
    {
        public static void Analyze(params PParser.ProgramContext[] programUnits)
        {
            var programDeclarations = new ParseTreeProperty<DeclarationTable>();
            var topLevelTable = new DeclarationTable();
            /* TODO: strengthen interface of two listeners to ensure they are
             * always called at the root of the parse trees?
             */
            var walker = new ParseTreeWalker();
            var stubListener = new DeclarationStubListener(programDeclarations, topLevelTable);
            var types = new PTypeUniverse();
            var typeVisitor = new TypeVisitor(types);
            var declListener = new DeclarationListener(programDeclarations, typeVisitor);

            // Step 1: Create mapping of names to declaration stubs
            foreach (PParser.ProgramContext programUnit in programUnits)
            {
                walker.Walk(stubListener, programUnit);
            }

#if DEBUG
            // ASSERT: no declarations have ambiguous names.
            // ASSERT: there is exactly one declaration object for each declaration.
            // ASSERT: every declaration object is associated in both directions with its corresponding parse tree node.
            // TODO: implement above assertions
#endif

            // Step 2: Fill in declarations with types
            foreach (PParser.ProgramContext programUnit in programUnits)
            {
                walker.Walk(declListener, programUnit);
            }
        }
    }

    public class DeclPrinter : IParseTreeListener
    {
        private readonly ParseTreeProperty<DeclarationTable> programDeclarations;

        public DeclPrinter(ParseTreeProperty<DeclarationTable> programDeclarations)
        {
            this.programDeclarations = programDeclarations;
        }

        public void VisitTerminal(ITerminalNode node) { }

        public void VisitErrorNode(IErrorNode node)
        {
            throw new NotImplementedException();
        }

        public void EnterEveryRule(ParserRuleContext ctx)
        {
            string padding = "".PadLeft(ctx.Depth());
            string output = ctx.GetType().Name;
            DeclarationTable decls = programDeclarations.Get(ctx);
            if (decls != null)
            {
                string declList = string.Join(", ", decls.AllDecls.Select(decl => $"{decl.Name}: {decl.GetType().Name}"));
                output = $"{output} [{declList}]";
            }
            Console.WriteLine($"{padding}{output}");
        }

        public void ExitEveryRule(ParserRuleContext ctx) { }
    }
}