using Antlr4.Runtime;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class MachineProto : IConstructibleDecl
    {
        public MachineProto(string name, PParser.ImplMachineProtoDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
        public PLanguageType PayloadType { get; set; } = PrimitiveType.Null;
    }
}
