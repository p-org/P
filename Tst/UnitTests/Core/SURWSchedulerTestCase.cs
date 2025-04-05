using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using PChecker;
using PChecker.Runtime.Logging;
using PChecker.SystematicTesting;
using Plang.Compiler;
using Plang.Options;
using UnitTests.Runners;
using UnitTests.Validators;

namespace UnitTests.Core
{
    internal class SURWSchedulerTestCase
    {
        [NUnit.Framework.Test]
        public void TestSURWSchedulerUniformSampling()
        {
            var tempDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory,
                nameof(TestSURWSchedulerUniformSampling)));
            var srcPath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tst", "SchedulerTests", "SURW",
                "assign1.p"));
            var dllPath = Path.Combine(Constants.ScratchParentDirectory, nameof(TestSURWSchedulerUniformSampling),
                "CSharp", "net8.0", "Main.dll");
            var runner = new PCheckerRunner(new[] { srcPath });
            runner.DoCompile(tempDir);

            var configuration =
                new PCheckerOptions().Parse(new[] { dllPath, "-o", tempDir.ToString(), "--sch-surw", "-i", "10000" });
            var engine = TestingEngine.Create(configuration);
            var values = new List<int>();
            engine.RegisterPerIterationCallBack(iter =>
            {
                engine.TryEmitTraces(tempDir.ToString() + Path.DirectorySeparatorChar, "trace");
                string jsonContent = File.ReadAllText(Path.Combine(tempDir.ToString(), $"trace_0.trace.json"));
                List<LogEntry> logEntries = JsonConvert.DeserializeObject<List<LogEntry>>(jsonContent);
                foreach (var logEntry in logEntries)
                {
                    if (logEntry.Type == "Print" && logEntry.Details.Log?.StartsWith("Value:") == true)
                    {
                        string valueString = logEntry.Details.Log.Split(':')[1].Trim();
                        if (int.TryParse(valueString, out int value) && value > 0)
                        {
                            values.Add(value);
                        }
                    }
                }
            });
            engine.Run();
            Assert.IsTrue(values.Count > 0, "No values were collected from the test run");

            var valueFrequencies = values.GroupBy(v => v)
                .ToDictionary(g => g.Key, g => g.Count());
            double meanFrequency = valueFrequencies.Values.Average();
            double varianceSum = valueFrequencies.Values.Sum(f => Math.Pow(f - meanFrequency, 2));
            double variance = varianceSum / valueFrequencies.Count;
            double normalizedVariance = Math.Sqrt(variance) / meanFrequency;
            Assert.Less(normalizedVariance, 0.2, 
                $"Variance too high for uniform sampling: {normalizedVariance:F4}");
        }
    }
}
