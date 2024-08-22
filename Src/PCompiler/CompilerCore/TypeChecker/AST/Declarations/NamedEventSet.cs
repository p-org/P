using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public interface IEventSet
    {
        IEnumerable<Event> Events { get; }

        bool AddEvent(Event pEvent);

        void AddEvents(IEnumerable<Event> evts);

        bool Contains(Event pEvent);

        bool IsSame(IEventSet eventSet);

        bool IsSubsetEqOf(IEventSet eventSet);

        bool IsSubsetEqOf(IEnumerable<Event> eventSet);

        bool Intersects(IEnumerable<Event> eventSet);
    }

    public class EventSet : IEventSet
    {
        private static readonly Comparer<Event> EventNameComparer =
            Comparer<Event>.Create((ev1, ev2) => string.Compare(ev1.Name, ev2.Name, StringComparison.Ordinal));

        private readonly SortedSet<Event> events = new SortedSet<Event>(EventNameComparer);

        public IEnumerable<Event> Events => events;

        public bool AddEvent(Event pEvent)
        {
            return events.Add(pEvent);
        }

        public void AddEvents(IEnumerable<Event> evts)
        {
            foreach (var pEvent in evts)
            {
                AddEvent(pEvent);
            }
        }

        public bool Contains(Event pEvent)
        {
            return events.Contains(pEvent);
        }

        public bool IsSame(IEventSet eventSet)
        {
            return events.SetEquals(eventSet.Events);
        }

        public bool IsSubsetEqOf(IEventSet eventSet)
        {
            return events.IsSubsetOf(eventSet.Events);
        }

        public bool IsSubsetEqOf(IEnumerable<Event> eventsList)
        {
            return events.IsSubsetOf(eventsList);
        }

        public bool Intersects(IEnumerable<Event> eventSet)
        {
            return events.Overlaps(eventSet);
        }
    }

    public class NamedEventSet : IPDecl, IEventSet
    {
        private readonly EventSet events = new EventSet();

        public NamedEventSet(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(string.Empty.Equals(name) && sourceNode == null ||
                         sourceNode is PParser.EventSetDeclContext ||
                         sourceNode is PParser.EventSetLiteralContext ||
                         sourceNode is PParser.InterfaceDeclContext ||
                         sourceNode is PParser.ImplMachineDeclContext ||
                         sourceNode is PParser.SpecMachineDeclContext ||
                         sourceNode is PParser.StateDeclContext);
            Name = name;
            SourceLocation = sourceNode;
        }

        public IEnumerable<Event> Events => events.Events;

        public bool AddEvent(Event evt)
        {
            return events.AddEvent(evt);
        }

        public bool Contains(Event pEvent)
        {
            return events.Contains(pEvent);
        }

        public bool IsSame(IEventSet eventSet)
        {
            return events.IsSame(eventSet);
        }

        public void AddEvents(IEnumerable<Event> evts)
        {
            foreach (var pEvent in evts)
            {
                AddEvent(pEvent);
            }
        }

        public bool IsSubsetEqOf(IEventSet eventSet)
        {
            return events.IsSubsetEqOf(eventSet);
        }

        public bool IsSubsetEqOf(IEnumerable<Event> eventsList)
        {
            return events.IsSubsetEqOf(eventsList);
        }

        public bool Intersects(IEnumerable<Event> eventSet)
        {
            return events.Intersects(eventSet);
        }

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}