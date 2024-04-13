using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using PChecker.Actors;
using PChecker.Feedback;
using PChecker.Random;


enum State {
    PosReachR,
    PosExctdW,
    PosNoInfo,
    PosSat,
    NegExctdW,
    NegReachW,
    NegOtherW,
    NegNoInfo,
    NegUnsat,
}

public record Constraint(Operation op1, Operation op2, bool positive)
{
    public override string ToString()
    {
        return $"({op1}, {op2}, {positive})";
    }
}
public record AbstractSchedule(HashSet<Constraint> constraints) {

    internal AbstractSchedule Mutate(List<Constraint> allConstraints, IRandomValueGenerator random)
    {
        List<Constraint> constraints = new(this.constraints);

        int op = random.Next(4);
        switch (op) {
            case 0: {

                int index = random.Next(allConstraints.Count);
                constraints.Add(allConstraints[index]);
                break;
            }
            case 1: {
                if (constraints.Count > 1) {
                    int index = random.Next(constraints.Count);
                    constraints.RemoveAt(index);
                    index = random.Next(allConstraints.Count);
                    constraints.Add(allConstraints[index]);
                }
                break;
            }
            case 2: {
                if (constraints.Count > 1) {
                    int index = random.Next(constraints.Count);
                    constraints.RemoveAt(index);
                }
                break;
            }
            case 3: {
                if (constraints.Count > 1) {
                    int index = random.Next(constraints.Count);
                    var c = constraints[index];
                    constraints.RemoveAt(index);
                    constraints.Add(new Constraint(c.op1, c.op2, !c.positive));
                }
                break;
            }
        }
        return new(constraints.ToHashSet());
    }
}