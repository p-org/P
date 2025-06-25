// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PChecker.Coverage.Code;
using PChecker.Coverage.Event;

namespace PChecker.Coverage
{
    /// <summary>
    /// Reporter that combines code and event coverage information.
    /// </summary>
    public class CoverageReporter
    {
        /// <summary>
        /// The code coverage information.
        /// </summary>
        private readonly CodeCoverage CodeCoverage;

        /// <summary>
        /// The event coverage information.
        /// </summary>
        private readonly EventCoverageInfo EventCoverageInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoverageReporter"/> class.
        /// </summary>
        /// <param name="codeCoverage">The code coverage data.</param>
        /// <param name="eventCoverageInfo">The event coverage data.</param>
        public CoverageReporter(CodeCoverage codeCoverage, EventCoverageInfo eventCoverageInfo)
        {
            this.CodeCoverage = codeCoverage;
            this.EventCoverageInfo = eventCoverageInfo;
        }

        /// <summary>
        /// Emits the combined coverage report to the specified file.
        /// </summary>
        /// <param name="outputFilePath">The output file path.</param>
        public void EmitCoverageReport(string outputFilePath)
        {
            using (var writer = new StreamWriter(outputFilePath))
            {
                WriteCombinedCoverageReport(writer);
            }
        }

        /// <summary>
        /// Emits the code coverage data in CSV format.
        /// </summary>
        /// <param name="outputFilePath">The output file path.</param>
        public void EmitCodeCoverageCsvReport(string outputFilePath)
        {
            if (CodeCoverage != null)
            {
                var reporter = new CodeCoverageReporter(CodeCoverage);
                reporter.EmitCsvReport(outputFilePath);
            }
        }

        /// <summary>
        /// Writes the combined coverage report to the specified text writer.
        /// </summary>
        /// <param name="writer">The text writer.</param>
        private void WriteCombinedCoverageReport(TextWriter writer)
        {
            // Write the global coverage header
            writer.WriteLine("======================================================");
            writer.WriteLine("      P Coverage Report - Combined Coverage Data      ");
            writer.WriteLine("======================================================");
            writer.WriteLine();

            // Write overall summary
            WriteCoverageSummary(writer);
            writer.WriteLine();

            // Write code coverage details if available
            if (CodeCoverage != null && CodeCoverage.GetAllMetrics().Any())
            {
                WriteCodeCoverageDetails(writer);
                writer.WriteLine();
            }

            // Write event coverage details
            if (EventCoverageInfo != null)
            {
                WriteEventCoverageDetails(writer);
            }
        }

        /// <summary>
        /// Writes the coverage summary to the specified text writer.
        /// </summary>
        /// <param name="writer">The text writer.</param>
        private void WriteCoverageSummary(TextWriter writer)
        {
            writer.WriteLine("Coverage Summary:");
            writer.WriteLine("----------------");
            
            // Code Coverage Summary
            if (CodeCoverage != null)
            {
                var metrics = CodeCoverage.GetAllMetrics().ToList();
                var hitPoints = metrics.Count(m => m.Value > 0);
                var totalPoints = metrics.Count;
                
                var coveragePercentage = totalPoints > 0 
                    ? (double)hitPoints / totalPoints * 100 
                    : 0;
                
                writer.WriteLine($"Code Coverage: {hitPoints} of {totalPoints} points covered ({coveragePercentage:F2}%)");
            }
            
            // Event Coverage Summary
            if (EventCoverageInfo != null)
            {
                var (handledEvents, totalEvents) = GetEventCoverageStats();
                
                var eventCoveragePercentage = totalEvents > 0 
                    ? (double)handledEvents / totalEvents * 100 
                    : 0;
                
                writer.WriteLine($"Event Coverage: {handledEvents} of {totalEvents} events handled ({eventCoveragePercentage:F2}%)");
            }
        }

        /// <summary>
        /// Writes the code coverage details to the specified text writer.
        /// </summary>
        /// <param name="writer">The text writer.</param>
        private void WriteCodeCoverageDetails(TextWriter writer)
        {
            writer.WriteLine("Code Coverage Details:");
            writer.WriteLine("---------------------");
            
            // Group by label for better organization
            var pointsByLabel = CodeCoverage.GetAllMetrics()
                .GroupBy(m => m.Key.Label)
                .OrderBy(g => g.Key);
            
            foreach (var group in pointsByLabel)
            {
                writer.WriteLine($"\nLabel: {group.Key}");
                writer.WriteLine(new string('-', group.Key.Length + 7));
                
                // Sort points by location
                foreach (var point in group.OrderBy(p => p.Key.CodeLocation))
                {
                    var hitCount = point.Value;
                    var status = hitCount > 0 ? "Covered" : "Not Covered";
                    var hitDetails = hitCount > 0 ? $" (Hit {hitCount} times)" : string.Empty;
                    
                    writer.WriteLine($"  {point.Key.CodeLocation} - {status}{hitDetails}");
                    
                    if (!string.IsNullOrEmpty(point.Key.CustomPayload))
                    {
                        writer.WriteLine($"    Payload: {point.Key.CustomPayload}");
                    }
                }
            }
        }

        /// <summary>
        /// Writes the event coverage details to the specified text writer.
        /// </summary>
        /// <param name="writer">The text writer.</param>
        private void WriteEventCoverageDetails(TextWriter writer)
        {
            writer.WriteLine("Event Coverage Details:");
            writer.WriteLine("----------------------");
            
            if (EventCoverageInfo.EventInfo != null)
            {
                // Group by machine
                foreach (var machine in EventCoverageInfo.Machines.OrderBy(m => m))
                {
                    writer.WriteLine($"\nMachine: {machine}");
                    writer.WriteLine(new string('-', machine.Length + 9));
                    
                    var states = EventCoverageInfo.MachinesToStates.TryGetValue(machine, out var machineStates)
                        ? machineStates.OrderBy(s => s)
                        : Enumerable.Empty<string>();
                    foreach (var state in states)
                    {
                        writer.WriteLine($"\n  State: {state}");
                        
                        // Report all events that can be handled in this state
                        var key = machine + "." + state;
                        if (EventCoverageInfo.RegisteredEvents.TryGetValue(key, out var stateEvents) && stateEvents.Any())
                        {
                            foreach (var eventName in stateEvents.OrderBy(e => e))
                            {
                                var wasHandled = EventCoverageInfo.EventInfo != null && 
                                                EventCoverageInfo.EventInfo.IsEventHandled(machine, state, eventName);
                                var status = wasHandled ? "Handled" : "Not Handled";
                                writer.WriteLine($"    Event: {eventName} - {status}");
                            }
                        }
                        else
                        {
                            writer.WriteLine("    No events declared for this state.");
                        }
                    }
                }
            }
            else
            {
                writer.WriteLine("\nNo event coverage data available.");
            }
        }

        /// <summary>
        /// Gets the event coverage statistics.
        /// </summary>
        /// <returns>A tuple containing the number of handled events and total events.</returns>
        private (int HandledEvents, int TotalEvents) GetEventCoverageStats()
        {
            int handledEvents = 0;
            int totalEvents = 0;
            
            foreach (var machine in EventCoverageInfo.Machines)
            {
                if (EventCoverageInfo.MachinesToStates.TryGetValue(machine, out var states))
                {
                    foreach (var state in states)
                    {
                        var key = machine + "." + state;
                        if (EventCoverageInfo.RegisteredEvents.TryGetValue(key, out var stateEvents))
                        {
                            foreach (var eventName in stateEvents)
                            {
                                totalEvents++;
                                if (EventCoverageInfo.EventInfo != null && 
                                    EventCoverageInfo.EventInfo.IsEventHandled(machine, state, eventName))
                                {
                                    handledEvents++;
                                }
                            }
                        }
                    }
                }
            }
            
            return (handledEvents, totalEvents);
        }
    }
}
