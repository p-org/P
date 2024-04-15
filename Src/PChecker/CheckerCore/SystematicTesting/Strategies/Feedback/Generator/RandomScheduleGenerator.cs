using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.Generator.Mutator;
using PChecker.Generator.Object;
using PChecker.SystematicTesting.Operations;

namespace PChecker.Generator;

internal class RandomScheduleGenerator: IScheduleGenerator<RandomScheduleGenerator>
{
    internal readonly System.Random Random;

    public RandomChoices<int> IntChoices;

    public RandomScheduleGenerator(System.Random random, RandomChoices<int>? intChoices)
    {
        Random = random;
        IntChoices = intChoices != null ? new RandomChoices<int>(intChoices) : new RandomChoices<int>(Random);
    }

    public RandomScheduleGenerator(CheckerConfiguration checkerConfiguration):
        this(new System.Random((int?)checkerConfiguration.RandomGeneratorSeed ?? Guid.NewGuid().GetHashCode()) , null)
    {
    }

    public AsyncOperation? NextRandomOperation(List<AsyncOperation> ops, AsyncOperation current)
    {
        var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
        if (enabledOperations.Count == 0)
        {
            return null;
        }

        if (enabledOperations.Count == 1)
        {
            return enabledOperations[0];
        }
        var idx = IntChoices.Next() % enabledOperations.Count;
        return enabledOperations[idx];
    }

    public RandomScheduleGenerator Mutate()
    {
        return new RandomScheduleMutator().Mutate(this);
    }

    public RandomScheduleGenerator Copy()
    {
        return new RandomScheduleGenerator(Random, IntChoices);
    }

    public void PrepareForNextInput()
    {
    }

    public RandomScheduleGenerator New()
    {
        return new RandomScheduleGenerator(Random, null);
    }
}