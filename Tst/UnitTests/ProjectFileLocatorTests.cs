using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Plang.Options;
using Plang.Parser;

namespace UnitTests
{
    /// <summary>
    /// Tests for <see cref="ProjectFileLocator"/>'s project-file resolution rules.
    /// These exercise <c>TryFindLocalPProject</c> directly (rather than the
    /// public <c>FindLocalPProject</c>) so the "multiple files" path can be
    /// asserted without the process-exiting error reporter.
    /// </summary>
    [TestFixture]
    public class ProjectFileLocatorTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "pproj-locator-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (_tempDir != null && Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private string CreatePProj(string name)
        {
            var path = Path.Combine(_tempDir, name);
            File.WriteAllText(path, "<Project></Project>");
            return path;
        }

        [Test]
        public void DoesNotSearchWhenPProjAlreadySpecified()
        {
            CreatePProj("Found.pproj");
            var result = new List<CommandLineArgument>
            {
                new CommandLineArgument { LongName = "pproj", Value = "Explicit.pproj" },
            };

            var ok = ProjectFileLocator.TryFindLocalPProject(result, _tempDir, out var error);

            Assert.IsTrue(ok);
            Assert.IsNull(error);
            // Untouched: still only the explicitly-provided argument.
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Explicit.pproj", result[0].Value);
        }

        [Test]
        public void DoesNotSearchWhenPFilesAlreadySpecified()
        {
            CreatePProj("Found.pproj");
            var result = new List<CommandLineArgument>
            {
                new CommandLineArgument { LongName = "pfiles", Value = "a.p" },
            };

            var ok = ProjectFileLocator.TryFindLocalPProject(result, _tempDir, out var error);

            Assert.IsTrue(ok);
            Assert.IsNull(error);
            Assert.AreEqual(1, result.Count);
            Assert.IsFalse(result.Any(a => a.LongName == "pproj"));
        }

        [Test]
        public void AddsNothingWhenNoPProjFound()
        {
            var result = new List<CommandLineArgument>();

            var ok = ProjectFileLocator.TryFindLocalPProject(result, _tempDir, out var error);

            Assert.IsTrue(ok);
            Assert.IsNull(error);
            Assert.IsEmpty(result);
        }

        [Test]
        public void AddsPProjArgumentWhenSinglePProjFound()
        {
            var expected = CreatePProj("OnlyOne.pproj");
            var result = new List<CommandLineArgument>();

            var ok = ProjectFileLocator.TryFindLocalPProject(result, _tempDir, out var error);

            Assert.IsTrue(ok);
            Assert.IsNull(error);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("pproj", result[0].LongName);
            Assert.AreEqual("pp", result[0].ShortName);
            Assert.AreEqual(expected, result[0].Value);
        }

        [Test]
        public void ReturnsErrorWhenMultiplePProjFound()
        {
            CreatePProj("First.pproj");
            CreatePProj("Second.pproj");
            var result = new List<CommandLineArgument>();

            var ok = ProjectFileLocator.TryFindLocalPProject(result, _tempDir, out var error);

            Assert.IsFalse(ok);
            Assert.IsNotNull(error);
            StringAssert.Contains("First.pproj", error);
            StringAssert.Contains("Second.pproj", error);
            StringAssert.Contains("--pproj", error);
            // Ambiguous: nothing is auto-selected.
            Assert.IsEmpty(result);
        }
    }
}
