using System;
using NUnit.Framework;
using Plang.Compiler.TypeChecker;

namespace UnitTests
{
    [TestFixture]
    public class TypeCheckingUtilsTest
    {
        [Test]
        public void TestCorrectPrintArguments()
        {
            // Correct, easy cases
            Assert.AreEqual(0, TypeCheckingUtils.PrintStmtNumArgs("This is a test."));
            Assert.AreEqual(1, TypeCheckingUtils.PrintStmtNumArgs("{0}"));
            Assert.AreEqual(3, TypeCheckingUtils.PrintStmtNumArgs("{0} {2} {1}"));
            Assert.AreEqual(3, TypeCheckingUtils.PrintStmtNumArgs("{0}{1}{2}"));

            // Correct, weird cases
            Assert.AreEqual(3, TypeCheckingUtils.PrintStmtNumArgs("{0} {2}"));
            Assert.AreEqual(1, TypeCheckingUtils.PrintStmtNumArgs("foo{{bar{{{0}}}baz}}.."));
            Assert.AreEqual(0, TypeCheckingUtils.PrintStmtNumArgs("{{0}}"));
            Assert.AreEqual(0, TypeCheckingUtils.PrintStmtNumArgs("{{"));
            Assert.AreEqual(0, TypeCheckingUtils.PrintStmtNumArgs("}}"));
            Assert.AreEqual(0, TypeCheckingUtils.PrintStmtNumArgs(""));
            Assert.AreEqual(124, TypeCheckingUtils.PrintStmtNumArgs("{123}"));

            // Incorrect cases
            Assert.AreEqual(-1, TypeCheckingUtils.PrintStmtNumArgs("{"));
            Assert.AreEqual(-1, TypeCheckingUtils.PrintStmtNumArgs("}"));
            Assert.AreEqual(-1, TypeCheckingUtils.PrintStmtNumArgs("{{{"));
            Assert.AreEqual(-1, TypeCheckingUtils.PrintStmtNumArgs("}}}"));
            Assert.AreEqual(-1, TypeCheckingUtils.PrintStmtNumArgs("{0"));
            Assert.AreEqual(-1, TypeCheckingUtils.PrintStmtNumArgs("0}"));
            Assert.AreEqual(-1, TypeCheckingUtils.PrintStmtNumArgs("{ 0}"));
            Assert.AreEqual(-1, TypeCheckingUtils.PrintStmtNumArgs("{0 }"));
        }

    }
}