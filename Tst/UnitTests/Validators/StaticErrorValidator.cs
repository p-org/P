using UnitTests.Core;

namespace UnitTests.Validators
{
    /// <inheritdoc />
    /// <summary>
    ///     Validates that the P compiler fails with the appropriate error for a type-checker test case.
    /// </summary>
    public class StaticErrorValidator : ITestResultsValidator
    {
        public bool ValidateResult(string stdout, string stderr, int? exitCode)
        {
            // TODO: use golden output to check stderr/stdout.
            return exitCode == null;
        }

        public bool ValidateException(CompilerTestException compilerTestException)
        {
            return compilerTestException.Reason == TestCaseError.TranslationFailed;
        }
    }
}