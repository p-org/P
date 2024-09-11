// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;
using PChecker.StateMachines;
using PChecker.StateMachines.Events;
using PChecker.StateMachines.Logging;

namespace PChecker.Coverage
{
    /// <summary>
    /// Implements the <see cref="IControlledRuntimeLog"/> and builds a directed graph
    /// from the recorded events and state transitions.
    /// </summary>
    public class ControlledRuntimeLogGraphBuilder : IControlledRuntimeLog
    {
        private Graph CurrentGraph;
        private readonly Dictionary<StateMachineId, EventInfo> Dequeued = new Dictionary<StateMachineId, EventInfo>(); // current dequeued event.
        private readonly Dictionary<StateMachineId, string> HaltedStates = new Dictionary<StateMachineId, string>(); // halted state for given state machine.
        private readonly bool MergeEventLinks; // merge events from node A to node B instead of making them separate links.
        private const string ExternalCodeName = "ExternalCode";
        private const string ExternalStateName = "ExternalState";
        private const string StateMachineCategory = "StateMachine";
        private const string MonitorCategory = "Monitor";

        private class EventInfo
        {
            public string Name;
            public string Type;
            public string State;
            public string Event;
            public string HandlingState;
        }

        private readonly Dictionary<string, List<EventInfo>> Inbox = new Dictionary<string, List<EventInfo>>();
        private static readonly Dictionary<string, string> EventAliases = new Dictionary<string, string>();
        private readonly HashSet<string> Namespaces = new HashSet<string>();
        private static readonly char[] TypeSeparators = new char[] { '.', '+' };

        private class DoActionEvent : Event
        {
        }

        private class PopStateEvent : Event
        {
        }

        static ControlledRuntimeLogGraphBuilder()
        {
            EventAliases[typeof(GotoStateEvent).FullName] = "goto";
            EventAliases[typeof(HaltEvent).FullName] = "halt";
            EventAliases[typeof(DefaultEvent).FullName] = "default";
            EventAliases[typeof(QuiescentEvent).FullName] = "quiescent";
            EventAliases[typeof(WildCardEvent).FullName] = "*";
            EventAliases[typeof(DoActionEvent).FullName] = "do";
            EventAliases[typeof(PopStateEvent).FullName] = "pop";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledRuntimeLogGraphBuilder"/> class.
        /// </summary>
        public ControlledRuntimeLogGraphBuilder(bool mergeEventLinks)
        {
            MergeEventLinks = mergeEventLinks;
            CurrentGraph = new Graph();
        }

        /// <summary>
        /// Set this boolean to true to get a collapsed graph showing only
        /// machine types, states and events.  This will not show machine "instances".
        /// </summary>
        public bool CollapseMachineInstances { get; set; }

        /// <summary>
        /// Get or set the underlying logging object.
        /// </summary>
        public TextWriter Logger { get; set; }

        /// <summary>
        /// Get the Graph object built by this logger.
        /// </summary>
        public Graph Graph
        {
            get
            {
                if (CurrentGraph == null)
                {
                    CurrentGraph = new Graph();
                }

                return CurrentGraph;
            }
        }

        /// <inheritdoc/>
        public void OnCreateStateMachine(StateMachineId id, string creatorName, string creatorType)
        {
            lock (Inbox)
            {
                var resolvedId = GetResolveStateMachineId(id?.Name, id?.Type);
                var node = Graph.GetOrCreateNode(resolvedId);
                node.Category = StateMachineCategory;
            }
        }

        /// <inheritdoc/>
        public void OnSendEvent(StateMachineId targetStateMachineId, string senderName, string senderType, string senderStateName,
            Event e, bool isTargetHalted)
        {
            var eventName = e.GetType().FullName;
            AddEvent(targetStateMachineId.Name, targetStateMachineId.Type, senderName, senderType, senderStateName, eventName);
        }

        /// <inheritdoc/>
        public void OnRaiseEvent(StateMachineId id, string stateName, Event e)
        {
            var eventName = e.GetType().FullName;
            // Raising event to self.
            AddEvent(id.Name, id.Type, id.Name, id.Type, stateName, eventName);
        }

        /// <inheritdoc/>
        public void OnEnqueueEvent(StateMachineId id, Event e)
        {
        }

        /// <inheritdoc/>
        public void OnDequeueEvent(StateMachineId id, string stateName, Event e)
        {
            lock (Inbox)
            {
                var resolvedId = GetResolveStateMachineId(id?.Name, id?.Type);
                var eventName = e.GetType().FullName;
                var info = PopEvent(resolvedId, eventName);
                if (info != null)
                {
                    Dequeued[id] = info;
                }
            }
        }

        private EventInfo PopEvent(string resolvedId, string eventName)
        {
            EventInfo result = null;
            lock (Inbox)
            {
                if (Inbox.TryGetValue(resolvedId, out var inbox))
                {
                    for (var i = inbox.Count - 1; i >= 0; i--)
                    {
                        if (inbox[i].Event == eventName)
                        {
                            result = inbox[i];
                            inbox.RemoveAt(i);
                        }
                    }
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public void OnReceiveEvent(StateMachineId id, string stateName, Event e, bool wasBlocked)
        {
            var resolvedId = GetResolveStateMachineId(id?.Name, id?.Type);
            lock (Inbox)
            {
                if (Inbox.TryGetValue(resolvedId, out var inbox))
                {
                    var eventName = e.GetType().FullName;
                    for (var i = inbox.Count - 1; i >= 0; i--)
                    {
                        var info = inbox[i];
                        if (info.Event == eventName)
                        {
                            // Yay, found it so we can draw the complete link connecting the Sender state to this state!
                            var category = StateMachineCategory;
                            var source = GetOrCreateChild(info.Name, info.Type, info.State);
                            var target = GetOrCreateChild(id?.Name, id?.Type, category, stateName);
                            GetOrCreateEventLink(source, target, info);
                            inbox.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void OnWaitEvent(StateMachineId id, string stateName, Type eventType)
        {
        }

        /// <inheritdoc/>
        public void OnWaitEvent(StateMachineId id, string stateName, params Type[] eventTypes)
        {
        }

        /// <inheritdoc/>
        public void OnStateTransition(StateMachineId id, string stateName, bool isEntry)
        {
            if (isEntry)
            {
                // record the fact we have entered this state
                GetOrCreateChild(id?.Name, id?.Type, stateName);
            }
        }

        /// <inheritdoc/>
        public void OnExecuteAction(StateMachineId id, string handlingStateName, string currentStateName, string actionName)
        {
            LinkTransition(typeof(DoActionEvent), id, handlingStateName, currentStateName, null);
        }

        /// <inheritdoc/>
        public void OnGotoState(StateMachineId id, string currentStateName, string newStateName)
        {
            LinkTransition(typeof(GotoStateEvent), id, currentStateName, currentStateName, newStateName);
        }

        /// <inheritdoc/>
        public void OnHalt(StateMachineId id, int inboxSize)
        {
            lock (Inbox)
            {
                HaltedStates.TryGetValue(id, out var stateName);
                if (string.IsNullOrEmpty(stateName))
                {
                    stateName = "null";
                }

                // Transition to the Halt state.
                var source = GetOrCreateChild(id?.Name, id?.Type, stateName);
                var target = GetOrCreateChild(id?.Name, id?.Type, "Halt", "Halt");
                GetOrCreateEventLink(source, target, new EventInfo() { Event = typeof(HaltEvent).FullName });
            }
        }

        private int? GetLinkIndex(GraphNode source, GraphNode target, string id)
        {
            if (MergeEventLinks)
            {
                return null;
            }

            return Graph.GetUniqueLinkIndex(source, target, id);
        }

        /// <inheritdoc/>
        public void OnDefaultEventHandler(StateMachineId id, string stateName)
        {
            lock (Inbox)
            {
                var resolvedId = GetResolveStateMachineId(id?.Name, id?.Type);
                var eventName = typeof(DefaultEvent).FullName;
                AddEvent(id.Name, id.Type, id.Name, id.Type, stateName, eventName);
                Dequeued[id] = PopEvent(resolvedId, eventName);
            }
        }

        /// <inheritdoc/>
        public void OnHandleRaisedEvent(StateMachineId id, string stateName, Event e)
        {
            lock (Inbox)
            {
                // We used the inbox to store raised event, but it should be the first one handled since
                // raised events are highest priority.
                var resolvedId = GetResolveStateMachineId(id?.Name, id?.Type);
                lock (Inbox)
                {
                    if (Inbox.TryGetValue(resolvedId, out var inbox))
                    {
                        var eventName = e.GetType().FullName;
                        for (var i = inbox.Count - 1; i >= 0; i--)
                        {
                            var info = inbox[i];
                            if (info.Event == eventName)
                            {
                                Dequeued[id] = info;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void OnPopStateUnhandledEvent(StateMachineId stateMachineId, string currentStateName, Event e)
        {
            if (e is HaltEvent)
            {
                HaltedStates[stateMachineId] = currentStateName;
            }
        }

        /// <inheritdoc/>
        public void OnExceptionThrown(StateMachineId id, string stateName, string actionName, Exception ex)
        {
        }

        /// <inheritdoc/>
        public void OnExceptionHandled(StateMachineId id, string stateName, string actionName, Exception ex)
        {
        }

        /// <inheritdoc/>
        public void OnCreateMonitor(string monitorType)
        {
            lock (Inbox)
            {
                var node = Graph.GetOrCreateNode(monitorType, monitorType);
                node.Category = MonitorCategory;
            }
        }

        /// <inheritdoc/>
        public void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
            // Monitors process actions immediately, so this state transition is a result of the only event in the inbox.
            lock (Inbox)
            {
                if (Inbox.TryGetValue(monitorType, out var inbox) && inbox.Count > 0)
                {
                    var e = inbox[inbox.Count - 1];
                    inbox.RemoveAt(inbox.Count - 1);
                    // Draw the link connecting the Sender state to this state!
                    var source = GetOrCreateChild(e.Name, e.Type, e.State);
                    var target = GetOrCreateChild(monitorType, monitorType, stateName);
                    GetOrCreateEventLink(source, target, e);
                }
            }
        }

        /// <inheritdoc/>
        public void OnMonitorProcessEvent(string monitorType, string stateName, string senderName, string senderType,
            string senderStateName, Event e)
        {
            lock (Inbox)
            {
                // Now add a fake event for internal monitor state transition that might now happen as a result of this event,
                // storing the monitor's current state in this event.
                var info = AddEvent(monitorType, monitorType, monitorType, monitorType, stateName, e.GetType().Name);

                // Draw the link connecting the Sender state to this state!
                var source = GetOrCreateChild(senderName, senderType, senderStateName);
                var target = GetOrCreateChild(monitorType, monitorType, stateName);
                GetOrCreateEventLink(source, target, info);
            }
        }

        /// <inheritdoc/>
        public void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
            // Raising event to self.
            var eventName = e.GetType().FullName;
            AddEvent(monitorType, monitorType, monitorType, monitorType, stateName, eventName);
        }

        /// <inheritdoc/>
        public void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
        {
            if (isEntry)
            {
                lock (Inbox)
                {
                    // Monitors process events immediately (and does not call OnDequeue), so this state transition is a result of
                    // the fake event we created in OnMonitorProcessEvent.
                    if (Inbox.TryGetValue(monitorType, out var inbox) && inbox.Count > 0)
                    {
                        var info = inbox[inbox.Count - 1];
                        inbox.RemoveAt(inbox.Count - 1);

                        // draw the link connecting the current state to this new state!
                        var source = GetOrCreateChild(monitorType, monitorType, info.State);

                        var shortStateName = GetLabel(monitorType, monitorType, stateName);
                        var suffix = string.Empty;
                        if (isInHotState.HasValue)
                        {
                            suffix = (isInHotState == true) ? "[hot]" : "[cold]";
                            shortStateName += suffix;
                        }

                        var label = shortStateName;
                        var target = GetOrCreateChild(monitorType, monitorType, shortStateName, label);

                        // In case this node was already created, we may need to override the label here now that
                        // we know this is a hot state. This is because, unfortunately, other OnMonitor* methods
                        // do not provide the isInHotState parameter.
                        target.Label = label;
                        GetOrCreateEventLink(source, target, info);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
        {
            var source = GetOrCreateChild(monitorType, monitorType, stateName);
            source.Category = "Error";
        }

        /// <inheritdoc/>
        public void OnRandom(object result, string callerName, string callerType)
        {
        }

        /// <inheritdoc/>
        public void OnAssertionFailure(string error)
        {
        }

        /// <inheritdoc/>
        public void OnStrategyDescription(string strategyName, string description)
        {
        }

        /// <inheritdoc/>
        public void OnCompleted()
        {
        }

        /// <summary>
        /// Return current graph and reset for next schedule.
        /// </summary>
        /// <param name="reset">Set to true will reset the graph for the next schedule.</param>
        /// <returns>The graph.</returns>
        public Graph SnapshotGraph(bool reset)
        {
            var result = CurrentGraph;
            if (reset)
            {
                // start fresh.
                CurrentGraph = null;
            }

            return result;
        }

        private string GetResolveStateMachineId(string name, string type)
        {
            if (type == null)
            {
                // The sender id can be null if an event is fired from non-stateMachine code.
                return ExternalCodeName;
            }

            if (CollapseMachineInstances)
            {
                return type;
            }

            return name;
        }

        private EventInfo AddEvent(string targetName, string targetType, string senderName, string senderType,
            string senderStateName, string eventName)
        {
            var targetId = GetResolveStateMachineId(targetName, targetType);
            EventInfo info = null;
            lock (Inbox)
            {
                if (!Inbox.TryGetValue(targetId, out var inbox))
                {
                    inbox = new List<EventInfo>();
                    Inbox[targetId] = inbox;
                }

                info = new EventInfo()
                {
                    Name = senderName ?? ExternalCodeName,
                    Type = senderType ?? ExternalCodeName,
                    State = senderStateName,
                    Event = eventName
                };

                inbox.Add(info);
            }

            return info;
        }

        private void LinkTransition(Type transitionType, StateMachineId id, string handlingStateName,
            string currentStateName, string newStateName)
        {
            var name = id.Name;
            var type = id.Type;
            lock (Inbox)
            {
                if (Dequeued.TryGetValue(id, out var info))
                {
                    // Event was dequeued, but now we know what state is handling this event, so connect the dots...
                    if (info.Type != type || info.Name != name || info.State != currentStateName)
                    {
                        var source = GetOrCreateChild(info.Name, info.Type, info.State);
                        var target = GetOrCreateChild(name, type, currentStateName);
                        info.HandlingState = handlingStateName;
                        GetOrCreateEventLink(source, target, info);
                    }
                }

                if (newStateName != null)
                {
                    // Then this is a goto or push and we can draw that link also.
                    var source = GetOrCreateChild(name, type, currentStateName);
                    var target = GetOrCreateChild(name, type, newStateName);
                    if (info == null)
                    {
                        info = new EventInfo { Event = transitionType.FullName };
                    }

                    GetOrCreateEventLink(source, target, info);
                }

                Dequeued.Remove(id);
            }
        }

        private GraphNode GetOrCreateChild(string name, string type, string stateName, string label = null)
        {
            GraphNode child = null;
            lock (Inbox)
            {
                AddNamespace(type);

                var initalStateName = stateName;

                // make label relative to fully qualified state machine id (it's usually a nested class).
                stateName = GetLabel(name, type, stateName);

                var id = GetResolveStateMachineId(name, type);
                var parent = Graph.GetOrCreateNode(id);
                parent.AddAttribute("Group", "Expanded");

                if (string.IsNullOrEmpty(label))
                {
                    label = stateName ?? ExternalStateName;
                }

                if (!string.IsNullOrEmpty(stateName))
                {
                    id += "." + stateName;
                }

                child = Graph.GetOrCreateNode(id, label);
                Graph.GetOrCreateLink(parent, child, null, null, "Contains");
            }

            return child;
        }

        private GraphLink GetOrCreateEventLink(GraphNode source, GraphNode target, EventInfo e)
        {
            GraphLink link = null;
            lock (Inbox)
            {
                var label = GetEventLabel(e.Event);
                var index = GetLinkIndex(source, target, label);
                var category = GetEventCategory(e.Event);
                link = Graph.GetOrCreateLink(source, target, index, label, category);
                if (MergeEventLinks)
                {
                    if (link.AddListAttribute("EventIds", e.Event) > 1)
                    {
                        link.Label = "*";
                    }
                }
                else
                {
                    link.AddAttribute("EventId", e.Event);
                    if (e.HandlingState != null)
                    {
                        link.AddAttribute("HandledBy", e.HandlingState);
                    }
                }
            }

            return link;
        }

        private void AddNamespace(string type)
        {
            if (type != null && !Namespaces.Contains(type))
            {
                var typeName = type;
                var index = typeName.Length;
                do
                {
                    typeName = typeName.Substring(0, index);
                    Namespaces.Add(typeName);
                    index = typeName.LastIndexOfAny(TypeSeparators);
                }
                while (index > 0);
            }
        }

        private string GetLabel(string name, string type, string fullyQualifiedName)
        {
            if (type == null)
            {
                // external code
                return fullyQualifiedName;
            }

            AddNamespace(type);
            
            var len = fullyQualifiedName.Length;
            var index = fullyQualifiedName.LastIndexOfAny(TypeSeparators);
            if (index > 0)
            {
                fullyQualifiedName = fullyQualifiedName.Substring(index).Trim('+').Trim('.');
            }

            return fullyQualifiedName;
        }

        private string GetEventLabel(string fullyQualifiedName)
        {
            if (EventAliases.TryGetValue(fullyQualifiedName, out var label))
            {
                return label;
            }

            var i = fullyQualifiedName.LastIndexOfAny(TypeSeparators);
            if (i > 0)
            {
                var ns = fullyQualifiedName.Substring(0, i);
                if (Namespaces.Contains(ns))
                {
                    return fullyQualifiedName.Substring(i + 1);
                }
            }

            return fullyQualifiedName;
        }

        private static string GetEventCategory(string fullyQualifiedName)
        {
            if (EventAliases.TryGetValue(fullyQualifiedName, out var label))
            {
                return label;
            }

            return null;
        }
    }

    /// <summary>
    /// A directed graph made up of Nodes and Links.
    /// </summary>
    [DataContract]
    public class Graph
    {
        // These [DataMember] fields are here so we can serialize the Graph across parallel or distributed
        // test processes without losing any information.

        [DataMember]
        private readonly Dictionary<string, GraphNode> InternalNodes = new Dictionary<string, GraphNode>();
        [DataMember]
        private readonly Dictionary<string, GraphLink> InternalLinks = new Dictionary<string, GraphLink>();
        // last used index for simple link key "a->b".
        [DataMember]
        private readonly Dictionary<string, int> InternalNextLinkIndex = new Dictionary<string, int>();
        // maps augmented link key to the index that has been allocated for that link id "a->b(goto)" => 0
        [DataMember]
        private readonly Dictionary<string, int> InternalAllocatedLinkIndexes = new Dictionary<string, int>();
        [DataMember]
        private readonly Dictionary<string, string> InternalAllocatedLinkIds = new Dictionary<string, string>();

        /// <summary>
        /// Return the current list of nodes (in no particular order).
        /// </summary>
        public IEnumerable<GraphNode> Nodes
        {
            get { return InternalNodes.Values; }
        }

        /// <summary>
        /// Return the current list of links (in no particular order).
        /// </summary>
        public IEnumerable<GraphLink> Links
        {
            get
            {
                if (InternalLinks == null)
                {
                    return Array.Empty<GraphLink>();
                }

                return InternalLinks.Values;
            }
        }

        /// <summary>
        /// Get existing node or null.
        /// </summary>
        /// <param name="id">The id of the node.</param>
        public GraphNode GetNode(string id)
        {
            InternalNodes.TryGetValue(id, out var node);
            return node;
        }

        /// <summary>
        /// Get existing node or create a new one with the given id and label.
        /// </summary>
        /// <returns>Returns the new node or the existing node if it was already defined.</returns>
        public GraphNode GetOrCreateNode(string id, string label = null, string category = null)
        {
            if (!InternalNodes.TryGetValue(id, out var node))
            {
                node = new GraphNode(id, label, category);
                InternalNodes.Add(id, node);
            }

            return node;
        }

        /// <summary>
        /// Get existing node or create a new one with the given id and label.
        /// </summary>
        /// <returns>Returns the new node or the existing node if it was already defined.</returns>
        private GraphNode GetOrCreateNode(GraphNode newNode)
        {
            if (!InternalNodes.ContainsKey(newNode.Id))
            {
                InternalNodes.Add(newNode.Id, newNode);
            }

            return newNode;
        }

        /// <summary>
        /// Get existing link or create a new one connecting the given source and target nodes.
        /// </summary>
        /// <returns>The new link or the existing link if it was already defined.</returns>
        public GraphLink GetOrCreateLink(GraphNode source, GraphNode target, int? index = null, string linkLabel = null, string category = null)
        {
            var key = source.Id + "->" + target.Id;
            if (index.HasValue)
            {
                key += string.Format("({0})", index.Value);
            }

            if (!InternalLinks.TryGetValue(key, out var link))
            {
                link = new GraphLink(source, target, linkLabel, category);
                if (index.HasValue)
                {
                    link.Index = index.Value;
                }

                InternalLinks.Add(key, link);
            }

            return link;
        }

        internal int GetUniqueLinkIndex(GraphNode source, GraphNode target, string id)
        {
            // augmented key
            var key = string.Format("{0}->{1}({2})", source.Id, target.Id, id);
            if (InternalAllocatedLinkIndexes.TryGetValue(key, out var index))
            {
                return index;
            }

            // allocate a new index for the simple key
            var simpleKey = string.Format("{0}->{1}", source.Id, target.Id);
            if (InternalNextLinkIndex.TryGetValue(simpleKey, out index))
            {
                index++;
            }

            InternalNextLinkIndex[simpleKey] = index;

            // remember this index has been allocated for this link id.
            InternalAllocatedLinkIndexes[key] = index;

            // remember the original id associated with this link index.
            key = string.Format("{0}->{1}({2})", source.Id, target.Id, index);
            InternalAllocatedLinkIds[key] = id;

            return index;
        }
        

        /// <summary>
        /// Merge the given graph so that this graph becomes a superset of both graphs.
        /// </summary>
        /// <param name="other">The new graph to merge into this graph.</param>
        public void Merge(Graph other)
        {
            foreach (var node in other.InternalNodes.Values)
            {
                var newNode = GetOrCreateNode(node.Id, node.Label, node.Category);
                newNode.Merge(node);
            }

            foreach (var link in other.InternalLinks.Values)
            {
                var source = GetOrCreateNode(link.Source.Id, link.Source.Label, link.Source.Category);
                var target = GetOrCreateNode(link.Target.Id, link.Target.Label, link.Target.Category);
                int? index = null;
                if (link.Index.HasValue)
                {
                    // ouch, link indexes cannot be compared across Graph instances, we need to assign a new index here.
                    var key = string.Format("{0}->{1}({2})", source.Id, target.Id, link.Index.Value);
                    var linkId = other.InternalAllocatedLinkIds[key];
                    index = GetUniqueLinkIndex(source, target, linkId);
                }

                var newLink = GetOrCreateLink(source, target, index, link.Label, link.Category);
                newLink.Merge(link);
            }
        }
    }

    /// <summary>
    /// A Node of a Graph.
    /// </summary>
    [DataContract]
    public class GraphObject
    {
        /// <summary>
        /// Optional list of attributes for the node.
        /// </summary>
        [DataMember]
        public Dictionary<string, string> Attributes { get; internal set; }

        /// <summary>
        /// Optional list of attributes that have a multi-part value.
        /// </summary>
        [DataMember]
        public Dictionary<string, HashSet<string>> AttributeLists { get; internal set; }

        /// <summary>
        /// Add an attribute to the node.
        /// </summary>
        public void AddAttribute(string name, string value)
        {
            if (Attributes == null)
            {
                Attributes = new Dictionary<string, string>();
            }

            Attributes[name] = value;
        }

        /// <summary>
        /// Creates a compound attribute value containing a merged list of unique values.
        /// </summary>
        /// <param name="key">The attribute name.</param>
        /// <param name="value">The new value to add to the unique list.</param>
        public int AddListAttribute(string key, string value)
        {
            if (AttributeLists == null)
            {
                AttributeLists = new Dictionary<string, HashSet<string>>();
            }

            if (!AttributeLists.TryGetValue(key, out var list))
            {
                list = new HashSet<string>();
                AttributeLists[key] = list;
            }

            list.Add(value);
            return list.Count;
        }

        internal void WriteAttributes(TextWriter writer)
        {
            if (Attributes != null)
            {
                var names = new List<string>(Attributes.Keys);
                names.Sort();  // creates a more stable output file (can be handy for expected output during testing).
                foreach (var name in names)
                {
                    var value = Attributes[name];
                    writer.Write(" {0}='{1}'", name, value);
                }
            }

            if (AttributeLists != null)
            {
                var names = new List<string>(AttributeLists.Keys);
                names.Sort();  // creates a more stable output file (can be handy for expected output during testing).
                foreach (var name in names)
                {
                    var value = AttributeLists[name];
                    writer.Write(" {0}='{1}'", name, string.Join(",", value));
                }
            }
        }

        internal void Merge(GraphObject other)
        {
            if (other.Attributes != null)
            {
                foreach (var key in other.Attributes.Keys)
                {
                    AddAttribute(key, other.Attributes[key]);
                }
            }

            if (other.AttributeLists != null)
            {
                foreach (var key in other.AttributeLists.Keys)
                {
                    foreach (var value in other.AttributeLists[key])
                    {
                        AddListAttribute(key, value);
                    }
                }
            }
        }
    }

    /// <summary>
    /// A Node of a Graph.
    /// </summary>
    [DataContract]
    public class GraphNode : GraphObject
    {
        /// <summary>
        /// The unique Id of the Node within the Graph.
        /// </summary>
        [DataMember]
        public string Id { get; internal set; }

        /// <summary>
        /// An optional display label for the node (does not need to be unique).
        /// </summary>
        [DataMember]
        public string Label { get; internal set; }

        /// <summary>
        /// An optional category for the node
        /// </summary>
        [DataMember]
        public string Category { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphNode"/> class.
        /// </summary>
        public GraphNode(string id, string label, string category)
        {
            Id = id;
            Label = label;
            Category = category;
        }
    }

    /// <summary>
    /// A Link represents a directed graph connection between two Nodes.
    /// </summary>
    [DataContract]
    public class GraphLink : GraphObject
    {
        /// <summary>
        /// An optional display label for the link.
        /// </summary>
        [DataMember]
        public string Label { get; internal set; }

        /// <summary>
        /// An optional category for the link.
        /// The special category "Contains" is reserved for building groups.
        /// </summary>
        [DataMember]
        public string Category { get; internal set; }

        /// <summary>
        /// The source end of the link.
        /// </summary>
        [DataMember]
        public GraphNode Source { get; internal set; }

        /// <summary>
        /// The target end of the link.
        /// </summary>
        [DataMember]
        public GraphNode Target { get; internal set; }

        /// <summary>
        /// The optional link index.
        /// </summary>
        [DataMember]
        public int? Index { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphLink"/> class.
        /// </summary>
        public GraphLink(GraphNode source, GraphNode target, string label, string category)
        {
            Source = source;
            Target = target;
            Label = label;
            Category = category;
        }
    }
}