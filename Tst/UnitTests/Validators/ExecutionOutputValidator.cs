using System;
using UnitTests.Core;

namespace UnitTests.Validators
{
    public class ExecutionOutputValidator : ITestResultsValidator
    {
        private readonly string expectedStderr;
        private readonly string expectedStdout;
        private readonly Func<int?, bool> isGoodExitCode;

        public ExecutionOutputValidator(Func<int?, bool> isGoodExitCode, string expectedStdout, string expectedStderr)
        {
            this.isGoodExitCode = isGoodExitCode;
            this.expectedStdout = expectedStdout;
            this.expectedStderr = expectedStderr;
        }

        public bool ValidateResult(string stdout, string stderr, int? exitCode)
        {
            return isGoodExitCode(exitCode);
        }

        public bool ValidateException(TestRunException testRunException)
        {
            return false;
        }
    }
}