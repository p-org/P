using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public interface IEventSet
    {
        IEnumerable<PEvent> Events { get; }
        bool AddEvent(PEvent pEvent);
        bool Contains(PEvent pEvent);
        bool IsSupersetOf(IEventSet eventSet);
    }

    public class EventSet : IEventSet
    {
        private static readonly Comparer<PEvent> EventNameComparer =
            Comparer<PEvent>.Create((ev1, ev2) => string.Compare(ev1.Name, ev2.Name, StringComparison.Ordinal));

        private readonly SortedSet<PEvent> events = new SortedSet<PEvent>(EventNameComparer);

        public IEnumerable<PEvent> Events => events;

        public bool AddEvent(PEvent pEvent)
        {
            return events.Add(pEvent);
        }

        public bool Contains(PEvent pEvent)
        {
            return events.Contains(pEvent);
        }

        public bool IsSupersetOf(IEventSet eventSet)
        {
            return events.IsSupersetOf(eventSet.Events);
        }
    }

    internal class UniversalEventSet : IEventSet
    {
        private static readonly Comparer<PEvent> EventNameComparer =
            Comparer<PEvent>.Create((ev1, ev2) => string.Compare(ev1.Name, ev2.Name, StringComparison.Ordinal));

        private readonly SortedSet<PEvent> events = new SortedSet<PEvent>(EventNameComparer);

        private UniversalEventSet()
        {
        }

        public static UniversalEventSet Instance { get; } = new UniversalEventSet();

        public IEnumerable<PEvent> Events { get; }

        public bool AddEvent(PEvent pEvent)
        {
            return events.Add(pEvent);
        }

        public bool Contains(PEvent pEvent)
        {
            return true;
        }

        public bool IsSupersetOf(IEventSet eventSet)
        {
            return true;
        }
    }

    public class NamedEventSet : IPDecl, IEventSet
    {
        private readonly EventSet events = new EventSet();

        public NamedEventSet(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(string.Empty.Equals(name) && sourceNode == null ||
                         sourceNode is PParser.EventSetDeclContext ||
                         sourceNode is PParser.EventSetLiteralContext);
            Name = name;
            SourceLocation = sourceNode;
        }

        public IEnumerable<PEvent> Events => events.Events;

        public bool AddEvent(PEvent evt)
        {
            return events.AddEvent(evt);
        }

        public bool Contains(PEvent pEvent)
        {
            return events.Contains(pEvent);
        }

        public bool IsSupersetOf(IEventSet eventSet)
        {
            return events.IsSupersetOf(eventSet);
        }

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }

        public void AddEvents(IEnumerable<PEvent> evts)
        {
            foreach (PEvent pEvent in evts)
            {
                AddEvent(pEvent);
            }
        }
    }
}