﻿using Antlr4.Runtime;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.Expressions;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class SafetyTest(ParserRuleContext sourceNode, string testName) : IPDecl
    {
        public string Main { get; set; }
        public IPModuleExpr ModExpr { get; set; }
        
        // from 2 to the number of parameters
        public int Twise { get; set; }

        // When there is no param expression, it fails back to normal test
        public IDictionary<string, List<IPExpr>> ParamExprMap { get; set; } = new Dictionary<string, List<IPExpr>>();

        // By default, assumption expression is true
        public IPExpr AssumeExpr { get; set; } = new BoolLiteralExpr(true);

        public string Name { get; } = testName;
        public ParserRuleContext SourceLocation { get; } = sourceNode;
    }
}