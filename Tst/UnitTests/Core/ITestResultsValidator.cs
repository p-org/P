namespace UnitTests.Core
{
    public interface ITestResultsValidator
    {
        bool ValidateResult(string stdout, string stderr, int? exitCode);
        bool ValidateException(TestRunException testRunException);
    }
}