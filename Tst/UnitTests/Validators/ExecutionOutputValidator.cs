using System;
using UnitTests.Core;

namespace UnitTests.Validators
{
    public class ExecutionOutputValidator : ITestResultsValidator
    {
        private readonly string expectedStderr;
        private readonly string expectedStdout;
        private readonly int expectedExitCode;

        public ExecutionOutputValidator(int expectedExitCode, string expectedStdout, string expectedStderr)
        {
            this.expectedExitCode = expectedExitCode;
            this.expectedStdout = expectedStdout;
            this.expectedStderr = expectedStderr;
        }

        public bool ValidateResult(string stdout, string stderr, int? exitCode)
        {
            if (expectedStdout != null && !expectedStdout.Equals(stdout))
            {
                return false;
            }
            if (expectedStderr != null && !expectedStderr.Equals(stderr))
            {
                return false;
            }
            return exitCode == expectedExitCode;
        }

        public bool ValidateException(TestRunException testRunException)
        {
            return false;
        }
    }
}