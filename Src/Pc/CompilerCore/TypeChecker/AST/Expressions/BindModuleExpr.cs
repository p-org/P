using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class BindModuleExpr : IPModuleExpr
    {
        
        private List<Tuple<Interface, Machine>> bindings;
        public IReadOnlyList<Tuple<Interface, Machine>> Bindings => bindings;

        public BindModuleExpr(ParserRuleContext sourceNode, List<Tuple<Interface, Machine>> bindings)
        {
            SourceLocation = sourceNode;
            this.bindings = bindings;
            ModuleInfo = null;
        }

        public ParserRuleContext SourceLocation { get; set; }

        public ModuleInfo ModuleInfo { get; set; }
    }
    
}
 
 