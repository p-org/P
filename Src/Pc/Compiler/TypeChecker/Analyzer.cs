using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST;

namespace Microsoft.Pc.TypeChecker
{
    public static class Analyzer
    {
        public static PProgramModel AnalyzeCompilationUnit(
            ITranslationErrorHandler handler,
            params PParser.ProgramContext[] programUnits)
        {
            var walker = new ParseTreeWalker();
            var globalScope = new Scope(handler);
            var nodesToScopes = new ParseTreeProperty<Scope>();
            var nodesToDeclarations = new ParseTreeProperty<IPDecl>();
            var declListener = new DeclarationListener(handler, nodesToScopes, nodesToDeclarations);
            var funcBodyListener = new FunctionBodyListener(handler, nodesToScopes, nodesToDeclarations);

            // Add built-in events to the table.
            globalScope.Put("halt", (PParser.EventDeclContext) null);
            globalScope.Put("null", (PParser.EventDeclContext) null);

            // Step 1: Create mapping of names to declaration stubs
            foreach (PParser.ProgramContext programUnit in programUnits)
            {
                DeclarationStubVisitor.PopulateStubs(globalScope, nodesToScopes, nodesToDeclarations, handler, programUnit);
            }

            // NOW: no declarations have ambiguous names.
            // NOW: there is exactly one declaration object for each declaration.
            // NOW: every declaration object is associated in both directions with its corresponding parse tree node.
            // NOW: enums and their elements are related to one another

            // Step 2: Validate declarations and fill with types
            foreach (PParser.ProgramContext programUnit in programUnits)
            {
                walker.Walk(declListener, programUnit);
            }

            //Validator.ValidateDeclarations(nodesToScopes, nodesToDeclarations, topLevelTable);

            // NOW: all declarations are valid, with appropriate links and types resolved.

            // Step 3: Fill in method bodies
            foreach (PParser.ProgramContext programUnit in programUnits)
            {
                walker.Walk(funcBodyListener, programUnit);
            }

            // NOW: AST Complete, pass to StringTemplate
            return new PProgramModel
            {
                GlobalScope = globalScope
            };
        }
    }
}
