using Antlr4.Runtime;
using System.Collections.Generic;

namespace Plang.Compiler.TypeChecker.AST.ModuleExprs
{
    public class UnionOrComposeModuleExpr : IPModuleExpr
    {
        public UnionOrComposeModuleExpr(ParserRuleContext sourceNode, IReadOnlyList<IPModuleExpr> modules,
            bool isComposition)
        {
            SourceLocation = sourceNode;
            ComponentModules = modules;
            IsComposition = isComposition;
            ModuleInfo = null;
        }

        public IReadOnlyList<IPModuleExpr> ComponentModules { get; }

        public bool IsComposition { get; }

        public ParserRuleContext SourceLocation { get; }

        public ModuleInfo ModuleInfo { get; set; }
    }
}