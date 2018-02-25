using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST.ModuleExprs
{
    public class RenameModuleExpr : IPModuleExpr
    {
        public RenameModuleExpr(ParserRuleContext sourceNode, Interface newName, Interface oldName, IPModuleExpr module)
        {
            SourceLocation = sourceNode;
            NewInterface = newName;
            OldInterface = oldName;
            ComponentModule = module;
            ModuleInfo = null;
        }

        public IPModuleExpr ComponentModule { get; }

        public Interface NewInterface { get; }

        public Interface OldInterface { get; }

        public ParserRuleContext SourceLocation { get; }

        public ModuleInfo ModuleInfo { get; set; }
    }
}
