// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PChecker.Runtime.Events;

namespace PChecker.Coverage
{
    /// <summary>
    /// The code coverage reporter.
    /// </summary>
    public class ActivityCoverageReporter
    {
        /// <summary>
        /// Data structure containing information
        /// regarding testing coverage.
        /// </summary>
        private readonly CoverageInfo CoverageInfo;

        /// <summary>
        /// Set of built in events which we hide in the coverage report.
        /// </summary>
        private readonly HashSet<string> BuiltInEvents = new HashSet<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityCoverageReporter"/> class.
        /// </summary>
        public ActivityCoverageReporter(CoverageInfo coverageInfo)
        {
            CoverageInfo = coverageInfo;
            BuiltInEvents.Add(typeof(GotoStateEvent).FullName);
            BuiltInEvents.Add(typeof(DefaultEvent).FullName);
        }

        /// <summary>
        /// Emits the code coverage report.
        /// </summary>
        public void EmitCoverageReport(string coverageFile)
        {
            using (var writer = new StreamWriter(coverageFile))
            {
                WriteCoverageText(writer);
            }
        }

        /// <summary>
        /// Writes the visualization text.
        /// </summary>
        internal void WriteCoverageText(TextWriter writer)
        {
            var machines = new List<string>(CoverageInfo.Machines);
            machines.Sort();

            // (machines + "." + states => registered events
            var uncoveredEvents = new Dictionary<string, HashSet<string>>();
            foreach (var item in CoverageInfo.RegisteredEvents)
            {
                uncoveredEvents[item.Key] = new HashSet<string>(item.Value);
            }

            var totalEvents = (from h in uncoveredEvents select h.Value.Count).Sum();

            // Now use the graph to find incoming links to each state and remove those from the list of uncovered events.
            RemoveCoveredEvents(uncoveredEvents);

            var totalUncoveredEvents = (from h in uncoveredEvents select h.Value.Count).Sum();

            var eventCoverage = totalEvents == 0 ? "100.0" : ((totalEvents - totalUncoveredEvents) * 100.0 / totalEvents).ToString("F1");

            WriteHeader(writer, string.Format("Total event coverage: {0}%", eventCoverage));

            // Per-machine data.
            foreach (var machine in machines)
            {
                WriteHeader(writer, string.Format("StateMachine: {0}", GetSanitizedName(machine)));

                // find all possible events for this machine.
                var uncoveredMachineEvents = new Dictionary<string, HashSet<string>>();
                var allMachineEvents = new Dictionary<string, HashSet<string>>();

                foreach (var item in CoverageInfo.RegisteredEvents)
                {
                    var id = GetMachineId(item.Key);
                    if (id == machine)
                    {
                        uncoveredMachineEvents[item.Key] = new HashSet<string>(item.Value);
                        allMachineEvents[item.Key] = new HashSet<string>(item.Value);
                    }
                }

                // Now use the graph to find incoming links to each state in this machine and remove those from the list of uncovered events.
                RemoveCoveredEvents(uncoveredMachineEvents);

                var totalMachineEvents = (from h in allMachineEvents select h.Value.Count).Sum();
                var totalUncoveredMachineEvents = (from h in uncoveredMachineEvents select h.Value.Count).Sum();

                eventCoverage = totalMachineEvents == 0 ? "100.0" : ((totalMachineEvents - totalUncoveredMachineEvents) * 100.0 / totalMachineEvents).ToString("F1");
                writer.WriteLine("Event coverage: {0}%", eventCoverage);

                if (!CoverageInfo.MachinesToStates.ContainsKey(machine))
                {
                    CoverageInfo.MachinesToStates[machine] = new HashSet<string>(new[] { "ExternalState" });
                }

                // Per-state data.
                foreach (var state in CoverageInfo.MachinesToStates[machine])
                {
                    var key = machine + "." + state;
                    var totalStateEvents = (from h in allMachineEvents where h.Key == key select h.Value.Count).Sum();
                    var uncoveredStateEvents = (from h in uncoveredMachineEvents where h.Key == key select h.Value.Count).Sum();

                    writer.WriteLine();
                    writer.WriteLine("\tState: {0}{1}", state, totalStateEvents > 0 && totalStateEvents == uncoveredStateEvents ? " is uncovered" : string.Empty);
                    if (totalStateEvents == 0)
                    {
                        writer.WriteLine("\t\tState has no expected events, so coverage is 100%");
                    }
                    else if (totalStateEvents != uncoveredStateEvents)
                    {
                        eventCoverage = totalStateEvents == 0 ? "100.0" : ((totalStateEvents - uncoveredStateEvents) * 100.0 / totalStateEvents).ToString("F1");
                        writer.WriteLine("\t\tState event coverage: {0}%", eventCoverage);
                    }
                    

                    var received = new HashSet<string>(CoverageInfo.EventInfo.GetEventsReceived(key));
                    RemoveBuiltInEvents(received);

                    if (received.Count > 0)
                    {
                        writer.WriteLine("\t\tEvents received: {0}", string.Join(", ", SortHashSet(received)));
                    }

                    var sent = new HashSet<string>(CoverageInfo.EventInfo.GetEventsSent(key));
                    RemoveBuiltInEvents(sent);

                    if (sent.Count > 0)
                    {
                        writer.WriteLine("\t\tEvents sent: {0}", string.Join(", ", SortHashSet(sent)));
                    }

                    var stateUncoveredEvents = (from h in uncoveredMachineEvents where h.Key == key select h.Value).FirstOrDefault();
                    if (stateUncoveredEvents != null && stateUncoveredEvents.Count > 0)
                    {
                        writer.WriteLine("\t\tEvents not covered: {0}", string.Join(", ", SortHashSet(stateUncoveredEvents)));
                    }
                }

                writer.WriteLine();
            }
        }

        private void RemoveBuiltInEvents(HashSet<string> eventList)
        {
            var gotoState = typeof(GotoStateEvent).FullName;
            var defaultEvent = typeof(DefaultEvent).FullName;

            foreach (var name in eventList.ToArray())
            {
                if (BuiltInEvents.Contains(name))
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

                foreach (var e in CoverageInfo.EventInfo.GetEventsReceived(stateId))
                {
                    eventSet.Remove(e);
                }
            }
        }

        private static List<string> SortHashSet(HashSet<string> items)
        {
            var sorted = new List<string>();
            foreach (var i in items)
            {
                sorted.Add(GetSanitizedName(i));
            }
            sorted.Sort();
            return sorted;
        }

        private static string GetStateName(string nodeId)
        {
            var i = nodeId.LastIndexOf(".");
            if (i > 0)
            {
                return nodeId.Substring(i + 1);
            }

            return nodeId;
        }

        private static void WriteHeader(TextWriter writer, string header)
        {
            writer.WriteLine(header);
            writer.WriteLine(new string('=', header.Length));
        }

        private static string GetMachineId(string nodeId)
        {
            var i = nodeId.LastIndexOf(".");
            if (i > 0)
            {
                return nodeId.Substring(0, i);
            }

            return nodeId;
        }

        private static string GetSanitizedName(string name)
        {
            var i = name.LastIndexOf(".");
            if (i > 0)
            {
                return name.Substring(i + 1);
            }

            return name;
        }
    }
}