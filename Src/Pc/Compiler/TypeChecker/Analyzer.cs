using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    public static class Analyzer
    {
        public static void AnalyzeCompilationUnit(params PParser.ProgramContext[] programUnits)
        {
            var walker = new ParseTreeWalker();
            var topLevelTable = new DeclarationTable();
            var programDeclarations = new ParseTreeProperty<DeclarationTable>();
            var nodesToDeclarations = new ParseTreeProperty<IPDecl>();
            var stubListener = new DeclarationStubListener(topLevelTable, programDeclarations, nodesToDeclarations);
            var declListener = new DeclarationListener(programDeclarations, nodesToDeclarations);

            // Add built-in events to the table.
            topLevelTable.Put("halt", (PParser.EventDeclContext) null);
            topLevelTable.Put("null", (PParser.EventDeclContext) null);
            
            // Step 1: Create mapping of names to declaration stubs
            foreach (PParser.ProgramContext programUnit in programUnits)
            {
                walker.Walk(stubListener, programUnit);
            }

            // NOW: no declarations have ambiguous names.
            // NOW: there is exactly one declaration object for each declaration.
            // NOW: every declaration object is associated in both directions with its corresponding parse tree node.
            // NOW: enums and their elements are related to one another

            // Step 4: Validate declarations and fill with types
            foreach (PParser.ProgramContext programUnit in programUnits)
            {
                walker.Walk(declListener, programUnit);
            }

            // NOW: all enums are valid
            // NOW: all event sets are valid
        }
    }
}