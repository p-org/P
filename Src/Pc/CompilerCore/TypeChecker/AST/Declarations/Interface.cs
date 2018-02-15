using System;
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

    public interface IInterfaceSet
    {
        IEnumerable<Interface> Interfaces { get; }
        bool AddInterface(Interface @interface);
        bool Contains(Interface @interface);
        bool Intersects(IInterfaceSet eventSet);
    }

    public class InterfaceSet : IInterfaceSet
    {
        private static readonly Comparer<Interface> InterfaceNameComparer =
            Comparer<Interface>.Create((it1, it2) => string.Compare(it1.Name, it2.Name, StringComparison.Ordinal));

        private readonly SortedSet<Interface> interfaces = new SortedSet<Interface>(InterfaceNameComparer);

        public IEnumerable<Interface> Interfaces=> interfaces;

        public bool AddInterface(Interface @interface)
        {
            return interfaces.Add(@interface);
        }

        public bool Contains(Interface @interface)
        {
            return interfaces.Contains(@interface);
        }

        public bool Intersects(IInterfaceSet interfaceSet)
        {
            return interfaces.Overlaps(interfaceSet.Interfaces);
        }
    }
}