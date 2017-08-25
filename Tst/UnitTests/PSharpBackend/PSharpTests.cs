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

        private static readonly HashSet<string> testWhitelist = new HashSet<string>
        {
            "RegressionTests/Feature2Stmts/Correct/receive1",
            "RegressionTests/Feature2Stmts/Correct/receive17",
            "RegressionTests/Feature2Stmts/DynamicError/receive2",
            "RegressionTests/Feature4DataTypes/Correct/nonAtomicDataTypes12",
            "RegressionTests/Feature4DataTypes/Correct/nonAtomicDataTypes13",
            "RegressionTests/Integration/Correct/German"
        };

        private static void RunTest(string testName, params FileInfo[] inputFiles)
        {
            try
            {
                var trees = new PParser.ProgramContext[inputFiles.Length];
                var originalFiles = new ParseTreeProperty<FileInfo>();
                ITranslationErrorHandler handler = new DefaultTranslationHandler(originalFiles);

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
                        throw handler.ParseFailure(inputFile);
                    }

                    originalFiles.Put(trees[i], inputFile);
                }

                Analyzer.AnalyzeCompilationUnit(handler, trees);
                Console.WriteLine($"[{testName}] Success!");
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
            foreach (TestCaseData testCase in testCases)
            {
                var testDir = (DirectoryInfo) testCase.Arguments[0];
                string testName = new Uri(Constants.TestDirectory + Path.DirectorySeparatorChar)
                    .MakeRelativeUri(new Uri(testDir.FullName))
                    .ToString();

                if (testWhitelist.Contains(testName))
                {
                    RunTest(testName, testDir.GetFiles("*.p"));
                }
            }
        }

        [Test]
        public void TestAnalyzeTemp()
        {
            RunTest("TEMP", SolutionPath("tmp", "tupOrder.p"), SolutionPath("tmp", "N.p"));
        }
    }
}
