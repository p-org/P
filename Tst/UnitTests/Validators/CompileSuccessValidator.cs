namespace UnitTests.Validators
{
    public class CompileSuccessValidator : ITestResultsValidator
    {
        public bool ValidateResult(string stdout, string stderr, int? exitCode)
        {
            return exitCode == 0;
        }

        public bool ValidateException(TestRunException testRunException)
        {
            return false;
        }
    }
}