using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Pc;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.Backend;
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

        private class PParserErrorListener : IAntlrErrorListener<IToken>
        {
            private readonly ITranslationErrorHandler handler;
            private readonly FileInfo inputFile;

            public PParserErrorListener(FileInfo inputFile, ITranslationErrorHandler handler)
            {
                this.inputFile = inputFile;
                this.handler = handler;
            }

            public void SyntaxError(
                IRecognizer recognizer,
                IToken offendingSymbol,
                int line,
                int charPositionInLine,
                string msg,
                RecognitionException e)
            {
                throw handler.ParseFailure(inputFile, $"line {line}:{charPositionInLine} {msg}");
            }
        }

        private static IDictionary<string, string> LoadRegressionsFile()
        {
            var result = new Dictionary<string, string>();
            string setPath = Path.Combine(Constants.TestDirectory, Constants.FrontEndRegressionFileName);
            if (!File.Exists(setPath))
            {
                return result;
            }

            var lines = File.ReadAllLines(setPath);
            if (lines.Length % 2 != 0)
            {
                return result;
            }

            for (var i = 0; i < lines.Length; i += 2)
            {
                result.Add(lines[i], lines[i+1]);
            }
            return result;
        }

        private void SaveRegressionsFile(IDictionary<string, string> set)
        {
            string setPath = Path.Combine(Constants.TestDirectory, Constants.FrontEndRegressionFileName);
            using (var file = new StreamWriter(setPath))
            {
                foreach (var entry in set)
                {
                    file.WriteLine(entry.Key);
                    file.WriteLine(entry.Value);
                }
            }
        }

        private static string RunTest(string testName, params FileInfo[] inputFiles)
        {
            bool expectCorrect = testName.Contains("Correct") || testName.Contains("DynamicError");
            try
            {
                var trees = new PParser.ProgramContext[inputFiles.Length];
                var originalFiles = new ParseTreeProperty<FileInfo>();
                ITranslationErrorHandler handler = new DefaultTranslationErrorHandler(originalFiles);

                for (var i = 0; i < inputFiles.Length; i++)
                {
                    FileInfo inputFile = inputFiles[i];
                    var fileStream = new AntlrFileStream(inputFile.FullName);
                    var lexer = new PLexer(fileStream);
                    var tokens = new CommonTokenStream(lexer);
                    var parser = new PParser(tokens);
                    parser.RemoveErrorListeners();
                    parser.AddErrorListener(new PParserErrorListener(inputFile, handler));

                    trees[i] = parser.program();
                    originalFiles.Put(trees[i], inputFile);
                }

                PProgramModel program = Analyzer.AnalyzeCompilationUnit(handler, trees);
                if (!expectCorrect)
                {
                    return $"[{testName}] Expected error, but none were found!";
                }
            }
            catch (TranslationException e)
            {
                if (expectCorrect)
                {
                    return $"[{testName}] Expected correct, but error was found: {e.Message}";
                }
            }
            catch (NotImplementedException e)
            {
                return $"[{testName}] {e.Message} not implemented.";
            }
            return null;
        }

        [Test]
        public void TestAnalyzeAllTests()
        {
            var baseline = LoadRegressionsFile();
            var currentSet = new Dictionary<string, string>();

            var pass = true;

            var testCases = TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory);
            foreach (TestCaseData testCase in testCases)
            {
                var testDir = (DirectoryInfo) testCase.Arguments[0];
                string testName = new Uri(Constants.TestDirectory + Path.DirectorySeparatorChar)
                    .MakeRelativeUri(new Uri(testDir.FullName))
                    .ToString();

                string result = RunTest(testName, testDir.GetFiles("*.p"));
                if (result != null)
                {
                    currentSet.Add(testName, result);
                }

                baseline.TryGetValue(testName, out string oldResult);
                if (result != null)
                {
                    if (oldResult != null)
                    {
                        if (!result.Equals(oldResult))
                        {
                            pass = false;
                            Console.Error.WriteLine($"*** {result}");
                        }
                    }
                    else
                    {
                        pass = false;
                        Console.Error.WriteLine($"+++ {result}");
                    }
                }
                else if (oldResult != null)
                {
                    Console.Error.WriteLine($"--- {oldResult}");
                }
            }

            if (baseline.Count == 0 || pass)
            {
                SaveRegressionsFile(currentSet);
            }

            if (pass && currentSet.Count > 0)
            {
                Console.WriteLine($"Still failing {currentSet.Count} test cases.");
                foreach (string result in currentSet.Values)
                {
                    Console.WriteLine(result);
                }
            }

            Assert.IsTrue(pass);
        }

        [Test]
        public void TestAnalyzeTemp()
        {
            RunTest("TEMP", SolutionPath("tmp", "tupOrder.p"), SolutionPath("tmp", "N.p"));
        }
    }
}
