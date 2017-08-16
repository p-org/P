using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker;
using NUnit.Framework;
using UnitTests.CBackend;

namespace UnitTests.PSharpBackend
{
    [TestFixture]
    public class PSharpTests
    {
        [Test]
        public void TestAnalyzeAllTests()
        {
            IEnumerable<TestCaseData> testCases = TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory);
            foreach (TestCaseData testCase in testCases)
            {
                var testDir = (DirectoryInfo)testCase.Arguments[0];
                string testName = new Uri(Constants.TestDirectory + Path.DirectorySeparatorChar).MakeRelativeUri(new Uri(testDir.FullName)).ToString();

                RunTest(testName, testDir.GetFiles("*.p"));
            }
        }

        [Test]
        public void TestAnalyzeTemp()
        {
            RunTest("TEMP", SolutionPath("tmp", "tupOrder.p"), SolutionPath("tmp", "N.p"));
        }

        private static FileInfo SolutionPath(params string[] names)
        {
            return new FileInfo(Path.Combine(new [] {Constants.SolutionDirectory}.Concat(names).ToArray()));
        }

        private static void RunTest(string testName, params FileInfo[] inputFiles)
        {
            try
            {
                var trees = new PParser.ProgramContext[inputFiles.Length];
                var originalFiles = new ParseTreeProperty<FileInfo>();

                for (var i = 0; i < inputFiles.Length; i++)
                {
                    FileInfo inputFile = inputFiles[i];
                    var fileStream = new AntlrFileStream(inputFile.FullName);
                    var lexer = new PLexer(fileStream);
                    var tokens = new CommonTokenStream(lexer);
                    var parser = new PParser(tokens);
                    parser.RemoveErrorListeners();

                    trees[i] = parser.program();

                    if (parser.NumberOfSyntaxErrors != 0)
                    {
                        throw new PParseException(inputFile.FullName);
                    }

                    originalFiles.Put(trees[i], inputFile);
                }

                try
                {
                    Analyzer.Analyze(trees);
                }
                catch (DuplicateDeclarationException e)
                {
                    int badLine = e.Conflicting.SourceNode.Start.Line;
                    int badCol = e.Conflicting.SourceNode.Start.Column;
                    string badFile = originalFiles.Get(GetRoot(e.Conflicting.SourceNode))?.Name;

                    int goodLine = e.Existing.SourceNode.Start.Line;
                    int goodCol = e.Existing.SourceNode.Start.Column;
                    string goodFile = originalFiles.Get(GetRoot(e.Existing.SourceNode))?.Name;

                    Console.Error.WriteLine($"[{testName}] Declaration of {e.Conflicting.Name} at {badFile}:{badLine},{badCol} duplicates the declaration at {goodFile}:{goodLine},{goodCol}");
                }
            }
            catch (PParseException e)
            {
                Console.Error.WriteLine($"[{testName}] {e.Message}");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"[{testName}] UNEXPECTED ERROR: {e.Message}");
            }
        }

        private static RuleContext GetRoot(RuleContext node)
        {
            while (node?.Parent != null)
            {
                node = node.Parent;
            }

            return node;
        }
    }

    public class PParseException : Exception
    {
        public PParseException(string message) : base($"Failed to parse file: {message}") { }
    }
}