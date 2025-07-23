using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Statements;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class Pure : IPDecl, IHasScope
    {

        public Pure(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.PureDeclContext);
            Name = name;
            SourceLocation = sourceNode;
        }
        
        public FunctionSignature Signature { get; } = new FunctionSignature();

        public string Name { get; set; }
        public IPExpr Body { get; set; }
        public ParserRuleContext SourceLocation { get; }
        
        public Scope Scope { get; set; }
    }
}