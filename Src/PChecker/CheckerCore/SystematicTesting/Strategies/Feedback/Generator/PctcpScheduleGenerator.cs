using System;
using System.Collections.Generic;
using PChecker.Feedback;
using PChecker.Generator.Mutator;
using PChecker.Generator.Object;
using PChecker.SystematicTesting.Operations;
using PChecker.SystematicTesting.Strategies.Probabilistic;
using PChecker.SystematicTesting.Strategies.Probabilistic.pctcp;

namespace PChecker.Generator;

internal class PctcpScheduleGenerator: PCTCPScheduler, IScheduleGenerator<PctcpScheduleGenerator>
{

    public System.Random Random;
    public RandomChoices<int> PriorityChoices;
    public RandomChoices<double> SwitchPointChoices;

    public PctcpScheduleGenerator(System.Random random, RandomChoices<int>? priorityChoices, RandomChoices<double>?
            switchPointChoices, int numSwitchPoints, int maxScheduleLength):
        base(numSwitchPoints, maxScheduleLength,
            new ParametricProvider(
                priorityChoices != null ? new RandomChoices<int>(priorityChoices) : new RandomChoices<int>(random),
                switchPointChoices != null ? new RandomChoices<double>(switchPointChoices) : new
        RandomChoices<double>(random)))
    {
        Random = random;
        var provider = (ParametricProvider) Provider;
        PriorityChoices = provider.PriorityChoices;
        SwitchPointChoices = provider.SwitchPointChoices;
    }
    public PctcpScheduleGenerator(CheckerConfiguration checkerConfiguration):
        this(new System.Random((int?)checkerConfiguration.RandomGeneratorSeed ?? Guid.NewGuid().GetHashCode()), null,
        null, checkerConfiguration.StrategyBound,  0)
    {
    }

    public PctcpScheduleGenerator Mutate()
    {
        return new PctcpScheduleMutator().Mutate(this);
    }

    public PctcpScheduleGenerator New()
    {
        return new PctcpScheduleGenerator(Random, null, null, MaxPrioritySwitchPoints, ScheduleLength);
    }

    public PctcpScheduleGenerator Copy()
    {
        return new PctcpScheduleGenerator(Random, PriorityChoices, SwitchPointChoices, MaxPrioritySwitchPoints,
            ScheduleLength);
    }

    public AsyncOperation? NextRandomOperation(List<AsyncOperation> enabledOperations, AsyncOperation current)
    {
        if (GetNextOperation(current, enabledOperations, out var next)) {
            return next;
        } else {
            return null;
        }
    }


    public void PrepareForNextInput()
    {
        PrepareForNextIteration();
    }

}