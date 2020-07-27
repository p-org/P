namespace UnitTests.Core
{
    /// <summary>
    ///     Interface for validating test cases
    /// </summary>
    public interface ITestResultsValidator
    {
        /// <summary>
        ///     Check if the given result is valid.
        /// </summary>
        /// <param name="exitCode">The exit code produced by the test</param>
        /// <returns>True if the result is valid, otherwise false</returns>
        bool ValidateResult(int? exitCode);

        /// <summary>
        ///     Check if the given exception was expected
        /// </summary>
        /// <param name="compilerTestException">The exception raised during the test run</param>
        /// <returns>True if the exception is expected, otherwise false.</returns>
        bool ValidateException(CompilerTestException compilerTestException);
    }
}