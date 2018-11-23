using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.AST.ModuleExprs
{
    public class BindModuleExpr : IPModuleExpr
    {
        public BindModuleExpr(ParserRuleContext sourceNode, IReadOnlyList<Tuple<Interface, Machine>> bindings)
        {
            SourceLocation = sourceNode;
            Bindings = bindings;
            ModuleInfo = null;
        }

        public IReadOnlyList<Tuple<Interface, Machine>> Bindings { get; }

        public ParserRuleContext SourceLocation { get; }

        public ModuleInfo ModuleInfo { get; set; }
    }
}