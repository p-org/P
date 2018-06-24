using System.IO;

namespace UnitTests.Core
{
    public interface ICompilerTestRunner
    {
        int? RunTest(DirectoryInfo scratchDirectory, out string stdout, out string stderr);
    }
}