using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public interface IEventSet
    {
        IEnumerable<PEvent> Events { get; }

        bool AddEvent(PEvent pEvent);

        void AddEvents(IEnumerable<PEvent> evts);

        bool Contains(PEvent pEvent);

        bool IsSame(IEventSet eventSet);

        bool IsSubsetEqOf(IEventSet eventSet);

        bool IsSubsetEqOf(IEnumerable<PEvent> eventSet);

        bool Intersects(IEnumerable<PEvent> eventSet);
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

        public void AddEvents(IEnumerable<PEvent> evts)
        {
            foreach (var pEvent in evts)
            {
                AddEvent(pEvent);
            }
        }

        public bool Contains(PEvent pEvent)
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

        public bool IsSubsetEqOf(IEnumerable<PEvent> eventsList)
        {
            return events.IsSubsetOf(eventsList);
        }

        public bool Intersects(IEnumerable<PEvent> eventSet)
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

        public IEnumerable<PEvent> Events => events.Events;

        public bool AddEvent(PEvent evt)
        {
            return events.AddEvent(evt);
        }

        public bool Contains(PEvent pEvent)
        {
            return events.Contains(pEvent);
        }

        public bool IsSame(IEventSet eventSet)
        {
            return events.IsSame(eventSet);
        }

        public void AddEvents(IEnumerable<PEvent> evts)
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

        public bool IsSubsetEqOf(IEnumerable<PEvent> eventsList)
        {
            return events.IsSubsetEqOf(eventsList);
        }

        public bool Intersects(IEnumerable<PEvent> eventSet)
        {
            return events.Intersects(eventSet);
        }

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}