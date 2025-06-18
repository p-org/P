using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Plang;
using Plang.Compiler;
using Plang.Options;
using PChecker;
using PChecker.Testing;

namespace UnitTests
{
    [TestFixture]
    public class ParametricRegressionTests
    {
        private string GetProjectRoot()
        {
            // Start from the current directory and walk up until we find the P.sln file
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "P.sln")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new Exception("Could not find P project root directory");
            }

            return directory.FullName;
        }

        [Test]
        public void TestClientServerParametric()
        {
            // Path to the project file
            string projectPath = Path.Combine(GetProjectRoot(), "Tst", "TestParamTester", "1_ClientServer", "ClientServer.pproj");

            // First compile the project
            string[] compileArgs = new[] { "compile", "--pproj", projectPath };
            CommandLine.RunCompiler(compileArgs.Skip(1).ToArray());

            // Then run different test cases
            string[] testCases = new[]
            {
                "tcSingleClient",  // Basic test
                "aaaa1___nClients_2__g1_1__g2_4",  // Parametric test
                "testTWise2___nClients_4__g1_1__g2_4__b1_True"  // T-wise test
            };

            foreach (var testCase in testCases)
            {
                var checkerOptions = new PCheckerOptions();
                var config = checkerOptions.Parse(new[] { "--testcase", testCase });
                Checker.Run(config);
            }
        }

        [Test]
        public void TestClientServerAllParametricCombinations()
        {
            // Path to the project file
            string projectPath = Path.Combine(GetProjectRoot(), "Tst", "TestParamTester", "1_ClientServer", "ClientServer.pproj");

            // First compile the project
            string[] compileArgs = new[] { "compile", "--pproj", projectPath };
            CommandLine.RunCompiler(compileArgs.Skip(1).ToArray());

            // Run all parametric test combinations
            for (int nClients = 2; nClients <= 4; nClients++)
            {
                for (int g1 = 1; g1 <= 2; g1++)
                {
                    for (int g2 = 4; g2 <= 5; g2++)
                    {
                        string testCase = $"aaaa1___nClients_{nClients}__g1_{g1}__g2_{g2}";
                        var checkerOptions = new PCheckerOptions();
                        var config = checkerOptions.Parse(new[] { "--testcase", testCase });
                        Checker.Run(config);
                    }
                }
            }
        }

        [Test]
        public void TestClientServerTWise()
        {
            // Path to the project file
            string projectPath = Path.Combine(GetProjectRoot(), "Tst", "TestParamTester", "1_ClientServer", "ClientServer.pproj");

            // First compile the project
            string[] compileArgs = new[] { "compile", "--pproj", projectPath };
            CommandLine.RunCompiler(compileArgs.Skip(1).ToArray());

            // Run T-wise tests with different T values
            string[] tWiseTests = new[]
            {
                "testTWise1___nClients_4__g1_1__g2_4__b1_True",  // 1-wise
                "testTWise2___nClients_4__g1_1__g2_4__b1_True",  // 2-wise
                "testTWise3___nClients_4__g1_1__g2_4__b1_True",  // 3-wise
                "testTWise4___nClients_4__g1_1__g2_4__b1_True"   // 4-wise
            };

            foreach (var testCase in tWiseTests)
            {
                var checkerOptions = new PCheckerOptions();
                var config = checkerOptions.Parse(new[] { "--testcase", testCase });
                Checker.Run(config);
            }
        }
    }
}
