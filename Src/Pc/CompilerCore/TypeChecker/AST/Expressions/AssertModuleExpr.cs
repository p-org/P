using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class AssertModuleExpr : IPModuleExpr
    {
       
        private IPModuleExpr componentModule;
        private List<Machine> specMonitors;

        public IPModuleExpr ComponentModule => componentModule;
        public IReadOnlyList<Machine> SpecMonitors => specMonitors;

        public AssertModuleExpr(ParserRuleContext sourceNode, List<Machine> specs, IPModuleExpr module)
        {
            SourceLocation = sourceNode;
            specMonitors = specs;
            this.componentModule = module;
            ModuleInfo = null;
        }
        
        public ParserRuleContext SourceLocation { get; set; }
        
        public ModuleInfo ModuleInfo { get; set; }
    }
    
}