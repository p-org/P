using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.Actors;
using PChecker.Actors.Events;
using PChecker.Actors.Logging;
using PChecker.Feedback;
using PChecker.SystematicTesting.Operations;

internal class AbstractScheduleObserver : ActorRuntimeLogBase
{
    AbstractSchedule abstractSchedule;
    Dictionary<string, List<Operation>> opQueue = new();
    Dictionary<Constraint, State> constraintState = new();
    public Dictionary<string, HashSet<Constraint>> avoidSendTo = new();
    public Dictionary<Operation, HashSet<Constraint>> avoidSchedule = new();
    public Dictionary<Operation, HashSet<Constraint>> lookingForSchedule = new();
    public Dictionary<string, HashSet<Constraint>> lookingForSendTo = new();
    public Dictionary<string, HashSet<Constraint>> relevantConstraintsByReceiver = new();
    public Dictionary<Operation, HashSet<Constraint>> relevantConstraintsByOp = new();

    public Dictionary<Constraint, long> visitedConstraints = new();
    public Dictionary<Constraint, long> allVisitedConstraints = new();

    public void OnNewAbstractSchedule(AbstractSchedule schedule)
    {
        abstractSchedule = schedule;

        // Reset everything;
        opQueue.Clear();
        avoidSendTo.Clear();
        avoidSchedule.Clear();
        constraintState.Clear();
        lookingForSchedule.Clear();
        lookingForSendTo.Clear();
        relevantConstraintsByOp.Clear();
        relevantConstraintsByReceiver.Clear();
        visitedConstraints.Clear();

        foreach (var constraint in abstractSchedule.constraints) {
            if (constraint.positive) {
                constraintState[constraint] = State.PosNoInfo;
            } else {
                constraintState[constraint] = State.NegNoInfo;
            }

            if (!avoidSendTo.ContainsKey(constraint.op1.Receiver)) {
                avoidSendTo[constraint.op1.Receiver] = new();
            }

            if (!lookingForSendTo.ContainsKey(constraint.op1.Receiver)) {
                lookingForSendTo[constraint.op1.Receiver] = new();
            }

            if (!relevantConstraintsByReceiver.ContainsKey(constraint.op1.Receiver)) {
                relevantConstraintsByReceiver[constraint.op1.Receiver] = new();
            }

            if (!relevantConstraintsByOp.ContainsKey(constraint.op1)) {
                relevantConstraintsByOp[constraint.op1] = new();
            }

            if (!relevantConstraintsByOp.ContainsKey(constraint.op2)) {
                relevantConstraintsByOp[constraint.op2] = new();
            }

            if (!avoidSchedule.ContainsKey(constraint.op1)) {
                avoidSchedule[constraint.op1] = new();
            }

            if (!avoidSchedule.ContainsKey(constraint.op2)) {
                avoidSchedule[constraint.op2] = new();
            }

            if (!lookingForSchedule.ContainsKey(constraint.op1)) {
                lookingForSchedule[constraint.op1] = new();
            }

            if (!lookingForSchedule.ContainsKey(constraint.op2)) {
                lookingForSchedule[constraint.op2] = new();
            }


            relevantConstraintsByOp[constraint.op1].Add(constraint);
            relevantConstraintsByOp[constraint.op2].Add(constraint);
        }
    }


    public HashSet<Constraint> GetRelevantConstraints(Operation op)
    {
        var constraints = new HashSet<Constraint>();
        if (relevantConstraintsByReceiver.ContainsKey(op.Receiver)) {
            constraints.UnionWith(relevantConstraintsByReceiver[op.Receiver]);
        }
        if (relevantConstraintsByOp.ContainsKey(op)) {
            constraints.UnionWith(relevantConstraintsByOp[op]);
        }
        return constraints;
    }

    public void OnExecute(Operation op)
    {
        foreach (var constraint in GetRelevantConstraints(op))
        {
            var newState = constraintState[constraint];
            if (constraint.positive)
            {
                if (constraint.op1 == op)
                {
                    newState = State.PosExctdW;
                }
                else if (constraintState[constraint] == State.PosReachR && constraint.op2 == op)
                {
                    newState = State.PosNoInfo;
                }
                else if (constraintState[constraint] == State.PosExctdW && constraint.op2 == op)
                {
                    newState = State.PosSat;
                    relevantConstraintsByOp[constraint.op1].Remove(constraint);
                    relevantConstraintsByOp[constraint.op2].Remove(constraint);
                }
                else if (constraintState[constraint] == State.PosExctdW && avoidSendTo[op.Receiver].Contains(constraint))
                {
                    newState = State.PosNoInfo;
                }
            }
            else
            {
                if (constraint.op1 == op)
                {
                    newState = State.NegExctdW;
                }
                else if (constraintState[constraint] == State.NegExctdW && constraint.op2 != op)
                {
                    newState = State.NegOtherW;
                }
                else if (constraintState[constraint] == State.NegOtherW && constraint.op2 == op)
                {
                    newState = State.NegOtherW;
                }
                else if (constraintState[constraint] == State.NegExctdW && constraint.op2 == op)
                {
                    newState = State.NegUnsat;
                    relevantConstraintsByOp[constraint.op1].Remove(constraint);
                    relevantConstraintsByOp[constraint.op2].Remove(constraint);
                }

            }
            if (newState != constraintState[constraint])
            {
                CleanState(constraint);
                constraintState[constraint] = newState;
                UpdateLookFor(constraint);
            }
        }

    }

    public void OnNewOp(Operation op)
    {
        foreach (var constraint in GetRelevantConstraints(op))
        {
            var newState = constraintState[constraint];
            if (constraint.positive)
            {
                if (constraintState[constraint] != State.PosExctdW && constraint.op2 == op)
                {
                    newState = State.PosReachR;
                }
            }
            else
            {
                if (constraint.op1 == op)
                {
                    newState = State.NegReachW;
                }
            }
            if (newState != constraintState[constraint])
            {
                CleanState(constraint);
                constraintState[constraint] = newState;
                UpdateLookFor(constraint);
            }
        }
    }

    public void CleanState(Constraint constraint)
    {
        switch (constraintState[constraint])
        {
            case State.PosReachR:
                avoidSchedule[constraint.op2].Remove(constraint);
                lookingForSchedule[constraint.op1].Remove(constraint);
                break;
            case State.PosExctdW:
                lookingForSchedule[constraint.op2].Remove(constraint);
                avoidSendTo[constraint.op1.Receiver].Remove(constraint);
                relevantConstraintsByReceiver[constraint.op1.Receiver].Remove(constraint);
                break;
            case State.NegExctdW:
                avoidSchedule[constraint.op2].Remove(constraint);
                lookingForSendTo[constraint.op1.Receiver].Remove(constraint);
                relevantConstraintsByReceiver[constraint.op1.Receiver].Remove(constraint);
                break;
            case State.NegReachW:
                avoidSchedule[constraint.op1].Remove(constraint);
                break;
            case State.NegOtherW:
                lookingForSchedule[constraint.op2].Remove(constraint);
                break;
        }
    }

    public void UpdateLookFor(Constraint constraint)
    {
        switch (constraintState[constraint])
        {
            case State.PosReachR:
                avoidSchedule[constraint.op2].Add(constraint);
                lookingForSchedule[constraint.op1].Add(constraint);
                break;
            case State.PosExctdW:
                lookingForSchedule[constraint.op2].Add(constraint);
                avoidSendTo[constraint.op1.Receiver].Add(constraint);
                relevantConstraintsByReceiver[constraint.op1.Receiver].Add(constraint);
                break;
            case State.NegExctdW:
                avoidSchedule[constraint.op2].Add(constraint);
                lookingForSendTo[constraint.op1.Receiver].Add(constraint);
                relevantConstraintsByReceiver[constraint.op1.Receiver].Add(constraint);
                break;
            case State.NegReachW:
                avoidSchedule[constraint.op1].Add(constraint);
                break;
            case State.NegOtherW:
                lookingForSchedule[constraint.op2].Add(constraint);
                break;
        }
    }

    public override void OnSendEvent(ActorId targetActorId, string senderName, string senderType, string senderStateName, Event e,
        Guid opGroupId, bool isTargetHalted)
    {
        OnNewOp(new Operation(e.Sender, e.Receiver, e.Loc));
    }

    public override void OnDequeueEvent(ActorId id, string stateName, Event e)
    {
        var receiverName = e.Receiver;
        var senderName = e.Sender;

        if (!opQueue.ContainsKey(receiverName))
        {
            opQueue[receiverName] = new();
        }
        var queue = opQueue[receiverName];

        queue.Add(new Operation(senderName, receiverName, e.Loc));
        OnExecute(new Operation(senderName, receiverName, e.Loc));

        if (queue.Count <= 1) return;
        var op1 = queue[^2];
        var op2 = queue[^1];

        var c = new Constraint(op1, op2, true);

        if (!visitedConstraints.ContainsKey(c)) {
            visitedConstraints[c] = 0;
        }
        visitedConstraints[c] += 1;
    }

    public int GetTraceHash() {
        if (visitedConstraints.Count == 0) {
            return "".GetHashCode();
        }
        string s = visitedConstraints.ToList().Select(it => $"<{it.Key}, {it.Value}>").OrderBy(it => it).Aggregate((current, next) => current + "," + next);
        return s.GetHashCode();
    }

    public bool CheckNoveltyAndUpdate()
    {
        bool isNovel = false;
        foreach (var constraint in visitedConstraints) {
            if (!allVisitedConstraints.ContainsKey(constraint.Key))
            {
                allVisitedConstraints[constraint.Key] = 0;
            }
            if (constraint.Value > allVisitedConstraints[constraint.Key])
            {
                allVisitedConstraints[constraint.Key] = constraint.Value;
                isNovel = true;
            }
        }
        return isNovel;
    }

    public bool CheckAbstractTimelineSatisfied()
    {
        return constraintState.All(it => {
            if (it.Key.positive) {
                return it.Value == State.PosSat;
            } else {
                return it.Value != State.NegUnsat;
            }
        });
    }

    public bool ShouldAvoid(AsyncOperation op)
    {
        if (op.Type == AsyncOperationType.Send)
        {
            var operation = new Operation(op.Name, op.LastSentReceiver, op.LastEvent!.Loc);
            if (avoidSchedule.ContainsKey(operation))
            {
                return true;
            }
            if (avoidSendTo.ContainsKey(operation.Receiver)
                && avoidSendTo[operation.Receiver].Any(it => it.op2 != operation))
            {
                return true;
            }
        }
        return false;
    }

    public bool ShouldTake(AsyncOperation op)
    {
        if (op.Type == AsyncOperationType.Send)
        {
            var operation = new Operation(op.Name, op.LastSentReceiver, op.LastEvent!.Loc);
            if (lookingForSchedule.ContainsKey(operation))
            {
                return true;
            }
            if (lookingForSendTo.ContainsKey(operation.Receiver) &&
                lookingForSendTo[operation.Receiver].Any(it => it.op2 == operation))
            {
                return true;
            }
            return false;
        }
        return true;
    }
}