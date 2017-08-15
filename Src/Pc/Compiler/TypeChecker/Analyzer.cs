using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    public class Analyzer
    {
        public static void Analyze(params PParser.ProgramContext[] programUnits)
        {
            var programDeclarations = new ParseTreeProperty<DeclarationTable>();

            // Step 1: Create mapping of names to 
            foreach (PParser.ProgramContext programUnit in programUnits)
            {
                DeclarationCollector.AnnotateWithDeclarations(programUnit, programDeclarations);
                //var walker = new ParseTreeWalker();
                //walker.Walk(new DeclPrinter(programDeclarations), programUnit);
            }
        }
    }
}