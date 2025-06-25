// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PChecker.Coverage.Common;
using PChecker.Runtime.Events;

namespace PChecker.Coverage.Event
{
    /// <summary>
    /// Generates coverage reports from event coverage data.
    /// </summary>
    public class EventCoverageReporter
    {
        /// <summary>
        /// Data structure containing information regarding testing coverage.
        /// </summary>
        private readonly EventCoverageInfo _eventCoverageInfo;

        /// <summary>
        /// Set of built in events which we hide in the coverage report.
        /// </summary>
        private readonly HashSet<string> _builtInEvents = new HashSet<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="EventCoverageReporter"/> class.
        /// </summary>
        /// <param name="eventCoverageInfo">The coverage information to report on.</param>
        public EventCoverageReporter(EventCoverageInfo eventCoverageInfo)
        {
            _eventCoverageInfo = eventCoverageInfo;
            _builtInEvents.Add(typeof(GotoStateEvent).FullName);
            _builtInEvents.Add(typeof(DefaultEvent).FullName);
        }

        /// <summary>
        /// Emits the code coverage report to a file.
        /// </summary>
        /// <param name="coverageFile">Path to the file to create.</param>
        public void EmitCoverageReport(string coverageFile)
        {
            using (var writer = new StreamWriter(coverageFile))
            {
                WriteCoverageText(writer);
            }
        }

        /// <summary>
        /// Writes the coverage report to the specified text writer.
        /// </summary>
        /// <param name="writer">The text writer to write the report to.</param>
        internal void WriteCoverageText(TextWriter writer)
        {
            var machines = new List<string>(_eventCoverageInfo.Machines);
            machines.Sort();

            // Write overall coverage
            WriteOverallCoverage(writer);
            
            // Write per-machine coverage
            foreach (var machine in machines)
            {
                WriteMachineCoverage(writer, machine);
            }
        }
        
        /// <summary>
        /// Writes the overall coverage statistics.
        /// </summary>
        private void WriteOverallCoverage(TextWriter writer)
        {
            // Calculate overall coverage
            var uncoveredEvents = CloneRegisteredEvents();
            var totalEvents = CountTotalEvents(uncoveredEvents);
            
            // Remove covered events
            RemoveCoveredEvents(uncoveredEvents);
            var totalUncoveredEvents = CountTotalEvents(uncoveredEvents);

            // Calculate coverage percentage
            var eventCoverage = CoverageUtilities.FormatCoveragePercentage(totalEvents, totalUncoveredEvents);
            
            WriteHeader(writer, $"Total event coverage: {eventCoverage}%");
        }
        
        /// <summary>
        /// Writes coverage information for a specific machine.
        /// </summary>
        private void WriteMachineCoverage(TextWriter writer, string machine)
        {
            WriteHeader(writer, $"StateMachine: {CoverageUtilities.GetSanitizedName(machine)}");

            // Get machine-specific events
            var uncoveredMachineEvents = GetMachineEvents(machine);
            var allMachineEvents = GetMachineEvents(machine);
            
            // Calculate coverage for this machine
            RemoveCoveredEvents(uncoveredMachineEvents);
            
            var totalMachineEvents = CountTotalEvents(allMachineEvents);
            var totalUncoveredMachineEvents = CountTotalEvents(uncoveredMachineEvents);

            var eventCoverage = CoverageUtilities.FormatCoveragePercentage(totalMachineEvents, totalUncoveredMachineEvents);
            writer.WriteLine("Event coverage: {0}%", eventCoverage);

            // Ensure the machine has a state collection
            if (!_eventCoverageInfo.MachinesToStates.ContainsKey(machine))
            {
                _eventCoverageInfo.MachinesToStates[machine] = new HashSet<string>(new[] { "ExternalState" });
            }

            // Write per-state coverage for this machine
            foreach (var state in _eventCoverageInfo.MachinesToStates[machine])
            {
                WriteStateCoverage(writer, machine, state, allMachineEvents, uncoveredMachineEvents);
            }

            writer.WriteLine();
        }
        
        /// <summary>
        /// Writes coverage information for a specific state.
        /// </summary>
        private void WriteStateCoverage(
            TextWriter writer, 
            string machine, 
            string state,
            Dictionary<string, HashSet<string>> allMachineEvents,
            Dictionary<string, HashSet<string>> uncoveredMachineEvents)
        {
            var key = machine + "." + state;
            
            // Calculate state coverage statistics
            var totalStateEvents = (from h in allMachineEvents where h.Key == key select h.Value.Count).Sum();
            var uncoveredStateEvents = (from h in uncoveredMachineEvents where h.Key == key select h.Value.Count).Sum();
            var isUncovered = totalStateEvents > 0 && totalStateEvents == uncoveredStateEvents;

            // Write state header with uncovered flag if needed
            writer.WriteLine();
            writer.WriteLine("\tState: {0}{1}", state, isUncovered ? " is uncovered" : string.Empty);
            
            // Write coverage percentage 
            if (totalStateEvents == 0)
            {
                writer.WriteLine("\t\tState has no expected events, so coverage is 100%");
            }
            else if (totalStateEvents != uncoveredStateEvents)
            {
                var eventCoverage = CoverageUtilities.FormatCoveragePercentage(totalStateEvents, uncoveredStateEvents);
                writer.WriteLine("\t\tState event coverage: {0}%", eventCoverage);
            }

            // Write events received
            WriteEventsReceived(writer, key);
            
            // Write events sent
            WriteEventsSent(writer, key);
            
            // Write uncovered events
            WriteUncoveredEvents(writer, key, uncoveredMachineEvents);
        }
        
        /// <summary>
        /// Writes information about events received by a state.
        /// </summary>
        private void WriteEventsReceived(TextWriter writer, string stateKey)
        {
            var received = new HashSet<string>(_eventCoverageInfo.EventInfo.GetEventsReceived(stateKey));
            RemoveBuiltInEvents(received);

            if (received.Count > 0)
            {
                writer.WriteLine("\t\tEvents received: {0}", string.Join(", ", CoverageUtilities.SortHashSet(received)));
            }
        }
        
        /// <summary>
        /// Writes information about events sent by a state.
        /// </summary>
        private void WriteEventsSent(TextWriter writer, string stateKey)
        {
            var sent = new HashSet<string>(_eventCoverageInfo.EventInfo.GetEventsSent(stateKey));
            RemoveBuiltInEvents(sent);

            if (sent.Count > 0)
            {
                writer.WriteLine("\t\tEvents sent: {0}", string.Join(", ", CoverageUtilities.SortHashSet(sent)));
            }
        }
        
        /// <summary>
        /// Writes information about uncovered events for a state.
        /// </summary>
        private void WriteUncoveredEvents(
            TextWriter writer, 
            string stateKey, 
            Dictionary<string, HashSet<string>> uncoveredEvents)
        {
            var stateUncoveredEvents = (from h in uncoveredEvents where h.Key == stateKey select h.Value).FirstOrDefault();
            if (stateUncoveredEvents != null && stateUncoveredEvents.Count > 0)
            {
                writer.WriteLine("\t\tEvents not covered: {0}", string.Join(", ", CoverageUtilities.SortHashSet(stateUncoveredEvents)));
            }
        }

        /// <summary>
        /// Creates a dictionary of all registered events grouped by state.
        /// </summary>
        private Dictionary<string, HashSet<string>> CloneRegisteredEvents()
        {
            var result = new Dictionary<string, HashSet<string>>();
            foreach (var item in _eventCoverageInfo.RegisteredEvents)
            {
                result[item.Key] = new HashSet<string>(item.Value);
            }
            return result;
        }
        
        /// <summary>
        /// Gets all events for a specific machine.
        /// </summary>
        private Dictionary<string, HashSet<string>> GetMachineEvents(string machine)
        {
            var result = new Dictionary<string, HashSet<string>>();
            foreach (var item in _eventCoverageInfo.RegisteredEvents)
            {
                var id = CoverageUtilities.GetMachineId(item.Key);
                if (id == machine)
                {
                    result[item.Key] = new HashSet<string>(item.Value);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Counts the total number of events across all states.
        /// </summary>
        private static int CountTotalEvents(Dictionary<string, HashSet<string>> events)
        {
            return (from h in events select h.Value.Count).Sum();
        }
        
        /// <summary>
        /// Removes built-in events from the event list.
        /// </summary>
        private void RemoveBuiltInEvents(HashSet<string> eventList)
        {
            foreach (var name in eventList.ToArray())
            {
                if (_builtInEvents.Contains(name))
                {
                    eventList.Remove(name);
                }
            }
        }

        /// <summary>
        /// Remove all events from expectedEvent that are found in the graph.
        /// </summary>
        /// <param name="expectedEvents">The list of all expected events organized by unique state Id</param>
        private void RemoveCoveredEvents(Dictionary<string, HashSet<string>> expectedEvents)
        {
            foreach (var pair in expectedEvents)
            {
                var stateId = pair.Key;
                var eventSet = pair.Value;

                foreach (var e in _eventCoverageInfo.EventInfo.GetEventsReceived(stateId))
                {
                    eventSet.Remove(e);
                }
            }
        }

        /// <summary>
        /// Writes a section header to the report.
        /// </summary>
        private static void WriteHeader(TextWriter writer, string header)
        {
            writer.WriteLine(header);
            writer.WriteLine(new string('=', header.Length));
        }
    }
}
