using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace UnitTests.Core
{
    /// <summary>
    ///     Load test cases from disk for NUnit
    /// </summary>
    public static class TestCaseLoader
    {
        private static readonly List<string> TestDirs = new List<string>
        {
            Path.Combine("RegressionTests", "Combined"),
            Path.Combine("RegressionTests", "Feature1SMLevelDecls"),
            Path.Combine("RegressionTests", "Feature2Stmts"),
            Path.Combine("RegressionTests", "Feature3Exprs"),
            Path.Combine("RegressionTests", "Feature4DataTypes"),
            //Path.Combine("RegressionTests","Feature5ModuleSystem"),
            Path.Combine("RegressionTests", "Integration")
        };

        public static IEnumerable<TestCaseData> FindTestCasesInDirectory(string directoryName, string[] testDirNames)
        {
            return from testDir in TestDirs
                let baseDirectory = new DirectoryInfo(Path.Combine(directoryName, testDir))
                from testCaseDir in baseDirectory.EnumerateDirectories("*.*", SearchOption.AllDirectories)
                where testDirNames.Contains(testCaseDir.Parent.Name)
                select DirectoryToTestCase(testCaseDir, baseDirectory);
        }

        private static TestCaseData DirectoryToTestCase(DirectoryInfo dir, DirectoryInfo testRoot)
        {
            var category = testRoot.Name + Constants.CategorySeparator + GetCategory(dir, testRoot);
            var testName = category + Constants.CategorySeparator + dir.Name;
            return new TestCaseData(dir).SetName(testName).SetCategory(category);
        }

        private static string GetCategory(DirectoryInfo dir, DirectoryInfo baseDirectory)
        {
            var category = "";
            var sep = "";
            dir = dir.Parent;
            while (dir != null && dir.FullName != baseDirectory.FullName)
            {
                //category = $"{category}{sep}{dir.Name}";
                category = $"{dir.Name}{sep}{category}";
                dir = dir.Parent;
                sep = Constants.CategorySeparator;
            }

            return category;
        }
    }
}