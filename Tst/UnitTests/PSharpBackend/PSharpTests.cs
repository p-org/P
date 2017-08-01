using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Pc;
using NUnit.Framework;

namespace UnitTests.PSharpBackend
{
    [TestFixture]
    public class PSharpTests
    {
        //private const string PSourcePath = @"RegressionTests\Integration\Correct\Multi_Paxos_4\Multi_Paxos_4.p";
        private const string PSourcePath = @"..\tmp\tupOrder.p";

        [Test]
        public void TestBasic()
        {
            string pFilePath = Path.Combine(Constants.TestDirectory, PSourcePath);
            string outputDir = Path.Combine(Constants.SolutionDirectory, "tmp");
            Directory.CreateDirectory(outputDir);
            var compiler = new Compiler(true);
            Assert.IsTrue(
                compiler.Compile(
                    new StandardOutput(),
                    new CommandLineOptions
                    {
                        inputFileNames = new List<string> {pFilePath},
                        shortFileNames = true,
                        outputDir = outputDir,
                        unitName = Path.ChangeExtension(pFilePath, ".4ml"),
                        liveness = LivenessOption.None,
                        compilerOutput = CompilerOutput.PSharp
                    }),
                "Compile failed");
        }
    }
}