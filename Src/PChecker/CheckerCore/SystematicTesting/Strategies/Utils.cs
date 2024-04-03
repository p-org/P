using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.SystematicTesting.Operations;

namespace PChecker.SystematicTesting.Strategies;

public class Utils
{
    internal static List<AsyncOperation> FindHighPriorityOperations(IEnumerable<AsyncOperation> ops, HashSet<Type> interestingEvents)
    {
        var highOps = ops.Where(it =>

            {
                if (it.Status == AsyncOperationStatus.Enabled)
                {
                    if (it is ActorOperation act)
                    {
                        if (act.Type == AsyncOperationType.Send)
                        {
                            if (act.LastEvent != null)
                            {
                                return !interestingEvents.Contains(act.LastEvent.GetType());
                            }
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            }
        ).ToList();
        if (highOps.Count != 0)
        {
            return highOps;
        }
        return ops.Where(
            op =>
            {
                return op.Status is AsyncOperationStatus.Enabled;
            }
        ).ToList();
    }
}