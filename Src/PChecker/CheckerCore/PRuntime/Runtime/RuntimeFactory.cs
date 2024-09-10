// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PChecker.StateMachines;
using PChecker.Random;
using PChecker.SystematicTesting;
using PChecker.SystematicTesting.Strategies.Probabilistic;

namespace PChecker.Runtime
{
    /// <summary>
    /// Provides methods for creating a <see cref="ControlledRuntime"/> runtime.
    /// </summary>
    public static class RuntimeFactory
    {
        /// <summary>
        /// The installed runtime instance.
        /// </summary>
        internal static ControlledRuntime InstalledRuntime { get; private set; } = CreateWithConfiguration(default);

        /// <summary>
        /// Protects access to the installed runtime.
        /// </summary>
        private static readonly object SyncObject = new object();

        /// <summary>
        /// Creates a new ControlledRuntime.
        /// </summary>
        /// <returns>The created task runtime.</returns>
        /// <remarks>
        /// Only one task runtime can be created per process. If you create a new task
        /// runtime it replaces the previously installed one.
        /// </remarks>
        public static ControlledRuntime Create() => CreateAndInstall(default);

        /// <summary>
        /// Creates a new ControlledRuntime with the specified <see cref="CheckerConfiguration"/>.
        /// </summary>
        /// <param name="checkerConfiguration">The runtime checkerConfiguration to use.</param>
        /// <returns>The created task runtime.</returns>
        /// <remarks>
        /// Only one task runtime can be created per process. If you create a new task
        /// runtime it replaces the previously installed one.
        /// </remarks>
        public static ControlledRuntime Create(CheckerConfiguration checkerConfiguration) => CreateAndInstall(checkerConfiguration);

        /// <summary>
        /// Creates a new ControlledRuntime with the specified <see cref="CheckerConfiguration"/> and sets
        /// it as the installed runtime, or returns the installed runtime if it already exists.
        /// </summary>
        private static ControlledRuntime CreateAndInstall(CheckerConfiguration checkerConfiguration)
        {
            lock (SyncObject)
            {
                // Assign the newly created runtime as the installed runtime.
                return InstalledRuntime = CreateWithConfiguration(checkerConfiguration);
            }
        }

        /// <summary>
        /// Creates a new ControlledRuntime with the specified <see cref="CheckerConfiguration"/>.
        /// </summary>
        private static ControlledRuntime CreateWithConfiguration(CheckerConfiguration checkerConfiguration)
        {
            if (checkerConfiguration is null)
            {
                checkerConfiguration = CheckerConfiguration.Create();
            }

            var valueGenerator = new RandomValueGenerator(checkerConfiguration);
            return new ControlledRuntime(checkerConfiguration, valueGenerator);
        }
    }
}