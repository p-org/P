using System.Collections.Generic;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.AST.ModuleExprs
{
    public class AssertModuleExpr : IPModuleExpr
    {
        private readonly List<Machine> specMonitors;

        public AssertModuleExpr(ParserRuleContext sourceNode, List<Machine> specs, IPModuleExpr module)
        {
            SourceLocation = sourceNode;
            specMonitors = specs;
            ComponentModule = module;
            ModuleInfo = null;
        }

        public IPModuleExpr ComponentModule { get; }

        public IReadOnlyList<Machine> SpecMonitors => specMonitors;

        public ParserRuleContext SourceLocation { get; }

        public ModuleInfo ModuleInfo { get; set; }
    }
}