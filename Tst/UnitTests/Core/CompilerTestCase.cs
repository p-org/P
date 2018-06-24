using System.IO;

namespace UnitTests.Core
{
    public class CompilerTestCase
    {
        public DirectoryInfo ScratchDirectory { get; }

        private readonly ICompilerTestRunner runner;
        private readonly ITestResultsValidator validator;

        public CompilerTestCase(DirectoryInfo scratchDirectory, ICompilerTestRunner runner, ITestResultsValidator validator)
        {
            ScratchDirectory = scratchDirectory;
            this.runner = runner;
            this.validator = validator;
        }

        public bool EvaluateTest(out string stdout, out string stderr, out int? exitCode)
        {
            stdout = "";
            stderr = "";
            exitCode = null;

            try
            {
                exitCode = runner.RunTest(ScratchDirectory, out stdout, out stderr);
                return validator.ValidateResult(stdout, stderr, exitCode);
            }
            catch (TestRunException e)
            {
                return validator.ValidateException(e);
            }
        }
    }
}