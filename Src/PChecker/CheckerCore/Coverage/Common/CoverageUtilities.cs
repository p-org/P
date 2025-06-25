// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace PChecker.Coverage.Common
{
    /// <summary>
    /// Utility methods for working with coverage data.
    /// </summary>
    internal static class CoverageUtilities
    {
        /// <summary>
        /// Gets a sanitized name by returning only the portion after the last dot.
        /// </summary>
        public static string GetSanitizedName(string name)
        {
            var i = name.LastIndexOf(".");
            if (i > 0)
            {
                return name.Substring(i + 1);
            }

            return name;
        }
        
        /// <summary>
        /// Gets the machine ID from a fully qualified state ID.
        /// </summary>
        public static string GetMachineId(string nodeId)
        {
            var i = nodeId.LastIndexOf(".");
            if (i > 0)
            {
                return nodeId.Substring(0, i);
            }

            return nodeId;
        }

        /// <summary>
        /// Gets the state name from a fully qualified state ID.
        /// </summary>
        public static string GetStateName(string nodeId)
        {
            var i = nodeId.LastIndexOf(".");
            if (i > 0)
            {
                return nodeId.Substring(i + 1);
            }

            return nodeId;
        }
        
        /// <summary>
        /// Creates a fully qualified state ID from machine type and state name.
        /// </summary>
        public static string GetStateId(string machineType, string stateName)
        {
            var id = ResolveMachineTypeName(machineType);
            if (string.IsNullOrEmpty(stateName))
            {
                if (machineType == null)
                {
                    stateName = "ExternalState";
                }
                else
                {
                    stateName = "ExternalState"; // In the original code this was using GetLabel, simplified here
                }
            }

            return id += "." + stateName;
        }

        /// <summary>
        /// Resolves the machine type name, handling null values.
        /// </summary>
        public static string ResolveMachineTypeName(string machineType)
        {
            if (machineType == null)
            {
                // The sender id can be null if an event is fired from non-stateMachine code.
                return "ExternalCode";
            }

            return machineType;
        }
        
        /// <summary>
        /// Formats a coverage percentage as a string.
        /// </summary>
        public static string FormatCoveragePercentage(int total, int uncovered)
        {
            return total == 0 ? "100.0" : ((total - uncovered) * 100.0 / total).ToString("F1");
        }

        /// <summary>
        /// Returns a sorted list of items from a HashSet.
        /// </summary>
        public static List<string> SortHashSet(HashSet<string> items)
        {
            var sorted = new List<string>();
            foreach (var i in items)
            {
                sorted.Add(GetSanitizedName(i));
            }
            sorted.Sort();
            return sorted;
        }
        
        /// <summary>
        /// Adds a value to a HashSet, creating the HashSet if it doesn't exist in the dictionary.
        /// </summary>
        public static void AddToHashSet<TKey, TValue>(
            IDictionary<TKey, HashSet<TValue>> dictionary, 
            TKey key, 
            TValue value)
        {
            if (!dictionary.TryGetValue(key, out HashSet<TValue> set))
            {
                set = new HashSet<TValue>();
                dictionary[key] = set;
            }

            set.Add(value);
        }
        
        /// <summary>
        /// Merges two dictionaries of HashSets.
        /// </summary>
        public static void MergeHashSets<TKey, TValue>(
            Dictionary<TKey, HashSet<TValue>> target, 
            Dictionary<TKey, HashSet<TValue>> source)
        {
            foreach (var pair in source)
            {
                var key = pair.Key;
                if (!target.TryGetValue(key, out var valueSet))
                {
                    valueSet = new HashSet<TValue>();
                    target[key] = valueSet;
                }

                valueSet.UnionWith(pair.Value);
            }
        }
    }
}
