using System;
using System.IO;
using NUnit.Framework;
using Plang.Options;
using UnitTests.Core;

namespace UnitTests
{
    /// <summary>
    /// Parsing of `p check` options into the checker configuration.
    /// </summary>
    [TestFixture]
    public class CheckerOptionsTest
    {
        private static PChecker.CheckerConfiguration Parse(params string[] options)
        {
            var outputDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory,
                nameof(CheckerOptionsTest), Guid.NewGuid().ToString("N")));
            var args = new string[options.Length + 3];
            args[0] = Path.Combine(outputDir.FullName, "Program.dll");
            args[1] = "-o";
            args[2] = outputDir.FullName;
            options.CopyTo(args, 3);
            return new PCheckerOptions().Parse(args);
        }

        [Test]
        public void ActivityCoverageIsCollectedByDefault()
        {
            Assert.That(Parse().ReportActivityCoverage, Is.True);
        }

        [Test]
        public void NoActivityCoverageDisablesActivityCoverage()
        {
            Assert.That(Parse("--no-activity-coverage").ReportActivityCoverage, Is.False);
        }
    }
}
