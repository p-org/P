using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class Variable : IPDecl, ITypedName
    {
        public Variable(string name, PParser.VarDeclContext containingVarDecl, PParser.IdenContext sourceNode)
        {
            Name = name;
            ContainingVarDecl = containingVarDecl;
            SourceLocation = sourceNode;
        }

        public Variable(string name, PParser.FunParamContext sourceNode)
        {
            Name = name;
            SourceLocation = sourceNode;
        }

        public PParser.VarDeclContext ContainingVarDecl { get; }
        public bool IsParam => ContainingVarDecl == null;

        public string Name { get; set; }
        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type { get; set; }
        public IList<IPAST> Children => throw new NotImplementedException("ast children");
    }
}
