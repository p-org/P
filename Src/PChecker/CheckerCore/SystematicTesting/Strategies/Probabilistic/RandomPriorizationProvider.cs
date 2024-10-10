using PChecker.Random;

namespace PChecker.SystematicTesting.Strategies.Probabilistic;

internal class RandomPriorizationProvider: PriorizationProvider
{

    /// <summary>
    /// Random value generator.
    /// </summary>
    private readonly IRandomValueGenerator RandomValueGenerator;

    public RandomPriorizationProvider(IRandomValueGenerator generator)
    {
        RandomValueGenerator = generator;
    }
    public int AssignPriority(int numOps)
    {
        return RandomValueGenerator.Next(numOps) + 1;
    }

    public double SwitchPointChoice()
    {
        return RandomValueGenerator.NextDouble();
    }

}