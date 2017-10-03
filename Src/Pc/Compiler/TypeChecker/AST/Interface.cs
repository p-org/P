using System.Collections.Generic;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class Interface : IConstructibleDecl
    {
        public Interface(string name, PParser.InterfaceDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public EventSet ReceivableEvents { get; set; }
        public ISet<Machine> Implementations { get; } = new HashSet<Machine>();

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
        public PLanguageType PayloadType { get; set; } = PrimitiveType.Null;
    }
}
