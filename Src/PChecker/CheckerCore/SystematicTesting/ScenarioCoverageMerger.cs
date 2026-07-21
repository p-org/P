// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PChecker.SystematicTesting
{
    /// <summary>
    /// Per-scenario coverage for one test case, in a machine-readable form so that
    /// scenario coverage can be aggregated across the many test cases in a suite
    /// (each `p check` runs a single test case).
    /// </summary>
    public class ScenarioCoverageEntry
    {
        /// <summary>Scenario (P monitor) name.</summary>
        public string Name { get; set; }

        /// <summary>True iff the scenario's accepting (cold) state was reached in at least one schedule.</summary>
        public bool Satisfied { get; set; }

        /// <summary>Number of schedules that reached the accepting state (i.e. satisfied the scenario).</summary>
        public int SatisfyingSchedules { get; set; }

        /// <summary>Number of distinct abstract timelines among the satisfying schedules.</summary>
        public int DistinctSatisfyingTimelines { get; set; }

        /// <summary>Furthest partial progress: the most distinct monitor states any single schedule visited.</summary>
        public int MaxStatesVisited { get; set; }

        /// <summary>Total number of states declared in the scenario monitor.</summary>
        public int MonitorStates { get; set; }
    }

    public class ScenarioCoverageArtifact
    {
        /// <summary>Artifact schema version, so the merger can detect/handle format changes.</summary>
        public int Version { get; set; } = ScenarioCoverageMerger.SchemaVersion;
        public string TestCase { get; set; }
        public List<ScenarioCoverageEntry> Scenarios { get; set; } = new();
    }

    /// <summary>
    /// Writes a per-test-case scenario-coverage artifact and merges a directory of them
    /// into one unified, suite-wide scenario-coverage report.
    /// </summary>
    public static class ScenarioCoverageMerger
    {
        /// <summary>Current artifact schema version (bump on any breaking field change).</summary>
        public const int SchemaVersion = 1;

        /// <summary>Suffix identifying a per-run scenario-coverage artifact.</summary>
        public const string FileSuffix = "_scenario_coverage.json";

        // camelCase keys (satisfied, satisfyingSchedules, ...); the same options are used for
        // both serialize and deserialize so the artifact round-trips.
        private static readonly JsonSerializerOptions JsonOptions =
            new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        /// <summary>Builds the artifact for a single completed test case from its report.</summary>
        public static ScenarioCoverageArtifact FromReport(TestReport report, string testCaseName)
        {
            var artifact = new ScenarioCoverageArtifact { TestCase = testCaseName };
            foreach (var scenario in report.ScenarioTriggerCounts.Keys.OrderBy(k => k))
            {
                var satisfyingSchedules = report.ScenarioTriggerCounts[scenario];
                artifact.Scenarios.Add(new ScenarioCoverageEntry
                {
                    Name = scenario,
                    Satisfied = satisfyingSchedules > 0,
                    SatisfyingSchedules = satisfyingSchedules,
                    DistinctSatisfyingTimelines = report.ScenarioSatisfyingTimelines.TryGetValue(scenario, out var tls) ? tls.Count : 0,
                    MaxStatesVisited = report.ScenarioMaxStatesReached.TryGetValue(scenario, out var r) ? r : 0,
                    MonitorStates = report.ScenarioTotalStates.TryGetValue(scenario, out var t) ? t : 0,
                });
            }
            return artifact;
        }

        /// <summary>Serializes the artifact to <paramref name="path"/> (no-op if no scenarios).</summary>
        public static void Write(TestReport report, string testCaseName, string path)
        {
            if (report.ScenarioTriggerCounts.Count == 0)
            {
                return;
            }
            File.WriteAllText(path, JsonSerializer.Serialize(FromReport(report, testCaseName), JsonOptions));
        }

        /// <summary>
        /// Reads every <c>*_scenario_coverage.json</c> artifact under <paramref name="directory"/>
        /// and produces a unified suite-wide report: per scenario, the total triggers and
        /// satisfying timelines SUMMED across test cases (raw timelines are gone by merge time, so
        /// this is a per-test-case sum, not a global-distinct count), the number of test cases that
        /// covered it,
        /// and the best partial progress anywhere.
        /// </summary>
        public static string MergeDirectory(string directory)
        {
            // Keep only the LATEST artifact per test case. The checker rotates each run into
            // history dirs (<Mode>0..9) and the recursion also picks those up, so without this
            // a re-run of a test case would be counted twice. Test-case identity is the JSON's
            // TestCase field; the newest file (by write time) is that test case's latest run.
            var latest = new Dictionary<string, (ScenarioCoverageArtifact artifact, DateTime when)>();
            foreach (var file in Directory.GetFiles(directory, "*" + FileSuffix, SearchOption.AllDirectories))
            {
                try
                {
                    var a = JsonSerializer.Deserialize<ScenarioCoverageArtifact>(File.ReadAllText(file), JsonOptions);
                    if (a == null) continue;
                    // Forward-compat guard: a newer artifact schema may have changed field
                    // meanings, so merging it as v1 would silently miscount. Skip it loudly
                    // rather than produce a wrong number. (This is why Version is written.)
                    if (a.Version > SchemaVersion)
                    {
                        Console.WriteLine($"... Skipping scenario artifact {file}: schema version {a.Version} is " +
                                          $"newer than this tool supports (v{SchemaVersion}); upgrade P to merge it.");
                        continue;
                    }
                    var when = File.GetLastWriteTimeUtc(file);
                    var key = a.TestCase ?? string.Empty;
                    if (!latest.TryGetValue(key, out var cur) || when > cur.when)
                    {
                        latest[key] = (a, when);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"... Skipping unreadable scenario artifact {file}: {e.Message}");
                }
            }
            return Merge(latest.Values.Select(v => v.artifact).ToList());
        }

        /// <summary>
        /// Aggregates artifacts into a unified, human-readable report: a one-line summary,
        /// then the covered scenarios (with WHICH test cases covered them), then the coverage
        /// gaps (never satisfied anywhere) called out last with their best progress.
        /// </summary>
        public static string Merge(IReadOnlyList<ScenarioCoverageArtifact> artifacts)
        {
            // scenario name -> aggregate
            var triggered = new Dictionary<string, int>();
            var uniqueTimelines = new Dictionary<string, int>();
            var testCasesTotal = new Dictionary<string, int>();
            var coveringCases = new Dictionary<string, List<string>>(); // which test cases covered it
            var bestReached = new Dictionary<string, int>();
            var totalStates = new Dictionary<string, int>();

            foreach (var artifact in artifacts)
            {
                var tcName = string.IsNullOrEmpty(artifact.TestCase) ? "(default)" : artifact.TestCase;
                foreach (var s in artifact.Scenarios)
                {
                    triggered[s.Name] = triggered.GetValueOrDefault(s.Name) + s.SatisfyingSchedules;
                    uniqueTimelines[s.Name] = uniqueTimelines.GetValueOrDefault(s.Name) + s.DistinctSatisfyingTimelines;
                    testCasesTotal[s.Name] = testCasesTotal.GetValueOrDefault(s.Name) + 1;
                    if (s.Satisfied)
                    {
                        if (!coveringCases.TryGetValue(s.Name, out var lst))
                        {
                            lst = new List<string>();
                            coveringCases[s.Name] = lst;
                        }
                        lst.Add(tcName);
                    }
                    if (s.MaxStatesVisited > bestReached.GetValueOrDefault(s.Name)) bestReached[s.Name] = s.MaxStatesVisited;
                    if (s.MonitorStates > 0) totalStates[s.Name] = s.MonitorStates;
                }
            }

            var names = triggered.Keys.OrderBy(k => k).ToList();
            var covered = names.Where(n => triggered[n] > 0).ToList();
            var gaps = names.Where(n => triggered[n] == 0).ToList();

            var report = new StringBuilder();
            report.AppendLine($"Unified scenario coverage across {artifacts.Count} test case{(artifacts.Count == 1 ? "" : "s")}:");
            report.AppendLine($"  {covered.Count}/{names.Count} scenarios covered in >=1 test case" +
                (gaps.Count > 0 ? $"; {gaps.Count} coverage gap{(gaps.Count == 1 ? "" : "s")}." : "."));

            foreach (var scenario in covered)
            {
                var cases = coveringCases.TryGetValue(scenario, out var lst) ? lst : new List<string>();
                report.AppendLine(
                    $"  [covered] {scenario}: covered in {cases.Count}/{testCasesTotal[scenario]} test cases " +
                    $"({string.Join(", ", cases)}); {triggered[scenario]} total triggers, " +
                    $"{uniqueTimelines[scenario]} satisfying timelines (summed per test case)");
            }
            foreach (var scenario in gaps)
            {
                report.Append($"  [  GAP  ] {scenario}: never covered in any of {testCasesTotal[scenario]} " +
                              $"test case{(testCasesTotal[scenario] == 1 ? "" : "s")}");
                if (totalStates.TryGetValue(scenario, out var total) && total > 0)
                {
                    report.Append($"; best progress anywhere {bestReached.GetValueOrDefault(scenario)}/{total} states");
                }
                report.AppendLine();
            }
            return report.ToString();
        }
    }
}
