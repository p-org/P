using System;
using System.Diagnostics;
using System.IO;
using PChecker.Generator.Mutator;
using PChecker.Generator.Object;

namespace PChecker.Generator
{

    /// <summary>
    /// This class implements a JQF-style stream-based input generator.
    /// See more: https://github.com/rohanpadhye/JQF
    /// </summary>
    public class RandomInputGenerator : IInputGenerator<RandomInputGenerator>
    {
        /// <summary>
        /// Device for generating random numbers.
        /// </summary>
        internal readonly System.Random Random;

        internal RandomChoices<int> IntChoices;
        internal RandomChoices<double> DoubleChoices;

        public RandomInputGenerator(System.Random random, RandomChoices<int>? intChoices, RandomChoices<double>? doubleChoices)
        {
            Random = random;
            IntChoices = intChoices != null ? new RandomChoices<int>(intChoices) : new RandomChoices<int>(Random);
            DoubleChoices = doubleChoices != null ? new RandomChoices<double>(doubleChoices) : new RandomChoices<double>(Random);
        }


        /// <summary>
        /// Create a stream based value generator using CheckerConfiguration.
        /// </summary>
        /// <param name="checkerConfiguration"></param>
        public RandomInputGenerator(CheckerConfiguration checkerConfiguration):
            this(new System.Random((int?)checkerConfiguration.RandomGeneratorSeed ?? Guid.NewGuid().GetHashCode()), null, null)
        {
        }

        /// <summary>
        /// Create a default stream based value generator.
        /// </summary>
        public RandomInputGenerator():
            this(new System.Random(Guid.NewGuid().GetHashCode()), null, null)
        {
        }


        /// <summary>
        /// Create a stream based value generator with an existing generator.
        /// </summary>
        /// <param name="other"></param>
        public RandomInputGenerator(RandomInputGenerator other) : this(other.Random, other.IntChoices, other.DoubleChoices)
        {
        }

        /// <summary>
        /// Returns a non-negative random number.
        /// </summary>
        public int Next()
        {
            return IntChoices.Next();
        }


        /// <summary>
        /// Returns a non-negative random number less than the specified max value.
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound.</param>
        public int Next(int maxValue)
        {
            var value = maxValue == 0 ? 0 : Next() % maxValue;
            return value;
        }

        /// <summary>
        /// Returns a random floating-point number that is greater
        /// than or equal to 0.0, and less than 1.0.
        /// </summary>
        public double NextDouble()
        {
            return DoubleChoices.Next();
        }

        public RandomInputGenerator Mutate()
        {
            return new RandomInputMutator().Mutate(this);
        }

        public RandomInputGenerator Copy()
        {
            return new RandomInputGenerator(this);
        }

        public RandomInputGenerator New()
        {
            return new RandomInputGenerator(Random, null, null);
        }
    }
}