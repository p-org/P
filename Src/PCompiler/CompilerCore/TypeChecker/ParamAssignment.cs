using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker;

public abstract class ParamAssignment
{
    public static Dictionary<string, IPExpr> Dic2StrDic(Dictionary<Variable, IPExpr> dic)
    {
        var dicAux = new Dictionary<string, IPExpr>();
        foreach (var (k, i) in dic)
        {
            dicAux[k.Name] = i;
        }
        return dicAux;
    }

    public static Dictionary<Variable, IPExpr> IndexDic2Dic(List<Variable> globalParams, IDictionary<string, List<IPExpr>> paramExprDic, IDictionary<string, int> indexDic)
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

    public static void IterateIndexDic(SafetyTest safety, List<Variable> globalParams, Action<Dictionary<string, int>> f)
    {
        // Console.WriteLine($"safety.ParamExpr.Count = {safety.ParamExpr.Count}");
        var indexArr = safety.ParamExprMap.ToList().Zip(Enumerable.Repeat(0, safety.ParamExprMap.Count), (x, y) => (x.Key, y)).ToArray();
        do
        {
            var indexDic = indexArr.ToDictionary(item => item.Item1, item => item.Item2);
            var dic = IndexDic2Dic(globalParams, safety.ParamExprMap, indexDic);
            // Console.WriteLine($"{string.Join(',', dic.ToList())} |- {safety.AssumeExpr}");
            // Console.WriteLine($"{string.Join(',', dic.ToList())} |- {safety.AssumeExpr} = {ForceBool(Eval(dic, safety.AssumeExpr))}");
            if (!SimpleExprEval.ForceBool(SimpleExprEval.Eval(dic, safety.AssumeExpr))) continue;
            // Console.WriteLine($"indexArr: {string.Join(',', indexArr)}");
            f(indexDic);
        } while (Next(indexArr, safety.ParamExprMap));
    }
}