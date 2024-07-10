using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Expressions;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class AssumeOnStart : IPDecl
    {
        public AssumeOnStart(string name, IPExpr body, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.AssumeOnStartDeclContext);
            Body = body;
            Name = name;
            SourceLocation = sourceNode;
        }
        
        public IPExpr Body { get; set; }

        public string Name { get; set; }
        public ParserRuleContext SourceLocation { get; }
    }
}