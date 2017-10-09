using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class EnumElem : IPDecl
    {
        public EnumElem(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.EnumElemContext || sourceNode is PParser.NumberedEnumElemContext);
            Name = name;
            SourceLocation = sourceNode;
        }
        
        public int Value { get; set; }
        public PEnum ParentEnum { get; set; }

        public ParserRuleContext SourceLocation { get; }
        public IList<IPAST> Children { get; } = new List<IPAST>();
        public string Name { get; }
    }
}
