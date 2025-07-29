using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using UnitTests.Core;

namespace UnitTests
{
    public static class TestAssertions
    {
        public static void AssertTestCase(CompilerTestCase testCase)
        {
            if (!testCase.EvaluateTest(out var stdout, out var stderr, out var exitCode))
            {
                Console.WriteLine("Test failed!\n");
                WriteOutput(stdout, stderr, exitCode);
                Assert.Fail($"EXIT: {exitCode}\n{stderr}\n{stdout}");
            }

            Console.WriteLine("Test succeeded!\n");
            WriteOutput(stdout, stderr, exitCode);

            // Delete ONLY if inside the solution directory
            SafeDeleteDirectory(testCase.ScratchDirectory);
        }
        
        public static void SafeDeleteDirectory(DirectoryInfo toDelete)
        {
            var safeBase = new DirectoryInfo(Constants.SolutionDirectory);
            var retrySleepDurationMs = 200;
            for (var scratch = toDelete; scratch.Parent != null; scratch = scratch.Parent)
            {
                if (string.Compare(scratch.FullName, safeBase.FullName, StringComparison.InvariantCultureIgnoreCase) ==
                    0)
                {
                    if (toDelete.Exists)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            try
                            {
                                toDelete.Delete(true);
                                break;
                            }
                            catch (IOException)
                            {
                                Thread.Sleep(retrySleepDurationMs);
                            }
                            catch (UnauthorizedAccessException)
                            {
                                Thread.Sleep(retrySleepDurationMs);
                            }
                        }
                    }

                    return;
                }
            }
        }

        private static void WriteOutput(string stdout, string stderr, int? exitCode)
        {
            if (!string.IsNullOrEmpty(stdout))
            {
                Console.WriteLine($"STDOUT\n======\n{stdout}\n\n");
            }

            if (!string.IsNullOrEmpty(stderr))
            {
                Console.WriteLine($"STDERR\n======\n{stderr}\n\n");
            }

            if (exitCode != null)
            {
                Console.WriteLine($"Exit code = {exitCode}");
            }
        }
    }
}