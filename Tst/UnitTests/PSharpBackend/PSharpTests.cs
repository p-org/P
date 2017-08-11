using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using Microsoft.Pc;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker;
using NUnit.Framework;

namespace UnitTests.PSharpBackend
{
    [TestFixture]
    public class PSharpTests
    {
        //private const string PSourcePath = @"RegressionTests\Feature4DataTypes\Correct\ReturnIssue\returnIssue.p";
        private const string PSourcePath = @"RegressionTests\Integration\Correct\Elevator\Elevator.p";

        [Test]
        public void TestBasic()
        {
            string pFilePath = Path.Combine(Constants.TestDirectory, PSourcePath);
            string outputDir = Path.Combine(Constants.SolutionDirectory, "tmp");
            Directory.CreateDirectory(outputDir);

            var fileStream = new AntlrFileStream(pFilePath);
            var lexer = new PLexer(fileStream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new PParser(tokens);

            PParser.ProgramContext tree = parser.program();
            Analyzer.Analyze(parser, tree);
        }
    }
}