using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker;

public abstract class ParamAssignment
{
    private static IEnumerable<IEnumerable<T>> DifferentCombinationsAux<T>(IEnumerable<T> elements, int k)
    {
        var enumerable = elements as T[] ?? elements.ToArray();
        return k == 0
            ? [Array.Empty<T>()]
            : enumerable.SelectMany((e, i) =>
                DifferentCombinationsAux(enumerable.Skip(i + 1), k - 1).Select(c => new[] { e }.Concat(c)));
    }

    public static HashSet<HashSet<T>> DifferentCombinations<T>(IEnumerable<T> elements, int k)
    {
        var res = DifferentCombinationsAux(elements, k);
        return new HashSet<HashSet<T>>(res.Select(innerEnumerable => new HashSet<T>(innerEnumerable)),
            HashSet<T>.CreateSetComparer());
    }

    // Should use sum type (Option) instead of product type
    public static (bool, string) TwiseNumWellFormednessCheck(int twise, int numParams)
    {
        return twise < 1
            ? (false, $"twise number {twise} is less than 1.")
            : (twise > numParams ? (false, $"twise number {twise} is greater than {numParams}.") : (true, ""));
    }

    private static Dictionary<int, T> EnumerableToIndexDict<T>(IEnumerable<T> l, Func<T, T> f)
    {
        return Enumerable.Range(0, l.Count()).Zip(l, (a, b) => new { a, b }).ToDictionary(x => x.a, x => f(x.b));
    }

    private static Dictionary<int, Dictionary<string, int>> MakeVectorMap(Dictionary<string, int> universe, int twise)
    {
        var vectorSet = new HashSet<Dictionary<string, int>>();
        foreach (var param in DifferentCombinations(universe.Select(kv => kv.Key), twise))
        {
            var result = new List<Dictionary<string, int>> { new() };
            foreach (var name in param)
            {
                var newResult = new List<Dictionary<string, int>>();
                for (var i = 0; i < universe[name]; i++)
                {
                    var resultCopy = result.Select(dict => dict.ToDictionary(kv => kv.Key, kv => kv.Value)).ToList();
                    foreach (var vector in resultCopy) vector.Add(name, i);
                    newResult.AddRange(resultCopy);
                }
                result = newResult;
            }
            vectorSet.UnionWith(result);
        }
        return EnumerableToIndexDict(vectorSet, x => x);
    }

    private static Dictionary<int, HashSet<int>> MakeAssignmentCoverageMap(
        Dictionary<int, Dictionary<string, int>> assignmentMap, Dictionary<int, Dictionary<string, int>> vectorMap,
        List<int> assignments)
    {
        var assignmentCoverageMap = new Dictionary<int, HashSet<int>>();
        foreach (var assignment in assignments)
        {
            assignmentCoverageMap.Add(assignment, []);
            foreach (var kv in vectorMap.Where(kv => kv.Value.All(assignmentMap[assignment].Contains)))
                assignmentCoverageMap[assignment].Add(kv.Key);
        }
        return assignmentCoverageMap;
    }

    private static List<int> GreedyCoverageExplore(Dictionary<string, int> universe,
        List<Dictionary<string, int>> assignmentList, int twise)
    {
        var assignments = Enumerable.Range(0, assignmentList.Count).ToList();
        if (twise == universe.Count) return assignments;
        var vectorMap = MakeVectorMap(universe, twise);
        var assignmentMap = EnumerableToIndexDict(assignmentList, x => x);
        var assignmentCoverageMap = MakeAssignmentCoverageMap(assignmentMap, vectorMap, assignments);
        var obligationSet = vectorMap.Keys.ToHashSet();
        obligationSet.IntersectWith(assignmentCoverageMap.SelectMany(kv => kv.Value));
        foreach (var kv in assignmentCoverageMap) assignmentCoverageMap[kv.Key].IntersectWith(obligationSet);
        var result = new List<int>();
        while (obligationSet.Count != 0)
        {
            var (ass, coverage) = assignmentCoverageMap.MaxBy(kv => kv.Value.Count);
            obligationSet.ExceptWith(coverage);
            assignmentCoverageMap.Remove(ass);
            foreach (var kv in assignmentCoverageMap) assignmentCoverageMap[kv.Key].ExceptWith(coverage);
            result.Add(ass);
        }
        return result;
    }

    private static Dictionary<string, IPExpr> Dic2StrDic(Dictionary<Variable, IPExpr> dic)
    {
        var dicAux = new Dictionary<string, IPExpr>();
        foreach (var (k, i) in dic) dicAux[k.Name] = i;
        return dicAux;
    }

    private static Dictionary<Variable, IPExpr> IndexDic2Dic(List<Variable> globalParams,
        IDictionary<string, List<IPExpr>> paramExprDic, IDictionary<string, int> indexDic)
    {
        var dic = new Dictionary<Variable, IPExpr>();
        foreach (var (k, i) in indexDic)
        {
            var values = paramExprDic[k];
            if (!globalParams.Any(v => v.Name == k))
            {
                throw new Exception($"Variable name '{k}' not found in globalParams. " +
                                    $"GlobalParams are: [{string.Join(", ", globalParams.Select(v => v.Name))}]");
            }
            var variable = globalParams.First(v => v.Name == k);
            if (i >= values.Count) throw new ArgumentException("Index out of range in global variable config.");
            dic[variable] = values[i];
        }
        return dic;
    }

    public static string RenameSafetyTestByAssignment(string name, Dictionary<Variable, IPExpr> dic)
    {
        var postfix = $"{string.Join("__", Dic2StrDic(dic).ToList().Select(p => $"{p.Key}_{p.Value}"))}";
        return postfix.Length == 0 ? name : $"{name}___{postfix}";
    }

    private static bool Next((string, int)[] indexArr, IDictionary<string, List<IPExpr>> globalParams)
    {
        for (var i = 0; i < indexArr.Length; i++)
        {
            indexArr[i] = (indexArr[i].Item1, indexArr[i].Item2 + 1);
            if (indexArr[i].Item2 < globalParams[indexArr[i].Item1].Count) return true;
            indexArr[i] = (indexArr[i].Item1, 0);
        }
        return false;
    }

    public static void IterateAssignments(SafetyTest safety, List<Variable> globalParams,
        Action<Dictionary<Variable, IPExpr>> generateTestCode)
    {
        var indexArr = safety.ParamExprMap.ToList()
            .Zip(Enumerable.Repeat(0, safety.ParamExprMap.Count), (x, y) => (x.Key, y)).ToArray();
        var universe = safety.ParamExprMap.ToDictionary(kv => kv.Key, kv => kv.Value.Count);
        var assignmentIndices = new List<Dictionary<string, int>>();
        do
        {
            var indexDic = indexArr.ToDictionary(item => item.Item1, item => item.Item2);
            var dic = IndexDic2Dic(globalParams, safety.ParamExprMap, indexDic);
            if (!SimpleExprEval.ForceBool(SimpleExprEval.Eval(dic, safety.AssumeExpr))) continue;
            assignmentIndices.Add(indexDic);
        } while (Next(indexArr, safety.ParamExprMap));
        foreach (var i in GreedyCoverageExplore(universe, assignmentIndices, safety.Twise))
            generateTestCode(IndexDic2Dic(globalParams, safety.ParamExprMap, assignmentIndices[i]));
    }
}