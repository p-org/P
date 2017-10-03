using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class EnumElem : IPDecl
    {
        public EnumElem(string name, PParser.EnumElemContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public EnumElem(string name, PParser.NumberedEnumElemContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public int Value { get; set; }
        public PEnum ParentEnum { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }
}
