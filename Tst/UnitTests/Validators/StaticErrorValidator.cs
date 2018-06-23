namespace UnitTests.Validators
{
    public class StaticErrorValidator : ITestResultsValidator
    {
        public bool ValidateResult(string stdout, string stderr, int? exitCode)
        {
            return exitCode == null;
        }

        public bool ValidateException(TestRunException testRunException)
        {
            return testRunException.Reason == TestCaseError.TranslationFailed;
        }
    }
}