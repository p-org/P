using System;
using System.Collections.Generic;
using System.IO;
using UnitTests.Runners;
using UnitTests.Validators;
using UnitTestsCore;

namespace UnitTests
{
    public class TestCaseFactory
    {
        private readonly DirectoryInfo testTempBaseDir;

        public TestCaseFactory(DirectoryInfo testTempBaseDir)
        {
            this.testTempBaseDir = testTempBaseDir;
        }

        public CompilerTestCase CreateTestCase(DirectoryInfo testDir, Dictionary<TestType, TestConfig> testConfigs)
        {
            // eg. RegressionTests/F1/Correct/TestCaseName
            var inputFiles = testDir.GetFiles("*.p");
            string testName = new Uri(Constants.TestDirectory + Path.DirectorySeparatorChar)
                              .MakeRelativeUri(new Uri(testDir.FullName))
                              .ToString();

            ICompilerTestRunner runner;
            ITestResultsValidator validator;

            if (!testConfigs.ContainsKey(TestType.Prt))
            {
                runner = new TranslationRunner(inputFiles);

                // TODO: validate information about the particular kind of compiler error
                bool isStaticError = testName.Contains("/StaticError/");
                validator = isStaticError ? (ITestResultsValidator) new StaticErrorValidator() : new CompileSuccessValidator();
            }
            else
            {
                runner = new ExecutionRunner(inputFiles);

                bool isCorrectTest = testName.Contains("/Correct/");
                validator = isCorrectTest
                    ? new ExecutionOutputValidator(exitCode => exitCode == 0, "", "")
                    : new ExecutionOutputValidator(exitCode => exitCode != 0, "", "");
            }
            DirectoryInfo tempDirName = Directory.CreateDirectory(Path.Combine(testTempBaseDir.FullName, testName));
            return new CompilerTestCase(tempDirName, runner, validator);
        }
    }
}