using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class RenameModuleExpr : ModuleExpr
    {

        private IPModuleExpr module;
        private Interface newInterfaceName;
        private Interface oldInterfaceName;

        public RenameModuleExpr(ParserRuleContext sourceNode, Interface newName, Interface oldName, IPModuleExpr module)
        {
            SourceLocation = sourceNode;
            newInterfaceName = newName;
            oldInterfaceName = oldName;
            this.module = module;
        }

        public override bool CheckAndPopulateAttributes(ITranslationErrorHandler handler)
        {
            if (IsWellFormed)
                return true;

            //check that component module is wellformed
            

            //check if the current module is wellformed


            //populate the attributes of the module



            //module is wellformed
            isWellFormed = true;
            return IsWellFormed;
        }
    }
    
}