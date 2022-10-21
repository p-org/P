// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Basic random value generator that uses the <see cref="System.Random"/> generator.
    /// </summary>
    internal class RandomValueGenerator : IRandomValueGenerator
    {
        /// <summary>
        /// Device for generating random numbers.
        /// </summary>
        private System.Random Random;

        /// <summary>
        /// The seed currently used by the generator.
        /// </summary>
        private uint RandomSeed;

        /// <summary>
        /// The seed currently used by the generator.
        /// </summary>
        public uint Seed
        {
            get => this.RandomSeed;

            set
            {
                this.RandomSeed = value;
                this.Random = new System.Random((int)this.RandomSeed);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomValueGenerator"/> class.
        /// </summary>
        internal RandomValueGenerator(Configuration configuration)
        {
            this.RandomSeed = configuration.RandomGeneratorSeed ?? (uint)Guid.NewGuid().GetHashCode();
            this.Random = new System.Random((int)this.RandomSeed);
        }

        /// <summary>
        /// Returns a non-negative random number.
        /// </summary>
        public int Next() => this.Random.Next();

        /// <summary>
        /// Returns a non-negative random number less than the specified max value.
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound.</param>
        public int Next(int maxValue) => this.Random.Next(maxValue);

        /// <summary>
        /// Returns a random floating-point number that is greater
        /// than or equal to 0.0, and less than 1.0.
        /// </summary>
        public double NextDouble() => this.Random.NextDouble();
    }
}
