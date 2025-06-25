// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using PChecker.Coverage.Common;

namespace PChecker.Coverage.Event
{
    /// <summary>
    /// This class maintains information about events received and sent from each state of each state machine.
    /// </summary>
    [DataContract]
    public class EventCoverage
    {
        /// <summary>
        /// Map from states to the list of events received by that state. The state id is fully qualified by
        /// the state machine id it belongs to.
        /// </summary>
        [DataMember]
        private readonly Dictionary<string, HashSet<string>> EventsReceived = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// Map from states to the list of events sent by that state.  The state id is fully qualified by
        /// the state machine id it belongs to.
        /// </summary>
        [DataMember]
        private readonly Dictionary<string, HashSet<string>> EventsSent = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// Adds an event to the list of events received by a state.
        /// </summary>
        /// <param name="stateId">The fully qualified state ID.</param>
        /// <param name="eventId">The event ID to add.</param>
        internal void AddEventReceived(string stateId, string eventId)
        {
            CoverageUtilities.AddToHashSet(EventsReceived, stateId, eventId);
        }

        /// <summary>
        /// Get list of events received by the given fully qualified state.
        /// </summary>
        /// <param name="stateId">The state machine qualified state name</param>
        public IEnumerable<string> GetEventsReceived(string stateId)
        {
            if (EventsReceived.TryGetValue(stateId, out var set))
            {
                return set;
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Adds an event to the list of events sent by a state.
        /// </summary>
        /// <param name="stateId">The fully qualified state ID.</param>
        /// <param name="eventId">The event ID to add.</param>
        internal void AddEventSent(string stateId, string eventId)
        {
            CoverageUtilities.AddToHashSet(EventsSent, stateId, eventId);
        }

        /// <summary>
        /// Get list of events sent by the given state.
        /// </summary>
        /// <param name="stateId">The state machine qualified state name</param>
        public IEnumerable<string> GetEventsSent(string stateId)
        {
            if (EventsSent.TryGetValue(stateId, out var set))
            {
                return set;
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Merges the information from the specified coverage data.
        /// </summary>
        /// <param name="other">The coverage data to merge.</param>
        internal void Merge(EventCoverage other)
        {
            CoverageUtilities.MergeHashSets(EventsReceived, other.EventsReceived);
            CoverageUtilities.MergeHashSets(EventsSent, other.EventsSent);
        }

        /// <summary>
        /// Determines whether a specific event was handled in a specific state of a machine.
        /// </summary>
        /// <param name="machine">The machine name.</param>
        /// <param name="state">The state name.</param>
        /// <param name="eventName">The event name.</param>
        /// <returns>True if the event was handled in the specified state, false otherwise.</returns>
        public bool IsEventHandled(string machine, string state, string eventName)
        {
            // Create the qualified state ID (machine.state)
            string stateId = machine + "." + state;
            
            // Check if this event was ever received in this state
            if (EventsReceived.TryGetValue(stateId, out var eventsReceived))
            {
                return eventsReceived.Contains(eventName);
            }
            
            return false;
        }
    }
}
