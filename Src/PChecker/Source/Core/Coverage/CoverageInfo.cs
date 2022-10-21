// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace Microsoft.Coyote.Coverage
{
    /// <summary>
    /// Class for storing coverage-specific data
    /// across multiple testing iterations.
    /// </summary>
    [DataContract]
    public class CoverageInfo
    {
        /// <summary>
        /// Set of known machines.
        /// </summary>
        [DataMember]
        public HashSet<string> Machines { get; private set; }

        /// <summary>
        /// Map from machines to set of all states states defined in that machine.
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
        /// The coverage graph.
        /// </summary>
        [DataMember]
        public Graph CoverageGraph { get; set; }

        /// <summary>
        /// Information about events sent and received
        /// </summary>
        [DataMember]
        public EventCoverage EventInfo { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoverageInfo"/> class.
        /// </summary>
        public CoverageInfo()
        {
            this.Machines = new HashSet<string>();
            this.MachinesToStates = new Dictionary<string, HashSet<string>>();
            this.RegisteredEvents = new Dictionary<string, HashSet<string>>();
        }

        /// <summary>
        /// Checks if the machine type has already been registered for coverage.
        /// </summary>
        public bool IsMachineDeclared(string machineName) => this.MachinesToStates.ContainsKey(machineName);

        /// <summary>
        /// Declares a state.
        /// </summary>
        public void DeclareMachineState(string machine, string state) => this.AddState(machine, state);

        /// <summary>
        /// Declares a registered state, event pair.
        /// </summary>
        public void DeclareStateEvent(string machine, string state, string eventName)
        {
            this.AddState(machine, state);

            string key = machine + "." + state;
            this.InternalAddEvent(key, eventName);
        }

        private void InternalAddEvent(string key, string eventName)
        {
            if (!this.RegisteredEvents.ContainsKey(key))
            {
                this.RegisteredEvents.Add(key, new HashSet<string>());
            }

            this.RegisteredEvents[key].Add(eventName);
        }

        /// <summary>
        /// Merges the information from the specified coverage info. This is not thread-safe.
        /// </summary>
        public void Merge(CoverageInfo coverageInfo)
        {
            foreach (var machine in coverageInfo.Machines)
            {
                this.Machines.Add(machine);
            }

            foreach (var machine in coverageInfo.MachinesToStates)
            {
                foreach (var state in machine.Value)
                {
                    this.DeclareMachineState(machine.Key, state);
                }
            }

            foreach (var tup in coverageInfo.RegisteredEvents)
            {
                foreach (var e in tup.Value)
                {
                    this.InternalAddEvent(tup.Key, e);
                }
            }

            if (this.CoverageGraph == null)
            {
                this.CoverageGraph = coverageInfo.CoverageGraph;
            }
            else if (coverageInfo.CoverageGraph != null && this.CoverageGraph != coverageInfo.CoverageGraph)
            {
                this.CoverageGraph.Merge(coverageInfo.CoverageGraph);
            }

            if (this.EventInfo == null)
            {
                this.EventInfo = coverageInfo.EventInfo;
            }
            else if (coverageInfo.EventInfo != null && this.EventInfo != coverageInfo.EventInfo)
            {
                this.EventInfo.Merge(coverageInfo.EventInfo);
            }
        }

        /// <summary>
        /// Adds a new state.
        /// </summary>
        private void AddState(string machineName, string stateName)
        {
            this.Machines.Add(machineName);

            if (!this.MachinesToStates.ContainsKey(machineName))
            {
                this.MachinesToStates.Add(machineName, new HashSet<string>());
            }

            this.MachinesToStates[machineName].Add(stateName);
        }

        /// <summary>
        /// Load the given Coverage info file.
        /// </summary>
        /// <param name="filename">Path to the file to load.</param>
        /// <returns>The deserialized coverage info.</returns>
        public static CoverageInfo Load(string filename)
        {
            using (var fs = new FileStream(filename, FileMode.Open))
            {
                using (var reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas()))
                {
                    DataContractSerializerSettings settings = new DataContractSerializerSettings();
                    settings.PreserveObjectReferences = true;
                    var ser = new DataContractSerializer(typeof(CoverageInfo), settings);
                    return (CoverageInfo)ser.ReadObject(reader, true);
                }
            }
        }

        /// <summary>
        /// Save the coverage info to the given XML file.
        /// </summary>
        /// <param name="serFilePath">The path to the file to create.</param>
        public void Save(string serFilePath)
        {
            using (var fs = new FileStream(serFilePath, FileMode.Create))
            {
                DataContractSerializerSettings settings = new DataContractSerializerSettings();
                settings.PreserveObjectReferences = true;
                var ser = new DataContractSerializer(typeof(CoverageInfo), settings);
                ser.WriteObject(fs, this);
            }
        }
    }
}
