using System;
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
                var inputFile = inputFiles[i];
                var fileStream = new AntlrFileStream(inputFile.FullName);
                var lexer = new PLexer(fileStream);
                var tokens = new CommonTokenStream(lexer);
                var parser = new PParser(tokens);
                parser.RemoveErrorListeners();

                trees[i] = parser.program();

                if (parser.NumberOfSyntaxErrors != 0)
                {
                    Console.Error.WriteLine($"[{testName}] Failed to parse {inputFile.FullName}");
                    return;
                }

                originalFiles.Put(trees[i], inputFile);
            }

            Location GetLocation(IPDecl decl)
            {
                return GetRuleLocation(decl.SourceNode);
            }

            Location GetRuleLocation(ParserRuleContext decl)
            {
                if (decl == null)
                    return new Location
                    {
                        Line = -1,
                        Column = -1,
                        File = null
                    };
                return new Location
                {
                    Line = decl.Start.Line,
                    Column = decl.Start.Column,
                    File = originalFiles.Get(GetRoot(decl))
                };
            }

            try
            {
                Analyzer.AnalyzeCompilationUnit(trees);
            }
            catch (DuplicateDeclarationException e)
            {
                var bad = GetLocation(e.Conflicting);
                var good = GetLocation(e.Existing);
                Console.Error.WriteLine(
                                        $"[{testName}] Declaration of {e.Conflicting.Name} at {bad} duplicates the declaration at {good}");
            }
            catch (MissingEventException e)
            {
                var eventSetLocation = GetLocation(e.EventSet);
                Console.Error.WriteLine(
                                        $"[{testName}] Event set {e.EventSet.Name} at {eventSetLocation} references non-existent event {e.EventName}");
            }
            catch (EnumMissingDefaultException e)
            {
                var enumLocation = GetLocation(e.Enum);
                Console.Error.WriteLine(
                                        $"[{testName}] Enum {e.Enum.Name} at {enumLocation} does not have a default 0-element");
            }
            catch (TypeConstructionException e)
            {
                var badTypeLocation = GetRuleLocation(e.Subtree);
                Console.Error.WriteLine($"[{testName}] {badTypeLocation} : {e.Message}");
            }
            catch (DuplicateHandlerException e)
            {
                var badLocation = GetLocation(e.BadEvent);
                Console.Error.WriteLine($"[{testName}] Event {e.BadEvent.Name} has multiple handlers at {badLocation}");
            }
            catch (MissingDeclarationException e)
            {
                var location = GetRuleLocation(e.Location);
                Console.Error.WriteLine($"[{testName}] Could not find declaration {e.Declaration} at {location}");
            }
            catch (NotImplementedException e)
            {
                Console.Error.WriteLine($"[{testName}] Still have to implement {e.Message}");
            }
        }

        private class Location
        {
            public int Line { get; set; }
            public int Column { get; set; }
            public FileInfo File { get; set; }

            public override string ToString()
            {
                return File == null ? "<built-in>" : $"{File.Name}:{Line},{Column}";
            }
        }

        private static IParseTree GetRoot(IParseTree node)
        {
            while (node?.Parent != null)
                node = node.Parent;

            return node;
        }

        [Test]
        public void TestAnalyzeAllTests()
        {
            var testCases = TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory);
            foreach (var testCase in testCases)
            {
                var testDir = (DirectoryInfo) testCase.Arguments[0];
                var testName = new Uri(Constants.TestDirectory + Path.DirectorySeparatorChar)
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
}