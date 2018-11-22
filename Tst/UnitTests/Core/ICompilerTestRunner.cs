using System.IO;

namespace UnitTests.Core
{
    /// <summary>
    ///     Interface for running test cases.
    /// </summary>
    public interface ICompilerTestRunner
    {
        /// <summary>
        ///     Run the test in the given scratch directory, recording output.
        /// </summary>
        /// <param name="scratchDirectory">The scratch directory to use. Caller is responsible for cleaning up.</param>
        /// <param name="stdout">The output produced by the test.</param>
        /// <param name="stderr">The error output produced by the test.</param>
        /// <returns></returns>
        int? RunTest(DirectoryInfo scratchDirectory, out string stdout, out string stderr);
    }
}