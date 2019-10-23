using UnitTests.Core;

namespace UnitTests.Validators
{
    /// <inheritdoc />
    /// <summary>
    ///     Validates that the compiler ran without issue.
    /// </summary>
    public class CompileSuccessValidator : ITestResultsValidator
    {
        public bool ValidateResult(int? exitCode)
        {
            return exitCode == 0;
        }

        public bool ValidateException(CompilerTestException compilerTestException)
        {
            return false;
        }
    }
}