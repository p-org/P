using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Expressions;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class PInvariant : IPDecl
    {
        public PInvariant(string name, IPExpr body, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.InvDeclContext);
            Name = name;
            Body = body;
            SourceLocation = sourceNode;
        }
        
        public IPExpr Body { get; set; }
        public Scope Scope { get; set; }

        public string Name { get; set; }
        public ParserRuleContext SourceLocation { get; }
    }
}