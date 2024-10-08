using System;
using System.Collections.Generic;
using PChecker.Feedback;
using PChecker.Generator.Mutator;
using PChecker.Generator.Object;
using PChecker.SystematicTesting.Operations;
using PChecker.SystematicTesting.Strategies.Probabilistic;

namespace PChecker.Generator;

internal class POSScheduleGenerator: POSScheduler, IScheduleGenerator<POSScheduleGenerator>
{
    public System.Random Random;
    public RandomChoices<int> PriorityChoices;
    public RandomChoices<double> SwitchPointChoices;

    public POSScheduleGenerator(System.Random random, RandomChoices<int>? priorityChoices, RandomChoices<double>? switchPointChoices):
        base(new ParametricProvider(
                priorityChoices != null ? new RandomChoices<int>(priorityChoices) : new RandomChoices<int>(random),
                switchPointChoices != null ? new RandomChoices<double>(switchPointChoices) : new RandomChoices<double>(random)))
    {
        Random = random;
        var provider = (ParametricProvider) Provider;
        PriorityChoices = provider.PriorityChoices;
        SwitchPointChoices = provider.SwitchPointChoices;
    }

    public POSScheduleGenerator(CheckerConfiguration checkerConfiguration):
        this(new System.Random((int?)checkerConfiguration.RandomGeneratorSeed ?? Guid.NewGuid().GetHashCode()), null,
            null)
    {
    }

    public POSScheduleGenerator Mutate()
    {
        return new POSScheduleMutator().Mutate(this);
    }

    public POSScheduleGenerator New()
    {
        return new POSScheduleGenerator(Random, null, null);
    }

    public POSScheduleGenerator Copy()
    {
        return new POSScheduleGenerator(Random, PriorityChoices, SwitchPointChoices);
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