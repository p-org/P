using UnitTests.Core;

namespace UnitTests.Validators
{
    /// <inheritdoc />
    /// <summary>
    ///     Validates that the generated program ran with the expected output.
    /// </summary>
    public class ExecutionOutputValidator : ITestResultsValidator
    {
        private readonly int expectedExitCode;
        private readonly string expectedStderr;
        private readonly string expectedStdout;

        public ExecutionOutputValidator(int expectedExitCode, string expectedStdout, string expectedStderr)
        {
            this.expectedExitCode = expectedExitCode;
            this.expectedStdout = expectedStdout;
            this.expectedStderr = expectedStderr;
        }

        public bool ValidateResult(string stdout, string stderr, int? exitCode)
        {
            if (expectedStdout != null && !expectedStdout.Equals(stdout)) return false;

            if (expectedStderr != null && !expectedStderr.Equals(stderr)) return false;

            return exitCode == expectedExitCode;
        }

        public bool ValidateException(CompilerTestException compilerTestException)
        {
            return false;
        }
    }
}