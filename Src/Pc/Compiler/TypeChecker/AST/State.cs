using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class State : IPDecl
    {
        public State(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.StateDeclContext);
            Name = name;
            SourceLocation = sourceNode;
        }

        public StateTemperature Temperature { get; set; }
        public bool IsStart { get; set; }
        public Function Entry { get; set; }
        public IDictionary<PEvent, IStateAction> Actions { get; } = new Dictionary<PEvent, IStateAction>();
        public Function Exit { get; set; }
        public Machine OwningMachine { get; set; }
        public IStateContainer Container { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
        public IList<IPAST> Children => throw new NotImplementedException("ast children");
    }
}
