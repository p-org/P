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
        private static FileInfo SolutionPath(params string[] names)
        {
            return new FileInfo(Path.Combine(new[] {Constants.SolutionDirectory}.Concat(names).ToArray()));
        }

        private static void RunTest(string testName, params FileInfo[] inputFiles)
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
                    Console.Error.WriteLine($"[{testName}] {inputFile.FullName}");
                    return;
                }

                originalFiles.Put(trees[i], inputFile);
            }

            Location GetLocation(IPDecl decl)
            {
                return GetTreeLocation(decl.SourceNode);
            }

            Location GetTreeLocation(ParserRuleContext decl)
            {
                return new Location
                {
                    Line = decl.Start.Line,
                    Column = decl.Start.Column,
                    File = originalFiles.Get(GetRoot(decl))
                };
            }

            try
            {
                Analyzer.Analyze(trees);
            }
            catch (DuplicateDeclarationException e)
            {
                Location bad = GetLocation(e.Conflicting);
                Location good = GetLocation(e.Existing);
                Console.Error.WriteLine($"[{testName}] Declaration of {e.Conflicting.Name} at {bad} duplicates the declaration at {good}");
            }
            catch (MissingEventException e)
            {
                Location eventSetLocation = GetLocation(e.EventSet);
                Console.Error.WriteLine(
                    $"[{testName}] Event set {e.EventSet.Name} at {eventSetLocation} references non-existent event {e.EventName}");
            }
            catch (EnumMissingDefaultException e)
            {
                Location enumLocation = GetLocation(e.Enum);
                Console.Error.WriteLine($"[{testName}] Enum {e.Enum.Name} at {enumLocation} does not have a default 0-element");
            }
            catch (TypeConstructionException e)
            {
                Location badTypeLocation = GetTreeLocation(e.Subtree);
                Console.Error.WriteLine($"[{testName}] {badTypeLocation} : {e.Message}");
            }
        }

        private class Location
        {
            public int Line { get; set; }
            public int Column { get; set; }
            public FileInfo File { get; set; }

            public override string ToString()
            {
                return $"{File.Name}:{Line},{Column}";
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

        [Test]
        public void TestAnalyzeAllTests()
        {
            IEnumerable<TestCaseData> testCases = TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory);
            foreach (TestCaseData testCase in testCases)
            {
                var testDir = (DirectoryInfo) testCase.Arguments[0];
                string testName = new Uri(Constants.TestDirectory + Path.DirectorySeparatorChar)
                    .MakeRelativeUri(new Uri(testDir.FullName)).ToString();

                RunTest(testName, testDir.GetFiles("*.p"));
            }
        }

        [Test]
        public void TestAnalyzeTemp()
        {
            RunTest("TEMP", SolutionPath("tmp", "tupOrder.p"), SolutionPath("tmp", "N.p"));
        }
    }

    public class PParseException : Exception
    {
        public PParseException(string message) : base($"Failed to parse file: {message}") { }
    }
}