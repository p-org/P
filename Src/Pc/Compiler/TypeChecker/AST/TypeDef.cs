using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class TypeDef : IPDecl
    {
        public TypeDef(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.TypeDefDeclContext);
            Name = name;
            SourceLocation = sourceNode;
        }

        public PLanguageType Type { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
        public IList<IPAST> Children => throw new NotImplementedException("ast children");
    }
}
