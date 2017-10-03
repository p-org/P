using System.Collections.Generic;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class State : IPDecl
    {
        public State(string name, PParser.StateDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public StateTemperature Temperature { get; set; }
        public bool IsStart { get; set; }
        public Function Entry { get; set; }
        public IDictionary<PEvent, IStateAction> Actions { get; } = new Dictionary<PEvent, IStateAction>();
        public Function Exit { get; set; }
        public Machine OwningMachine { get; set; }
        public IStateContainer Container { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }
}
