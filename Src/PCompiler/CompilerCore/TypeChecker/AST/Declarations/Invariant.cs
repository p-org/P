using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Expressions;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class Invariant : IPDecl
    {
        public Invariant(string name, IPExpr body, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.InvariantDeclContext);
            IsDefault = false;
            Name = name;
            Body = body;
            SourceLocation = sourceNode;
        }
        
        public Invariant(ParserRuleContext sourceNode)
        {
            SourceLocation = sourceNode;
            IsDefault = true;
            Name = "defualt";
        }
        
        public IPExpr Body { get; set; }

        public string Name { get; set; }
        
        public bool IsDefault { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}