using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.Actors;
using PChecker.Actors.Events;
using PChecker.Actors.Logging;
using PChecker.SystematicTesting.Operations;

namespace PChecker.Feedback;


internal class ConflictOpMonitor: ActorRuntimeLogBase
{

    public VectorClockGenerator VectorClockGenerator;

    // This dictionary stores all operations received by a machine.
    // Each operation is labeled with ActorId, source location, and its corresponding
    // vector clock timestamp.
    private Dictionary<string, HashSet<(Operation, Dictionary<string, int>)>> incomingOps = new();

    private Dictionary<Operation, HashSet<Operation>> conflictOps = new();
    
    public override void OnSendEvent(ActorId targetActorId, string senderName, string senderType, string senderStateName, Event e,
        Guid opGroupId, bool isTargetHalted)
    {
        var receiverKey = e.Receiver;
        var senderKey = e.Sender;
        var currentOp = new Operation(senderKey, receiverKey, e.Loc);
        if (!incomingOps.ContainsKey(receiverKey))
        {
            incomingOps.Add(receiverKey, new HashSet<(Operation, Dictionary<string, int>)>());
        }
        var opsSet = incomingOps[receiverKey];



        if (VectorClockGenerator.ContextVcMap.TryGetValue(senderKey, out var vectorClock))
        {

            foreach (var op in opsSet)
            {
                if (op.Item1.Sender == currentOp.Sender)
                {
                    continue;
                }

                if (!IsLEQ(op.Item2, vectorClock) && !IsLEQ(vectorClock, op.Item2))
                {
                    AddConflictOp(op.Item1, currentOp);
                }
            }
            opsSet.Add((currentOp, vectorClock
            .ToDictionary(entry => entry.Key, entry => entry.Value)));
        }
    }

    internal bool IsRacing(AsyncOperation op1, AsyncOperation op2)
    {
        if (op1.Type != AsyncOperationType.Send || op2.Type != AsyncOperationType.Send) {
            return false;
        }

        var operation1 = new Operation(op1.Name, op1.LastSentReceiver, op1.LastEvent!.Loc);
        var operation2 = new Operation(op2.Name, op2.LastSentReceiver, op2.LastEvent!.Loc);

        if (conflictOps.TryGetValue(operation1, out var ops)) {
            return ops.Contains(operation2);
        }
        return false;
    }

    public void Reset() {
        incomingOps.Clear();
    }

    bool IsLEQ(Dictionary<string, int> vc1, Dictionary<string, int> vc2)
    {
        foreach (var key in vc1.Keys.Union(vc2.Keys))
        {
            var op1 = vc1.GetValueOrDefault(key, 0);
            var op2 = vc2.GetValueOrDefault(key, 0);
            if (op1 > op2)
            {
                return false;
            }
        }
        return true;
    }

    void AddConflictOp(Operation op1, Operation op2)
    {
        if (!conflictOps.ContainsKey(op1)) {
            conflictOps[op1] = new HashSet<Operation>();
        }
        if (!conflictOps.ContainsKey(op2)) {
            conflictOps[op2] = new HashSet<Operation>();
        }
        conflictOps[op1].Add(op2);
        conflictOps[op2].Add(op1);
    }
    internal bool IsConflictingOp(AsyncOperation op)
    {
        if (op.Type != AsyncOperationType.Send) {
            return false;
        }

        var operation = new Operation(op.Name, op.LastSentReceiver, op.LastEvent!.Loc);
        if (conflictOps.TryGetValue(operation, out var ops ))
        {
            return ops.Count != 0;
        }
        return false;
    }
}