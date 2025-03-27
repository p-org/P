using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker;

public abstract class ParamAssignment
{
    
    public static IEnumerable<IEnumerable<T>> DifferentCombinations<T>(IEnumerable<T> elements, int k)
    {
        var enumerable = elements as T[] ?? elements.ToArray();
        return k == 0 ? [Array.Empty<T>()]
            :
            enumerable.SelectMany((e, i) =>
                DifferentCombinations(enumerable.Skip(i + 1), k - 1).Select(c => (new[] {e}).Concat(c)));
    }
    
    private static Dictionary<int, T> EnumerableToIndexDict<T> (IEnumerable<T> l, Func<T, T> f)
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
                    foreach (var vector in resultCopy)
                    {
                        vector.Add(name, i);
                    }
                    newResult.AddRange(resultCopy);
                }
                result = newResult;
            }
            vectorSet.UnionWith(result);
        }
        return EnumerableToIndexDict(vectorSet, x => x);
    }

    private static Dictionary<int, HashSet<int>> MakeAssignmentCoverageMap(
        Dictionary<int, Dictionary<string, int>> assignmentMap,
        Dictionary<int, Dictionary<string, int>> vectorMap,
        List<int> assignments)
    {
        var assignmentCoverageMap = new Dictionary<int, HashSet<int>>();
        foreach (var assignment in assignments)
        {
            assignmentCoverageMap.Add(assignment, []);
            foreach (var kv in vectorMap.Where(kv => kv.Value.All(assignmentMap[assignment].Contains)))
            {
                assignmentCoverageMap[assignment].Add(kv.Key);
            }
        }
        return assignmentCoverageMap;
    }

    private static List<int> GreedyCoverageExplore(Dictionary<string, int> universe, List<Dictionary<string, int>> assignmentList, int twise)
    {
        var assignments = Enumerable.Range(0, assignmentList.Count).ToList();
        if (twise == universe.Count) return assignments;
        var vectorMap = MakeVectorMap(universe, twise);
        var assignmentMap = EnumerableToIndexDict(assignmentList, x => x);
        // Console.WriteLine($"twise({twise})");
        // Console.WriteLine($"assignments({assignments.Count})");
        // Console.WriteLine($"vectorMap({vectorMap.Count})");
        // foreach (var (i, indexDic) in vectorMap)
        // {
        //     Console.WriteLine($"vectorMap: {i}: {string.Join(',', indexDic)}");
        // }
        var assignmentCoverageMap = MakeAssignmentCoverageMap(assignmentMap, vectorMap, assignments);
        // foreach (var (indexDic, c) in assignmentCoverageMap)
        // {
        //     Console.WriteLine($"Coverage: {string.Join(',', indexDic)} :: {string.Join(',', c)}");
        // }
        var obligationSet = vectorMap.Keys.ToHashSet();
        // Console.WriteLine($"init obligationSet: {string.Join(',', obligationSet)}");
        obligationSet.IntersectWith(assignmentCoverageMap.SelectMany(kv => kv.Value));
        // Console.WriteLine($"obligationSet: {string.Join(',', obligationSet)}");
        // foreach (var i in obligationSet)
        // {
        //     Console.WriteLine($"Missing Coverage: {string.Join(',', vectorMap[i])}");
        // }
        foreach (var kv in assignmentCoverageMap)
        {
            assignmentCoverageMap[kv.Key].IntersectWith(obligationSet);
        }
        var result = new List<int>();
        while (obligationSet.Count != 0)
        {
            var (ass, coverage)= assignmentCoverageMap.MaxBy(kv => kv.Value.Count);
            // Console.WriteLine($"Missing Coverage: {string.Join(',', obligationSet)}");
            // Console.WriteLine($"Max one({string.Join(',', ass)}) covers({coverage.Count}) {string.Join(',', coverage)}");
            obligationSet.ExceptWith(coverage);
            assignmentCoverageMap.Remove(ass);
            foreach (var kv in assignmentCoverageMap)
            {
                assignmentCoverageMap[kv.Key].ExceptWith(coverage);
            }
            result.Add(ass);
        }
        // Console.WriteLine($"result({result.Count})");
        return result;
    }
    
    private static Dictionary<string, IPExpr> Dic2StrDic(Dictionary<Variable, IPExpr> dic)
    {
        var dicAux = new Dictionary<string, IPExpr>();
        foreach (var (k, i) in dic)
        {
            dicAux[k.Name] = i;
        }
        return dicAux;
    }

    private static Dictionary<Variable, IPExpr> IndexDic2Dic(List<Variable> globalParams, IDictionary<string, List<IPExpr>> paramExprDic, IDictionary<string, int> indexDic)
    {
        var dic = new Dictionary<Variable, IPExpr>();
        foreach (var (k, i) in indexDic)
        {
            var values = paramExprDic[k];
            var variable = globalParams.First(v => v.Name == k);
            if (i >= values.Count)
            {
                throw new ArgumentException("Index out of range in global variable config."); 
            }
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
            // Console.WriteLine($"globalParams[globalVariables[{i}].Name].Count = {globalParams[globalVariables[i].Name].Count}");
            if (indexArr[i].Item2 < globalParams[indexArr[i].Item1].Count) return true;
            indexArr[i] = (indexArr[i].Item1, 0);
        }
        return false;
    }

    public static void IterateAssignments(SafetyTest safety, List<Variable> globalParams, Action<Dictionary<Variable, IPExpr>> f)
    {
        // Console.WriteLine($"safety.ParamExpr.Count = {safety.ParamExpr.Count}");
        var indexArr = safety.ParamExprMap.ToList().Zip(Enumerable.Repeat(0, safety.ParamExprMap.Count), (x, y) => (x.Key, y)).ToArray();
        var universe = safety.ParamExprMap.ToDictionary(kv => kv.Key, kv => kv.Value.Count);
        var assignmentIndices = new List<Dictionary<string, int>>();
        do
        {
            var indexDic = indexArr.ToDictionary(item => item.Item1, item => item.Item2);
            var dic = IndexDic2Dic(globalParams, safety.ParamExprMap, indexDic);
            if (!SimpleExprEval.ForceBool(SimpleExprEval.Eval(dic, safety.AssumeExpr)))
            {
                // Console.WriteLine($"UnSat Assumption: indexArr: {string.Join(',', indexArr)}");
                continue;
            }
            assignmentIndices.Add(indexDic);
            // Console.WriteLine($"Sat Assumption: indexArr: {string.Join(',', indexArr)}");
        } while (Next(indexArr, safety.ParamExprMap));
        
        foreach (var i in GreedyCoverageExplore(universe, assignmentIndices, safety.Twise))
        {
            // Console.WriteLine($"Choose {safety.Twise}-wise: indexArr: {string.Join(',', assignmentIndices[i])}");
            f(IndexDic2Dic(globalParams, safety.ParamExprMap, assignmentIndices[i]));
        }
        // do
        // {
        //     var indexDic = indexArr.ToDictionary(item => item.Item1, item => item.Item2);
        //     var dic = IndexDic2Dic(globalParams, safety.ParamExprMap, indexDic);
        //     // Console.WriteLine($"{string.Join(',', dic.ToList())} |- {safety.AssumeExpr}");
        //     // Console.WriteLine($"{string.Join(',', dic.ToList())} |- {safety.AssumeExpr} = {ForceBool(Eval(dic, safety.AssumeExpr))}");
        //     if (!SimpleExprEval.ForceBool(SimpleExprEval.Eval(dic, safety.AssumeExpr))) continue;
        //     // Console.WriteLine($"indexArr: {string.Join(',', indexArr)}");
        //     f(dic);
        // } while (Next(indexArr, safety.ParamExprMap));
    }
}