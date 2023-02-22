using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class Interface : IPDecl
    {
        public Interface(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.InterfaceDeclContext || sourceNode is PParser.ImplMachineDeclContext);
            Name = name;
            SourceLocation = sourceNode;
        }

        public IEventSet ReceivableEvents { get; set; }

        public PLanguageType PayloadType { get; set; } = PrimitiveType.Null;

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
    }

    public interface IInterfaceSet
    {
        IEnumerable<Interface> Interfaces { get; }

        bool AddInterface(Interface @interface);

        void AddInterfaces(IEnumerable<Interface> interfaces);

        bool Contains(Interface @interface);

        bool Intersects(IInterfaceSet interfaceSet);

        bool Intersects(IEnumerable<Interface> interfaceSet);
    }

    public class InterfaceSet : IInterfaceSet
    {
        private static readonly Comparer<Interface> InterfaceNameComparer =
            Comparer<Interface>.Create((it1, it2) => string.Compare(it1.Name, it2.Name, StringComparison.Ordinal));

        private readonly SortedSet<Interface> interfaces = new SortedSet<Interface>(InterfaceNameComparer);

        public IEnumerable<Interface> Interfaces => interfaces;

        public bool AddInterface(Interface @interface)
        {
            return interfaces.Add(@interface);
        }

        public void AddInterfaces(IEnumerable<Interface> its)
        {
            foreach (var @interface in its)
            {
                interfaces.Add(@interface);
            }
        }

        public bool Contains(Interface @interface)
        {
            return interfaces.Contains(@interface);
        }

        public bool Intersects(IInterfaceSet interfaceSet)
        {
            return interfaces.Overlaps(interfaceSet.Interfaces);
        }

        public bool Intersects(IEnumerable<Interface> interfaceSet)
        {
            return interfaces.Overlaps(interfaceSet);
        }
    }
}