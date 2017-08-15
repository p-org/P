using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
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
                Uri testName = new Uri(Constants.TestDirectory + Path.DirectorySeparatorChar).MakeRelativeUri(new Uri(testDir.FullName));

                try
                {
                    FileInfo[] inputFiles = testDir.GetFiles("*.p");
                    var trees = new PParser.ProgramContext[inputFiles.Length];

                    for (var i = 0; i < inputFiles.Length; i++)
                    {
                        FileInfo inputFile = inputFiles[i];
                        var fileStream = new AntlrFileStream(inputFile.FullName);
                        var lexer = new PLexer(fileStream);
                        var tokens = new CommonTokenStream(lexer);
                        var parser = new PParser(tokens);

                        trees[i] = parser.program();

                        if (parser.NumberOfSyntaxErrors != 0)
                        {
                            throw new PParseException(inputFile.FullName);
                        }
                    }

                    try
                    {
                        Analyzer.Analyze(trees);
                        Console.Error.WriteLine($"[{testName}] Success!");
                    }
                    catch (DuplicateDeclarationException e)
                    {
                        int badLine = e.ConflictingNameNode.SourceNode.Start.Line;
                        int badCol = e.ConflictingNameNode.SourceNode.Start.Column;
                        int goodLine = e.ExistingDeclarationNode.SourceNode.Start.Line;
                        int goodCol = e.ExistingDeclarationNode.SourceNode.Start.Column;
                        Console.Error.WriteLine($"[{testName}] Declaration at {goodLine}:{goodCol} conflicts with {badLine}:{badCol} ");
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
        }
    }

    public class PParseException : Exception
    {
        public PParseException(string message) : base($"Failed to parse file: {message}") { }
    }
}