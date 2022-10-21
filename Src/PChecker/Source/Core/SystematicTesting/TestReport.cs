// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Coyote.Coverage;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Class implementing the Coyote test report.
    /// </summary>
    [DataContract]
    public class TestReport
    {
        /// <summary>
        /// Configuration of the program-under-test.
        /// </summary>
        [DataMember]
        public Configuration Configuration { get; private set; }

        /// <summary>
        /// Information regarding code coverage.
        /// </summary>
        [DataMember]
        public CoverageInfo CoverageInfo { get; private set; }

        /// <summary>
        /// Number of explored fair schedules.
        /// </summary>
        [DataMember]
        public int NumOfExploredFairSchedules { get; internal set; }

        /// <summary>
        /// Number of explored unfair schedules.
        /// </summary>
        [DataMember]
        public int NumOfExploredUnfairSchedules { get; internal set; }

        /// <summary>
        /// Number of found bugs.
        /// </summary>
        [DataMember]
        public int NumOfFoundBugs { get; internal set; }

        /// <summary>
        /// Set of unique bug reports.
        /// </summary>
        [DataMember]
        public HashSet<string> BugReports { get; internal set; }

        /// <summary>
        /// The min explored scheduling steps in average,
        /// in fair tests.
        /// </summary>
        [DataMember]
        public int MinExploredFairSteps { get; internal set; }

        /// <summary>
        /// The max explored scheduling steps in average,
        /// in fair tests.
        /// </summary>
        [DataMember]
        public int MaxExploredFairSteps { get; internal set; }

        /// <summary>
        /// The total explored scheduling steps (across
        /// all testing iterations), in fair tests.
        /// </summary>
        [DataMember]
        public int TotalExploredFairSteps { get; internal set; }

        /// <summary>
        /// Number of times the fair max steps bound was hit,
        /// in fair tests.
        /// </summary>
        [DataMember]
        public int MaxFairStepsHitInFairTests { get; internal set; }

        /// <summary>
        /// Number of times the unfair max steps bound was hit,
        /// in fair tests.
        /// </summary>
        [DataMember]
        public int MaxUnfairStepsHitInFairTests { get; internal set; }

        /// <summary>
        /// Number of times the unfair max steps bound was hit,
        /// in unfair tests.
        /// </summary>
        [DataMember]
        public int MaxUnfairStepsHitInUnfairTests { get; internal set; }

        /// <summary>
        /// Set of internal errors. If no internal errors
        /// occurred, then this set is empty.
        /// </summary>
        [DataMember]
        public HashSet<string> InternalErrors { get; internal set; }

        /// <summary>
        /// Lock for the test report.
        /// </summary>
        private readonly object Lock;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestReport"/> class.
        /// </summary>
        public TestReport(Configuration configuration)
        {
            this.Configuration = configuration;

            this.CoverageInfo = new CoverageInfo();

            this.NumOfExploredFairSchedules = 0;
            this.NumOfExploredUnfairSchedules = 0;
            this.NumOfFoundBugs = 0;
            this.BugReports = new HashSet<string>();

            this.MinExploredFairSteps = -1;
            this.MaxExploredFairSteps = -1;
            this.TotalExploredFairSteps = 0;
            this.MaxFairStepsHitInFairTests = 0;
            this.MaxUnfairStepsHitInFairTests = 0;
            this.MaxUnfairStepsHitInUnfairTests = 0;

            this.InternalErrors = new HashSet<string>();

            this.Lock = new object();
        }

        /// <summary>
        /// Merges the information from the specified test report.
        /// </summary>
        /// <returns>True if merged successfully.</returns>
        public bool Merge(TestReport testReport)
        {
            if (!this.Configuration.AssemblyToBeAnalyzed.Equals(testReport.Configuration.AssemblyToBeAnalyzed))
            {
                // Only merge test reports that have the same program name.
                return false;
            }

            lock (this.Lock)
            {
                this.CoverageInfo.Merge(testReport.CoverageInfo);

                this.NumOfFoundBugs += testReport.NumOfFoundBugs;

                this.BugReports.UnionWith(testReport.BugReports);

                this.NumOfExploredFairSchedules += testReport.NumOfExploredFairSchedules;
                this.NumOfExploredUnfairSchedules += testReport.NumOfExploredUnfairSchedules;

                if (testReport.MinExploredFairSteps >= 0 &&
                    (this.MinExploredFairSteps < 0 ||
                    this.MinExploredFairSteps > testReport.MinExploredFairSteps))
                {
                    this.MinExploredFairSteps = testReport.MinExploredFairSteps;
                }

                if (this.MaxExploredFairSteps < testReport.MaxExploredFairSteps)
                {
                    this.MaxExploredFairSteps = testReport.MaxExploredFairSteps;
                }

                this.TotalExploredFairSteps += testReport.TotalExploredFairSteps;

                this.MaxFairStepsHitInFairTests += testReport.MaxFairStepsHitInFairTests;
                this.MaxUnfairStepsHitInFairTests += testReport.MaxUnfairStepsHitInFairTests;
                this.MaxUnfairStepsHitInUnfairTests += testReport.MaxUnfairStepsHitInUnfairTests;

                this.InternalErrors.UnionWith(testReport.InternalErrors);
            }

            return true;
        }

        /// <summary>
        /// Returns the testing report as a string, given a configuration and an optional prefix.
        /// </summary>
        public string GetText(Configuration configuration, string prefix = "")
        {
            StringBuilder report = new StringBuilder();

            report.AppendFormat("{0} Testing statistics:", prefix);

            report.AppendLine();
            report.AppendFormat(
                "{0} Found {1} bug{2}.",
                prefix.Equals("...") ? "....." : prefix,
                this.NumOfFoundBugs,
                this.NumOfFoundBugs == 1 ? string.Empty : "s");

            report.AppendLine();
            report.AppendFormat("{0} Scheduling statistics:", prefix);

            int totalExploredSchedules = this.NumOfExploredFairSchedules +
                this.NumOfExploredUnfairSchedules;

            report.AppendLine();
            report.AppendFormat(
                "{0} Explored {1} schedule{2}: {3} fair and {4} unfair.",
                prefix.Equals("...") ? "....." : prefix,
                totalExploredSchedules,
                totalExploredSchedules == 1 ? string.Empty : "s",
                this.NumOfExploredFairSchedules,
                this.NumOfExploredUnfairSchedules);

            if (totalExploredSchedules > 0 &&
                this.NumOfFoundBugs > 0)
            {
                report.AppendLine();
                report.AppendFormat(
                    "{0} Found {1:F2}% buggy schedules.",
                    prefix.Equals("...") ? "....." : prefix,
                    this.NumOfFoundBugs * 100.0 / totalExploredSchedules);
            }

            if (this.NumOfExploredFairSchedules > 0)
            {
                int averageExploredFairSteps = this.TotalExploredFairSteps /
                    this.NumOfExploredFairSchedules;

                report.AppendLine();
                report.AppendFormat(
                    "{0} Number of scheduling points in fair terminating schedules: {1} (min), {2} (avg), {3} (max).",
                    prefix.Equals("...") ? "....." : prefix,
                    this.MinExploredFairSteps < 0 ? 0 : this.MinExploredFairSteps,
                    averageExploredFairSteps,
                    this.MaxExploredFairSteps < 0 ? 0 : this.MaxExploredFairSteps);

                if (configuration.MaxUnfairSchedulingSteps > 0 &&
                    this.MaxUnfairStepsHitInFairTests > 0)
                {
                    report.AppendLine();
                    report.AppendFormat(
                        "{0} Exceeded the max-steps bound of '{1}' in {2:F2}% of the fair schedules.",
                        prefix.Equals("...") ? "....." : prefix,
                        configuration.MaxUnfairSchedulingSteps,
                        (double)this.MaxUnfairStepsHitInFairTests / this.NumOfExploredFairSchedules * 100);
                }

                if (configuration.UserExplicitlySetMaxFairSchedulingSteps &&
                    configuration.MaxFairSchedulingSteps > 0 &&
                    this.MaxFairStepsHitInFairTests > 0)
                {
                    report.AppendLine();
                    report.AppendFormat(
                        "{0} Hit the max-steps bound of '{1}' in {2:F2}% of the fair schedules.",
                        prefix.Equals("...") ? "....." : prefix,
                        configuration.MaxFairSchedulingSteps,
                        (double)this.MaxFairStepsHitInFairTests / this.NumOfExploredFairSchedules * 100);
                }
            }

            if (this.NumOfExploredUnfairSchedules > 0)
            {
                if (configuration.MaxUnfairSchedulingSteps > 0 &&
                    this.MaxUnfairStepsHitInUnfairTests > 0)
                {
                    report.AppendLine();
                    report.AppendFormat(
                        "{0} Hit the max-steps bound of '{1}' in {2:F2}% of the unfair schedules.",
                        prefix.Equals("...") ? "....." : prefix,
                        configuration.MaxUnfairSchedulingSteps,
                        (double)this.MaxUnfairStepsHitInUnfairTests / this.NumOfExploredUnfairSchedules * 100);
                }
            }

            return report.ToString();
        }

        /// <summary>
        /// Clones the test report.
        /// </summary>
        public TestReport Clone()
        {
            var serializerSettings = new DataContractSerializerSettings();
            serializerSettings.PreserveObjectReferences = true;
            var serializer = new DataContractSerializer(typeof(TestReport), serializerSettings);
            using (var ms = new System.IO.MemoryStream())
            {
                lock (this.Lock)
                {
                    serializer.WriteObject(ms, this);
                    ms.Position = 0;
                    return (TestReport)serializer.ReadObject(ms);
                }
            }
        }
    }
}
