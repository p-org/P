using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;

namespace UnitTests.TypeChecker;

[TestFixture]
[TestOf(typeof(ParamAssignment))]
public class ParamAssignmentTest
{
    [Test]
    public void TestDifferentCombinations()
    {
        var input = new List<int> { 1, 2, 3 };
        var expected = new HashSet<HashSet<int>> { new() { 1, 2 }, new() { 1, 3 }, new() { 2, 3 } };
        Assert.AreEqual(ParamAssignment.DifferentCombinations(input, 2), expected);
        expected = [new HashSet<int> { 1 }, new HashSet<int> { 2 }, new HashSet<int> { 3 }];
        Assert.AreEqual(ParamAssignment.DifferentCombinations(input, 1), expected);
        expected = [new HashSet<int> { 1, 2, 3 }];
        Assert.AreEqual(ParamAssignment.DifferentCombinations(input, 3), expected);
    }

    private static Dictionary<string, int> MakeSingleAssignment(int a, int b, int c)
    {
        return new Dictionary<string, int> { { "a", a }, { "b", b }, { "c", c } };
    }

    private static HashSet<Dictionary<string, int>> MakeExpectAssignments(int i)
    {
        return i switch
        {
            1 => // Total 3 obligations
            [
                MakeSingleAssignment(1, 1, 1), MakeSingleAssignment(2, 2, 2), MakeSingleAssignment(1, 3, 1)
            ],
            2 => // Total 2*3 + 2*2 + 3*2 = 16 obligations
            [
                MakeSingleAssignment(1, 1, 1), // (a = 1, b = 1) (a = 1, c = 1) (b = 1, c = 1)
                MakeSingleAssignment(2, 2, 1), // (a = 2, b = 2) (a = 2, c = 1) (b = 2, c = 1)
                MakeSingleAssignment(2, 1, 2), // (a = 2, b = 1) (a = 2, c = 2) (b = 1, c = 2)
                MakeSingleAssignment(1, 2, 2), // (a = 1, b = 2) (a = 1, c = 2) (b = 2, c = 2)
                MakeSingleAssignment(1, 3, 1), // (a = 1, b = 3) (b = 3, c = 1)
                MakeSingleAssignment(2, 3, 2)  // (a = 2, b = 3) (b = 3, c = 2)
            ],
            3 => // Total 2*3*2 = 12 obligations, need all combinations
            [
                MakeSingleAssignment(1, 1, 1), MakeSingleAssignment(2, 1, 1), MakeSingleAssignment(1, 2, 1),
                MakeSingleAssignment(2, 2, 1), MakeSingleAssignment(1, 3, 1), MakeSingleAssignment(2, 3, 1),
                MakeSingleAssignment(1, 1, 2), MakeSingleAssignment(2, 1, 2), MakeSingleAssignment(1, 2, 2),
                MakeSingleAssignment(2, 2, 2), MakeSingleAssignment(1, 3, 2), MakeSingleAssignment(2, 3, 2),
            ],
            _ => []
        };
    }

    [Test]
    public void TestIterateAssignments()
    {
        var safety = new SafetyTest(null, "test")
        {
            ParamExprMap = new Dictionary<string, List<IPExpr>>
            {
                { "a", [new IntLiteralExpr(1), new IntLiteralExpr(2)] },
                { "b", [new IntLiteralExpr(1), new IntLiteralExpr(2), new IntLiteralExpr(3)] },
                { "c", [new IntLiteralExpr(1), new IntLiteralExpr(2)] },
            },
            AssumeExpr = new BoolLiteralExpr(true),
            Twise = 0
        };
        var globalParams = safety.ParamExprMap.Select(kv => new Variable(kv.Key)).ToList();
        for (var i = 1; i <= 3; i++)
        {
            safety.Twise = i;
            var nativeRecord = new HashSet<Dictionary<Variable, IPExpr>>();
            ParamAssignment.IterateAssignments(safety, globalParams, dict => nativeRecord.Add(dict));
            var record = nativeRecord
                .Select(dict => dict.ToDictionary(kv => kv.Key.Name, kv => ((IntLiteralExpr)kv.Value).Value))
                .ToHashSet();
            Assert.AreEqual(record, MakeExpectAssignments(i));
        }
    }
}