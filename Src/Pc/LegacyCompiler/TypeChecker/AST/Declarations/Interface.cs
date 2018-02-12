using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class Interface : IPDecl
    {
        private readonly HashSet<Machine> implementations = new HashSet<Machine>();

        public Interface(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.InterfaceDeclContext);
            Name = name;
            SourceLocation = sourceNode;
        }

        public IEventSet ReceivableEvents { get; set; }
        public IEnumerable<Machine> Implementations => implementations;

        public PLanguageType PayloadType { get; set; } = PrimitiveType.Null;

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }

        public void AddImplementation(Machine machine)
        {
            if (implementations.Contains(machine))
            {
                return;
            }
            implementations.Add(machine);
            machine.AddInterface(this);
        }
    }
}