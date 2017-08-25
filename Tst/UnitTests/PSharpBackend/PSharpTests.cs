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

            try
            {
                ITranslationErrorHandler handler = new DefaultTranslationHandler(originalFiles);
                Analyzer.AnalyzeCompilationUnit(handler, trees);
            }
            catch (TranslationException e)
            {
                Console.Error.WriteLine($"[{testName}] {e.Message}");
            }
            catch (NotImplementedException e)
            {
                Console.Error.WriteLine($"[{testName}] Still have to implement {e.Message}");
            }
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