using Antlr4.Runtime;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class PEvent : IPDecl
    {
        public PEvent(string name, PParser.EventDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
            PayloadType = PrimitiveType.Null;
            Assert = -1;
            Assume = -1;
        }

        public int Assume { get; set; }
        public int Assert { get; set; }
        public PLanguageType PayloadType { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }
}
