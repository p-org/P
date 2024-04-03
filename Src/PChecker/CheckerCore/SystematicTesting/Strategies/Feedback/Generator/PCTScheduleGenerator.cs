using System;
using System.Collections.Generic;
using PChecker.Generator.Mutator;
using PChecker.Generator.Object;
using PChecker.SystematicTesting.Operations;
using PChecker.SystematicTesting.Strategies.Probabilistic;

namespace PChecker.Generator;

internal sealed class PctScheduleGenerator: PCTScheduler, IScheduleGenerator<PctScheduleGenerator>
{
    public System.Random Random;
    public RandomChoices<int> PriorityChoices;
    public RandomChoices<double> SwitchPointChoices;

    public PctScheduleGenerator(System.Random random, RandomChoices<int>? priorityChoices, RandomChoices<double>? switchPointChoices, int numSwitchPoints, int maxScheduleLength):
        base(numSwitchPoints, maxScheduleLength,
            new ParametricProvider(
                priorityChoices != null ? new RandomChoices<int>(priorityChoices) : new RandomChoices<int>(random),
                switchPointChoices != null ? new RandomChoices<double>(switchPointChoices) : new RandomChoices<double>(random)))
    {
        Random = random;
        var provider = (ParametricProvider) Provider;
        PriorityChoices = provider.PriorityChoices;
        SwitchPointChoices = provider.SwitchPointChoices;
    }

    public PctScheduleGenerator(CheckerConfiguration checkerConfiguration):
        this(new System.Random((int?)checkerConfiguration.RandomGeneratorSeed ?? Guid.NewGuid().GetHashCode()), null, null, checkerConfiguration.StrategyBound,  0)
    {
    }

    public PctScheduleGenerator Mutate()
    {
        return new PCTScheduleMutator().Mutate(this);
    }

    public PctScheduleGenerator New()
    {
        return new PctScheduleGenerator(Random, null, null, MaxPrioritySwitchPoints, ScheduleLength);
    }

    public PctScheduleGenerator Copy()
    {
        return new PctScheduleGenerator(Random, PriorityChoices, SwitchPointChoices, MaxPrioritySwitchPoints, ScheduleLength);
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