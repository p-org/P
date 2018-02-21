using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class RenameModuleExpr : IPModuleExpr
    {

        private IPModuleExpr componentModule;
        private Interface newInterface;
        private Interface oldInterface;

        public IPModuleExpr ComponentModule => componentModule;
        public Interface NewInterface => newInterface;
        public Interface OldInterface => oldInterface;

        public RenameModuleExpr(ParserRuleContext sourceNode, Interface newName, Interface oldName, IPModuleExpr module)
        {
            SourceLocation = sourceNode;
            newInterface = newName;
            oldInterface = oldName;
            this.componentModule = module;
            ModuleInfo = null;
        }

        public ParserRuleContext SourceLocation { get; set; }

        public ModuleInfo ModuleInfo { get; set; }
    }
    
}