using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using System.Linq;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class UnionOrComposeModuleExpr : IPModuleExpr
    {

        private List<IPModuleExpr> componentModules;
        private bool isComposition = false;

        public IReadOnlyList<IPModuleExpr> ComponentModules => componentModules;
        public bool IsComposition => isComposition;

        public UnionOrComposeModuleExpr(ParserRuleContext sourceNode, List<IPModuleExpr> modules, bool isComposition)
        {
            SourceLocation = sourceNode;
            this.componentModules = modules;
            this.isComposition = isComposition;
            ModuleInfo = null;
        }

        public ParserRuleContext SourceLocation { get; set; }

        public ModuleInfo ModuleInfo { get; set; }
    }
    
}