using System;
using PChecker.Generator.Mutator;
using PChecker.Random;

namespace PChecker.Generator.Object;

/// <summary>
/// Controlled Random.
/// </summary>
public class ControlledRandom: IRandomValueGenerator
{
    public RandomChoices<int> IntChoices;
    public RandomChoices<double> DoubleChoices;
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="int"></param>
    /// <param name="double"></param>
    public ControlledRandom(RandomChoices<int> @int, RandomChoices<double> @double)
    {
        IntChoices = @int;
        DoubleChoices = @double;
    }

    public ControlledRandom(CheckerConfiguration checkerConfiguration) : this(new System.Random((int?)checkerConfiguration.RandomGeneratorSeed ?? Guid.NewGuid().GetHashCode()))
    {
    }

    public ControlledRandom(System.Random random)
    {
        IntChoices = new RandomChoices<int>(random);
        DoubleChoices = new RandomChoices<double>(random);
    }
    
    /// <inheritdoc/>
    public uint Seed { get; set; }
    
    /// <inheritdoc/>
    public int Next()
    {
        return IntChoices.Next();
    }

    /// <inheritdoc/>
    public int Next(int maxValue)
    {
        if (maxValue == 0)
        {
            return 0;
        }

        return IntChoices.Next() % maxValue;
    }

    /// <inheritdoc/>
    public double NextDouble()
    {
        return DoubleChoices.Next();
    }

    public ControlledRandom New()
    {
        return new ControlledRandom(IntChoices.Random);
    }
    
    public ControlledRandom Mutate()
    {
        // Mutate ~5 integers on average (the paper's target): ~5 mutation sites of size 1,
        // not ~5 runs of ~5 (which changed ~25). Small, localized changes keep the mutated
        // schedule similar-but-distinct from its parent.
        return new ControlledRandom(Utils.MutateRandomChoices(IntChoices, 5, 1, IntChoices.Random),
            Utils.MutateRandomChoices(DoubleChoices, 5, 1, DoubleChoices.Random)
        );
    }
    
}