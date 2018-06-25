using System.IO;
using Microsoft.Pc;
using NUnit.Framework;
using UnitTests.Core;
using UnitTests.Runners;
using UnitTests.Validators;

namespace UnitTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    internal class PSharpBackendTests
    {
        [Test]
        public void TestCompilePSharpTemp()
        {
            DirectoryInfo tempDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, "TestTemp"));
            var tempFilePath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "tmp", "test.p"));

            if (!tempFilePath.Exists)
            {
                return;
            }

            var testCase = new CompilerTestCase(tempDir,
                                                new CompileOnlyRunner(CompilerOutput.PSharp, new[] {tempFilePath}),
                                                new CompileSuccessValidator());

            TestAssertions.AssertTestCase(testCase);
        }
    }
}
