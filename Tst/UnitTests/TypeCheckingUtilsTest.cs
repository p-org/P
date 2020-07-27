using NUnit.Framework;
using Plang.Compiler.Backend.Prt;
using Plang.Compiler.TypeChecker;
using System;

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

        [Test]
        public void TestCorrectPrintParsing()
        {
            // Correct, easy cases
            Assert.AreEqual(new object[] { "This is a test." }, PrtTranslationUtils.ParsePrintMessage("This is a test."));
            Assert.AreEqual(new object[] { "", 0, "" }, PrtTranslationUtils.ParsePrintMessage("{0}"));
            Assert.AreEqual(new object[] { "", 0, " ", 2, " ", 1, "" },
                PrtTranslationUtils.ParsePrintMessage("{0} {2} {1}"));
            Assert.AreEqual(new object[] { "", 0, "", 1, "", 2, "" }, PrtTranslationUtils.ParsePrintMessage("{0}{1}{2}"));

            // Correct, weird cases
            Assert.AreEqual(new object[] { "", 0, " ", 2, "" }, PrtTranslationUtils.ParsePrintMessage("{0} {2}"));
            Assert.AreEqual(new object[] { "foo{bar{", 0, "}baz}.." },
                PrtTranslationUtils.ParsePrintMessage("foo{{bar{{{0}}}baz}}.."));
            Assert.AreEqual(new object[] { "{0}" }, PrtTranslationUtils.ParsePrintMessage("{{0}}"));
            Assert.AreEqual(new object[] { "{" }, PrtTranslationUtils.ParsePrintMessage("{{"));
            Assert.AreEqual(new object[] { "}" }, PrtTranslationUtils.ParsePrintMessage("}}"));
            Assert.AreEqual(new object[] { "" }, PrtTranslationUtils.ParsePrintMessage(""));
            Assert.AreEqual(new object[] { "", 123, "" }, PrtTranslationUtils.ParsePrintMessage("{123}"));

            // Incorrect cases
            Assert.Throws<ArgumentException>(() => PrtTranslationUtils.ParsePrintMessage("{"));
            Assert.Throws<ArgumentException>(() => PrtTranslationUtils.ParsePrintMessage("}"));
            Assert.Throws<ArgumentException>(() => PrtTranslationUtils.ParsePrintMessage("{{{"));
            Assert.Throws<ArgumentException>(() => PrtTranslationUtils.ParsePrintMessage("}}}"));
            Assert.Throws<ArgumentException>(() => PrtTranslationUtils.ParsePrintMessage("{0"));
            Assert.Throws<ArgumentException>(() => PrtTranslationUtils.ParsePrintMessage("0}"));
            Assert.Throws<ArgumentException>(() => PrtTranslationUtils.ParsePrintMessage("{ 0}"));
            Assert.Throws<ArgumentException>(() => PrtTranslationUtils.ParsePrintMessage("{0 }"));
        }
    }
}