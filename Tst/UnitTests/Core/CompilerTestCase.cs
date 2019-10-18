using System.IO;

namespace UnitTests.Core
{
    /// <summary>
    ///     Class representing a single compiler test. Coordinates running a test and validating the results.
    ///     Has an associated scratch directory to be passed to the runner and cleaned up later.
    /// </summary>
    public class CompilerTestCase
    {
        private readonly ICompilerTestRunner runner;
        private readonly ITestResultsValidator validator;

        /// <summary>
        ///     Box a new test case with the given scratch directory, test runner, and validator
        /// </summary>
        /// <param name="scratchDirectory">The scratch directory to use. Caller is responsible for cleaning up.</param>
        /// <param name="runner">The test runner to use</param>
        /// <param name="validator">The results validator to use</param>
        public CompilerTestCase(DirectoryInfo scratchDirectory, ICompilerTestRunner runner,
            ITestResultsValidator validator)
        {
            ScratchDirectory = scratchDirectory;
            this.runner = runner;
            this.validator = validator;
        }

        public DirectoryInfo ScratchDirectory { get; }

        /// <summary>
        ///     Run the test and determine whether or not it passed
        /// </summary>
        /// <param name="stdout">The standard output produced by the test case</param>
        /// <param name="stderr">The standard error output produced by the test case</param>
        /// <param name="exitCode">The exit code produced by the compiled program, or null if it failed to compile</param>
        /// <returns>True if the test passed, otherwise false</returns>
        public bool EvaluateTest(out string stdout, out string stderr, out int? exitCode)
        {
            stdout = "";
            stderr = "";
            exitCode = null;

            try
            {
                exitCode = runner.RunTest(ScratchDirectory, out stdout, out stderr);
                return validator.ValidateResult(exitCode);
            }
            catch (CompilerTestException e)
            {
                return validator.ValidateException(e);
            }
        }
    }
}