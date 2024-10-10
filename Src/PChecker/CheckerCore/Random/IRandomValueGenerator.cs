// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PChecker.Random
{
    /// <summary>
    /// Interface for random value generators.
    /// </summary>
    internal interface IRandomValueGenerator
    {
        /// <summary>
        /// The seed currently used by the generator.
        /// </summary>
        uint Seed { get; set; }

        /// <summary>
        /// Returns a non-negative random number.
        /// </summary>
        int Next();

        /// <summary>
        /// Returns a non-negative random number less than maxValue.
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound</param>
        int Next(int maxValue);

        /// <summary>
        /// Returns a random floating-point number that is greater
        /// than or equal to 0.0, and less than 1.0.
        /// </summary>
        double NextDouble();
    }
}