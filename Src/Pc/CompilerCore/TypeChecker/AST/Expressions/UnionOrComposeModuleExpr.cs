using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class UnionOrComposeModuleExpr : ModuleExpr
    {

        private IEnumerable<IPModuleExpr> modules;
        private bool isComposition = false;

        public UnionOrComposeModuleExpr(ParserRuleContext sourceNode, IEnumerable<IPModuleExpr> modules, bool isComposition)
        {
            SourceLocation = sourceNode;
            this.modules = modules;
            this.isComposition = isComposition;
        }

        public override bool CheckAndPopulateAttributes(ITranslationErrorHandler handler)
        {
            if (IsWellFormed)
                return true;

            //check that all component modules are wellformed
            foreach(var module in modules)
            {
                
            }

            //check if the current module is wellformed


            //populate the attributes of the module



            //module is wellformed
            isWellFormed = true;
            return IsWellFormed;
        }
    }
    
}