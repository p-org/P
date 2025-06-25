// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Threading;

namespace PChecker.Coverage.Code
{
    /// <summary>
    /// Tracks code coverage information emitted during program execution.
    /// </summary>
    [DataContract]
    public class CodeCoverage
    {
        /// <summary>
        /// Singleton instance of the CodeCoverage class.
        /// </summary>
        private static readonly Lazy<CodeCoverage> _instance = new Lazy<CodeCoverage>(() => new CodeCoverage());

        /// <summary>
        /// Gets the singleton instance of the CodeCoverage class.
        /// </summary>
        public static CodeCoverage Instance => _instance.Value;
        
        /// <summary>
        /// Dictionary mapping coverage information to the count of times it was hit.
        /// </summary>
        [DataMember]
        private readonly Dictionary<CodeCoverageInfo, int> CoverageCounter = new Dictionary<CodeCoverageInfo, int>();

        /// <summary>
        /// Records a coverage metric emitted from code execution.
        /// </summary>
        /// <param name="label">User-provided label.</param>
        /// <param name="codeLocation">Location in code where metric was emitted.</param>
        /// <param name="customPayload">Custom payload data.</param>
        public void RecordCoverageMetric(string label, string codeLocation, string customPayload)
        {
            var info = new CodeCoverageInfo(label, codeLocation, customPayload);
            
            if (CoverageCounter.TryGetValue(info, out int count))
            {
                CoverageCounter[info] = count + 1;
            }
            else
            {
                CoverageCounter[info] = 1;
            }
        }

        /// <summary>
        /// Gets all recorded coverage metrics and their counts.
        /// </summary>
        /// <returns>A collection of coverage metrics and their counts.</returns>
        public IEnumerable<KeyValuePair<CodeCoverageInfo, int>> GetAllMetrics() => CoverageCounter;

        /// <summary>
        /// Gets the count for a specific coverage metric.
        /// </summary>
        /// <param name="label">The label of the metric.</param>
        /// <param name="codeLocation">The code location of the metric.</param>
        /// <param name="customPayload">The custom payload of the metric.</param>
        /// <returns>The number of times the metric was hit, or 0 if it was not hit.</returns>
        public int GetMetricCount(string label, string codeLocation, string customPayload)
        {
            var info = new CodeCoverageInfo(label, codeLocation, customPayload);
            return GetMetricCount(info);
        }

        /// <summary>
        /// Gets the count for a specific coverage metric.
        /// </summary>
        /// <param name="info">The coverage info to query.</param>
        /// <returns>The number of times the metric was hit, or 0 if it was not hit.</returns>
        public int GetMetricCount(CodeCoverageInfo info)
        {
            return CoverageCounter.TryGetValue(info, out int count) ? count : 0;
        }

        /// <summary>
        /// Gets the total number of unique coverage points that were hit.
        /// </summary>
        public int UniquePointsCount => CoverageCounter.Count;

        /// <summary>
        /// Gets the total number of coverage hits across all metrics.
        /// </summary>
        public int TotalHitsCount => CoverageCounter.Values.Sum();

        /// <summary>
        /// Gets all coverage metrics with a specific label.
        /// </summary>
        /// <param name="label">The label to filter by.</param>
        /// <returns>A collection of coverage metrics with the specified label.</returns>
        public IEnumerable<KeyValuePair<CodeCoverageInfo, int>> GetMetricsByLabel(string label)
        {
            return CoverageCounter.Where(pair => pair.Key.Label == label);
        }

        /// <summary>
        /// Gets all coverage metrics for a specific code location.
        /// </summary>
        /// <param name="codeLocation">The code location to filter by.</param>
        /// <returns>A collection of coverage metrics for the specified code location.</returns>
        public IEnumerable<KeyValuePair<CodeCoverageInfo, int>> GetMetricsByLocation(string codeLocation)
        {
            return CoverageCounter.Where(pair => pair.Key.CodeLocation == codeLocation);
        }

        /// <summary>
        /// Merges another CodeCoverage instance into this one.
        /// </summary>
        /// <param name="other">The other CodeCoverage instance to merge.</param>
        public void Merge(CodeCoverage other)
        {
            if (other == null) return;
            
            foreach (var pair in other.CoverageCounter)
            {
                if (CoverageCounter.TryGetValue(pair.Key, out int count))
                {
                    CoverageCounter[pair.Key] = count + pair.Value;
                }
                else
                {
                    CoverageCounter[pair.Key] = pair.Value;
                }
            }
        }

        /// <summary>
        /// Save the coverage info to the given XML file.
        /// </summary>
        /// <param name="filePath">The path to the file to create.</param>
        public void Save(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Create);
            var settings = new DataContractSerializerSettings
            {
                PreserveObjectReferences = true
            };
            var ser = new DataContractSerializer(typeof(CodeCoverage), settings);
            ser.WriteObject(fs, this);
        }

        /// <summary>
        /// Load the given Coverage info file.
        /// </summary>
        /// <param name="filePath">Path to the file to load.</param>
        /// <returns>The deserialized coverage info.</returns>
        public static CodeCoverage Load(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open);
            using var reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
            var settings = new DataContractSerializerSettings
            {
                PreserveObjectReferences = true
            };
            var ser = new DataContractSerializer(typeof(CodeCoverage), settings);
            return (CodeCoverage)ser.ReadObject(reader, true);
        }

        /// <summary>
        /// Clears all coverage data.
        /// </summary>
        public void Clear()
        {
            CoverageCounter.Clear();
        }
    }
}
