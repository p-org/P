using System;
using System.IO;
using PChecker.Generator.Object;

namespace PChecker.Generator.Mutator;

public class RandomInputMutator : IMutator<RandomInputGenerator>
{
    private readonly int _meanMutationCount = 10;
    private readonly int _meanMutationSize = 10;
    private System.Random _random = new();
    public RandomInputGenerator Mutate(RandomInputGenerator prev)
    {
        return new RandomInputGenerator(prev.Random, Utils.MutateRandomChoices(prev.IntChoices, _meanMutationCount, _meanMutationSize, _random),
            Utils.MutateRandomChoices(prev.DoubleChoices, _meanMutationCount, _meanMutationSize, _random));
    }
}