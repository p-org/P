using System.Collections.Generic;
using NUnit.Framework;
using Plang.Compiler.TypeChecker;

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
}