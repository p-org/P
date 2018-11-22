using System.Collections.Generic;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST.ModuleExprs
{
    public class HideInterfaceModuleExpr : IPModuleExpr
    {
        private readonly List<Interface> hideInterfaces;

        public HideInterfaceModuleExpr(ParserRuleContext sourceNode, List<Interface> interfaces, IPModuleExpr module)
        {
            SourceLocation = sourceNode;
            hideInterfaces = interfaces;
            ComponentModule = module;
            ModuleInfo = null;
        }

        public IPModuleExpr ComponentModule { get; }

        public IReadOnlyList<Interface> HideInterfaces => hideInterfaces;

        public ParserRuleContext SourceLocation { get; }

        public ModuleInfo ModuleInfo { get; set; }
    }
}