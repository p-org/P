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
    public ConflictOpMonitor? Monitor;

    public POSScheduleGenerator(System.Random random, RandomChoices<int>? priorityChoices, RandomChoices<double>? switchPointChoices,
        ConflictOpMonitor? monitor):
        base(new ParametricProvider(
                priorityChoices != null ? new RandomChoices<int>(priorityChoices) : new RandomChoices<int>(random),
                switchPointChoices != null ? new RandomChoices<double>(switchPointChoices) : new RandomChoices<double>(random)),
            monitor)
    {
        Random = random;
        var provider = (ParametricProvider) Provider;
        PriorityChoices = provider.PriorityChoices;
        SwitchPointChoices = provider.SwitchPointChoices;
        Monitor = monitor;
    }

    public POSScheduleGenerator(CheckerConfiguration checkerConfiguration, ConflictOpMonitor? monitor):
        this(new System.Random((int?)checkerConfiguration.RandomGeneratorSeed ?? Guid.NewGuid().GetHashCode()), null,
            null, monitor)
    {
    }

    public POSScheduleGenerator Mutate()
    {
        return new POSScheduleMutator().Mutate(this);
    }

    public POSScheduleGenerator New()
    {
        return new POSScheduleGenerator(Random, null, null, Monitor);
    }

    public POSScheduleGenerator Copy()
    {
        return new POSScheduleGenerator(Random, PriorityChoices, SwitchPointChoices, Monitor);
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