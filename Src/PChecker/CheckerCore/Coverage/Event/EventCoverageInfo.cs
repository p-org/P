// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using PChecker.Coverage.Common;

namespace PChecker.Coverage.Event
{
    /// <summary>
    /// Class for storing coverage-specific data
    /// across multiple testing schedules.
    /// </summary>
    [DataContract]
    public class EventCoverageInfo
    {
        /// <summary>
        /// Set of known machines.
        /// </summary>
        [DataMember]
        public HashSet<string> Machines { get; private set; }

        /// <summary>
        /// Map from machines to set of all states defined in that machine.
        /// </summary>
        [DataMember]
        public Dictionary<string, HashSet<string>> MachinesToStates { get; private set; }

        /// <summary>
        /// Set of (machine + "." + state => registered events). So all events that can
        /// get us into each state.
        /// </summary>
        [DataMember]
        public Dictionary<string, HashSet<string>> RegisteredEvents { get; private set; }

        /// <summary>
        /// Information about events sent and received
        /// </summary>
        [DataMember]
        public EventCoverage EventInfo { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventCoverageInfo"/> class.
        /// </summary>
        public EventCoverageInfo()
        {
            Machines = new HashSet<string>();
            MachinesToStates = new Dictionary<string, HashSet<string>>();
            RegisteredEvents = new Dictionary<string, HashSet<string>>();
            EventInfo = new EventCoverage();
        }

        /// <summary>
        /// Checks if the machine type has already been registered for coverage.
        /// </summary>
        public bool IsMachineDeclared(string machineName) => MachinesToStates.ContainsKey(machineName);

        /// <summary>
        /// Declares a state.
        /// </summary>
        public void DeclareMachineState(string machine, string state) => AddState(machine, state);

        /// <summary>
        /// Declares a registered state, event pair.
        /// </summary>
        /// <param name="machine">The machine name.</param>
        /// <param name="state">The state name.</param>
        /// <param name="eventName">The event name.</param>
        public void DeclareStateEvent(string machine, string state, string eventName)
        {
            AddState(machine, state);

            var key = machine + "." + state;
            CoverageUtilities.AddToHashSet(RegisteredEvents, key, eventName);
        }

        /// <summary>
        /// Merges the information from the specified coverage info. This is not thread-safe.
        /// </summary>
        /// <param name="eventCoverageInfo">The coverage info to merge.</param>
        public void Merge(EventCoverageInfo eventCoverageInfo)
        {
            // Merge machines
            Machines.UnionWith(eventCoverageInfo.Machines);

            // Merge machine states
            foreach (var machine in eventCoverageInfo.MachinesToStates)
            {
                foreach (var state in machine.Value)
                {
                    DeclareMachineState(machine.Key, state);
                }
            }

            // Merge registered events
            CoverageUtilities.MergeHashSets(RegisteredEvents, eventCoverageInfo.RegisteredEvents);

            // Merge event info
            if (EventInfo == null)
            {
                EventInfo = eventCoverageInfo.EventInfo;
            }
            else if (eventCoverageInfo.EventInfo != null && EventInfo != eventCoverageInfo.EventInfo)
            {
                EventInfo.Merge(eventCoverageInfo.EventInfo);
            }
        }

        /// <summary>
        /// Adds a new state to a machine.
        /// </summary>
        /// <param name="machineName">The machine name.</param>
        /// <param name="stateName">The state name.</param>
        private void AddState(string machineName, string stateName)
        {
            Machines.Add(machineName);
            CoverageUtilities.AddToHashSet(MachinesToStates, machineName, stateName);
        }

        /// <summary>
        /// Load the given Coverage info file.
        /// </summary>
        /// <param name="filename">Path to the file to load.</param>
        /// <returns>The deserialized coverage info.</returns>
        public static EventCoverageInfo Load(string filename)
        {
            using var fs = new FileStream(filename, FileMode.Open);
            using var reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
            var settings = new DataContractSerializerSettings
            {
                PreserveObjectReferences = true
            };
            var ser = new DataContractSerializer(typeof(EventCoverageInfo), settings);
            return (EventCoverageInfo)ser.ReadObject(reader, true);
        }

        /// <summary>
        /// Save the coverage info to the given XML file.
        /// </summary>
        /// <param name="serFilePath">The path to the file to create.</param>
        public void Save(string serFilePath)
        {
            using var fs = new FileStream(serFilePath, FileMode.Create);
            var settings = new DataContractSerializerSettings();
            settings.PreserveObjectReferences = true;
            var ser = new DataContractSerializer(typeof(EventCoverageInfo), settings);
            ser.WriteObject(fs, this);
        }
    }
}
