using System.Collections.Generic;

namespace PChecker.SystematicTesting.Strategies.Probabilistic.pctcp;

public class Chain
{
    public List<OperationWithId> Ops = new();
    public int CurrentIndex = 0;

    public OperationWithId CurrentOp() {
        if (CurrentIndex < Ops.Count)
        {
            return Ops[CurrentIndex];
        }
        return null;
    }

    public List<OperationWithId> SliceSuccessors(int op)
    {
        var index = Ops.FindIndex(it => it.Id == op);
        return Ops.GetRange(index, Ops.Count - index - 1);
    }

    public void AppendAll(List<OperationWithId> ops)
    {
        Ops.AddRange(ops);
    }

    public void RemoveAll(List<OperationWithId> ops)
    {
        Ops.RemoveAll(it => ops.Contains(it));
    }
}