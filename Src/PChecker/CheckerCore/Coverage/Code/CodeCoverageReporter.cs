// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PChecker.Coverage.Code
{
    /// <summary>
    /// Generates coverage reports from code coverage data.
    /// </summary>
    public class CodeCoverageReporter
    {
        private readonly CodeCoverage _codeCoverage;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeCoverageReporter"/> class.
        /// </summary>
        /// <param name="codeCoverage">The code coverage data to report on.</param>
        public CodeCoverageReporter(CodeCoverage codeCoverage)
        {
            _codeCoverage = codeCoverage;
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
            var metrics = _codeCoverage.GetAllMetrics().ToList();
            
            // Write summary
            WriteHeader(writer, "Code Coverage Summary");
            writer.WriteLine($"Total unique coverage points: {metrics.Count}");
            writer.WriteLine($"Total coverage hits: {_codeCoverage.TotalHitsCount}");
            writer.WriteLine();

            // Group by label
            var labelGroups = metrics
                .GroupBy(m => m.Key.Label)
                .OrderBy(g => g.Key);

            // Write coverage by label
            WriteHeader(writer, "Coverage by Label");
            foreach (var group in labelGroups)
            {
                var label = string.IsNullOrEmpty(group.Key) ? "<empty>" : group.Key;
                var totalHits = group.Sum(m => m.Value);
                writer.WriteLine($"Label: {label}");
                writer.WriteLine($"  Coverage points: {group.Count()}");
                writer.WriteLine($"  Total hits: {totalHits}");
                
                // List all coverage points for this label
                foreach (var metric in group.OrderByDescending(m => m.Value))
                {
                    writer.WriteLine($"    Location: {metric.Key.CodeLocation}, Hits: {metric.Value}");
                    if (!string.IsNullOrEmpty(metric.Key.CustomPayload))
                    {
                        writer.WriteLine($"      Payload: {metric.Key.CustomPayload}");
                    }
                }
                
                writer.WriteLine();
            }

            // Group by location
            var locationGroups = metrics
                .GroupBy(m => m.Key.CodeLocation)
                .OrderBy(g => g.Key);

            // Write coverage by location
            WriteHeader(writer, "Coverage by Location");
            foreach (var group in locationGroups)
            {
                var location = string.IsNullOrEmpty(group.Key) ? "<empty>" : group.Key;
                var totalHits = group.Sum(m => m.Value);
                writer.WriteLine($"Location: {location}");
                writer.WriteLine($"  Coverage points: {group.Count()}");
                writer.WriteLine($"  Total hits: {totalHits}");
                
                // List all coverage points for this location
                foreach (var metric in group.OrderByDescending(m => m.Value))
                {
                    writer.WriteLine($"    Label: {metric.Key.Label}, Hits: {metric.Value}");
                    if (!string.IsNullOrEmpty(metric.Key.CustomPayload))
                    {
                        writer.WriteLine($"      Payload: {metric.Key.CustomPayload}");
                    }
                }
                
                writer.WriteLine();
            }
        }

        /// <summary>
        /// Generates a CSV report of the coverage data.
        /// </summary>
        /// <param name="csvFile">Path to the CSV file to create.</param>
        public void EmitCsvReport(string csvFile)
        {
            using (var writer = new StreamWriter(csvFile))
            {
                // Write header
                writer.WriteLine("Label,CodeLocation,CustomPayload,Hits");
                
                // Write metrics
                foreach (var metric in _codeCoverage.GetAllMetrics().OrderByDescending(m => m.Value))
                {
                    var label = EscapeCsvField(metric.Key.Label);
                    var location = EscapeCsvField(metric.Key.CodeLocation);
                    var payload = EscapeCsvField(metric.Key.CustomPayload);
                    
                    writer.WriteLine($"{label},{location},{payload},{metric.Value}");
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
        
        /// <summary>
        /// Escapes a field for CSV output.
        /// </summary>
        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;
                
            if (field.Contains("\"") || field.Contains(",") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            
            return field;
        }
    }
}
