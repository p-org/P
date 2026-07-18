using System.Collections.Generic;
using System.Reflection;
using Antlr4.Runtime;
using NUnit.Framework;
using Plang.Compiler.Backend.PVerifier;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace UnitTests
{
    [TestFixture]
    public class PVerifierChooseExprTests
    {
        [Test]
        public void ExprToStringSupportsChooseInsideNamedTuple()
        {
            var generator = new PVerifierCodeGenerator();
            var chooseSet = new HashSet<PLanguageType>();

            typeof(PVerifierCodeGenerator)
                .GetField("_chooseToDeclare", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(generator, chooseSet);

            var tupleType = new NamedTupleType(new[]
            {
                new NamedTupleEntry { Name = "a", FieldNo = 0, Type = PrimitiveType.Int },
                new NamedTupleEntry { Name = "b", FieldNo = 1, Type = PrimitiveType.Int }
            });
            var chooseExpr = new ChooseExpr(
                ParserRuleContext.EmptyContext,
                new IntLiteralExpr(ParserRuleContext.EmptyContext, 10),
                PrimitiveType.Int);
            var tupleWithNestedChoose = new NamedTupleExpr(
                ParserRuleContext.EmptyContext,
                new IPExpr[] { chooseExpr, chooseExpr },
                tupleType);

            var exprToString = typeof(PVerifierCodeGenerator)
                .GetMethod("ExprToString", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var rendered = (string)exprToString.Invoke(generator, new object[] { tupleWithNestedChoose })!;

            StringAssert.Contains("PChoose_integer(10)", rendered);
            Assert.That(chooseSet, Has.Count.EqualTo(1));
        }
    }
}
