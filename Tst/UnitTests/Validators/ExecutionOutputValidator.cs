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

        public ExecutionOutputValidator(int expectedExitCode)
        {
            this.expectedExitCode = expectedExitCode;
        }

        public bool ValidateResult(int? exitCode)
        {
            return exitCode == expectedExitCode;
        }

        public bool ValidateException(CompilerTestException compilerTestException)
        {
            return false;
        }
    }
}