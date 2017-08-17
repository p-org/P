using System.Collections.Generic;
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
            ParseTreeProperty<IPDecl> nodesToDeclarations = BuildParseTreeRelation(topLevelTable);
            var declListener = new DeclarationListener(programDeclarations, nodesToDeclarations);
            foreach (PParser.ProgramContext programUnit in programUnits)
            {
                walker.Walk(declListener, programUnit);
            }

            // NOW: all enums are valid
            // NOW: all event sets are valid
        }

        private static ParseTreeProperty<IPDecl> BuildParseTreeRelation(DeclarationTable table)
        {
            var prop = new ParseTreeProperty<IPDecl>();

            IEnumerable<IPDecl> WalkTable(DeclarationTable t)
            {
                foreach (IPDecl decl in t.AllDecls)
                {
                    yield return decl;
                }

                foreach (DeclarationTable child in t.Children)
                {
                    foreach (IPDecl decl in WalkTable(child))
                    {
                        yield return decl;
                    }
                }
            }

            foreach (IPDecl decl in WalkTable(table))
            {
                prop.Put(decl.SourceNode, decl);
            }

            return prop;
        }
    }
}