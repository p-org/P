using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.AST.States
{
    public class State : IPDecl
    {
        private readonly IDictionary<PEvent, IStateAction> actions = new Dictionary<PEvent, IStateAction>();

        public State(ParserRuleContext sourceNode, string name)
        {
            Debug.Assert(sourceNode is PParser.StateDeclContext);
            Name = name;
            SourceLocation = sourceNode;
        }

        public StateTemperature Temperature { get; set; }

        public bool IsStart { get; set; }
        public Function Entry { get; set; }
        public IEnumerable<KeyValuePair<PEvent, IStateAction>> AllEventHandlers => actions;

        public Function Exit { get; set; }
        public Machine OwningMachine { get; set; }
        public IStateContainer Container { get; set; }

        public IStateAction this[PEvent index]
        {
            get => actions[index];
            set => actions[index] = value;
        }

        public string QualifiedName
        {
            get
            {
                var name = Name;
                var parent = Container;
                while (parent != null)
                {
                    name = parent.Name + "." + name;
                    parent = parent.ParentStateContainer;
                }

                return name;
            }
        }

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }

        public bool HasHandler(PEvent pEvent)
        {
            return actions.ContainsKey(pEvent);
        }
    }
}