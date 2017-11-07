using System;
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

        private static void RunTest(string testName, params FileInfo[] inputFiles)
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
//                string generatedCode = CodeGen.GenerateCode(TargetLanguage.PSharp,
//                                                            new PSharpProgramModel
//                                                            {
//                                                                GlobalScope = program.GlobalScope,
//                                                                Namespace = "Program"
//                                                            });
//                Console.WriteLine(generatedCode);

                if (!expectCorrect)
                {
                    Console.Error.WriteLine($"[{testName}] Expected error, but none were found!");
                }
            }
            catch (TranslationException e)
            {
                if (expectCorrect)
                {
                    Console.Error.WriteLine($"[{testName}] Expected correct, but error was found: {e.Message}");
                }
            }
            catch (NotImplementedException e)
            {
                Console.Error.WriteLine($"[{testName}] {e.Message} not implemented.");
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
