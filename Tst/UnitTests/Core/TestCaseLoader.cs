using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace UnitTests.Core
{
    /// <summary>
    /// Load test cases from disk for NUnit
    /// </summary>
    public static class TestCaseLoader
    {
        private static readonly List<string> TestDirs = new List<string>
        {
            Path.Combine("RegressionTests","Combined"),
            Path.Combine("RegressionTests","Feature1SMLevelDecls"),
            Path.Combine("RegressionTests","Feature2Stmts"),
            Path.Combine("RegressionTests","Feature3Exprs"),
            Path.Combine("RegressionTests","Feature4DataTypes"),
            Path.Combine("RegressionTests","Integration")
        };

        public static IEnumerable<TestCaseData> FindTestCasesInDirectory(string directoryName)
        {
            var testDirNames = new[] {"Pc", "Prt"};
            return from testDir in TestDirs
                let baseDirectory = new DirectoryInfo(Path.Combine(directoryName, testDir))
                from testCaseDir in baseDirectory.EnumerateDirectories("*.*", SearchOption.AllDirectories)
                where testCaseDir.GetDirectories().Select(dir => dir.Name).Any(info => testDirNames.Contains(info))
                select DirectoryToTestCase(testCaseDir, baseDirectory);
        }

        private static TestCaseData DirectoryToTestCase(DirectoryInfo dir, DirectoryInfo testRoot)
        {
            TestConfig runConfig = null;
            string configPath = Path.Combine(dir.FullName, "Prt", Constants.TestConfigFileName);
            if (File.Exists(configPath))
            {
                var variables = GetVariables(testRoot);
                runConfig = ParseTestConfig(configPath, variables);
            }

            DirectoryInfo tempDir = Directory.CreateDirectory(Constants.ScratchParentDirectory);
            var factory = new TestCaseFactory(tempDir);
            CompilerTestCase testCase = factory.CreateTestCase(dir, runConfig);

            string category = testRoot.Name + Constants.CategorySeparator + GetCategory(dir, testRoot);
            string testName = category + Constants.CategorySeparator + dir.Name;
            return new TestCaseData(testCase).SetName(testName).SetCategory(category);
        }

        private static Dictionary<string, string> GetVariables(DirectoryInfo testRoot)
        {
            string binDir = Path.Combine(Constants.SolutionDirectory, "bld", "drops", Constants.BuildConfiguration, Constants.Platform,
                                         "binaries");
            var variables = new Dictionary<string, string>
            {
                {"platform", Constants.Platform},
                {"testroot", testRoot.FullName},
                {"configuration", Constants.BuildConfiguration},
                {"testbinaries", binDir}
            };
            return variables;
        }

        private static string GetCategory(DirectoryInfo dir, DirectoryInfo baseDirectory)
        {
            var category = "";
            var sep = "";
            dir = dir.Parent;
            while (dir != null && dir.FullName != baseDirectory.FullName)
            {
                //category = $"{category}{sep}{dir.Name}";
                category = $"{dir.Name}{sep}{category}";
                dir = dir.Parent;
                sep = Constants.CategorySeparator;
            }

            return category;
        }

        private static TestConfig ParseTestConfig(string testConfigPath, IDictionary<string, string> variables)
        {
            var testConfig = new TestConfig();

            foreach (string assignment in File.ReadLines(testConfigPath))
            {
                if (string.IsNullOrWhiteSpace(assignment))
                {
                    continue;
                }

                var parts = assignment.Split(new[] {':'}, 2).Select(x => x.Trim()).ToArray();
                string key = parts[0];
                string value = SubstituteVariables(parts[1], variables);
                switch (key)
                {
                    case "inc":
                        testConfig.Includes.Add(value);
                        break;
                    case "del":
                        testConfig.Deletes.Add(value);
                        break;
                    case "arg":
                        testConfig.Arguments.Add(value);
                        break;
                    case "dsc":
                        testConfig.Description = value;
                        break;
                    case "link":
                        testConfig.Link = value;
                        break;
                    default:
                        Debug.WriteLine($"Unrecognized option '{key}' in file ${testConfigPath}");
                        break;
                }
            }

            return testConfig;
        }

        private static string SubstituteVariables(string value, IDictionary<string, string> variables)
        {
            // Replaces variables that use a syntax like $(VarName).
            return Regex.Replace(value, @"\$\((?<VarName>[^)]+)\)", match =>
            {
                string variableName = match.Groups["VarName"].Value.ToLowerInvariant();
                return variables.TryGetValue(variableName, out string variableValue) ? variableValue : match.Value;
            });
        }
    }
}
