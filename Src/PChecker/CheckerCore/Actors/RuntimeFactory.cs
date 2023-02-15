// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PChecker.Random;
using PChecker.Runtime;

namespace PChecker.Actors
{
    /// <summary>
    /// Provides methods for creating a <see cref="IActorRuntime"/> runtime.
    /// </summary>
    public static class RuntimeFactory
    {
        /// <summary>
        /// Creates a new actor runtime.
        /// </summary>
        /// <returns>The created actor runtime.</returns>
        /// <remarks>
        /// Only one runtime can be created per async local context. This is not a thread-safe operation.
        /// </remarks>
        public static IActorRuntime Create() => Create(default);

        /// <summary>
        /// Creates a new actor runtime with the specified <see cref="CheckerConfiguration"/>.
        /// </summary>
        /// <param name="checkerConfiguration">The runtime checkerConfiguration to use.</param>
        /// <returns>The created actor runtime.</returns>
        /// <remarks>
        /// Only one runtime can be created per async local context. This is not a thread-safe operation.
        /// </remarks>
        public static IActorRuntime Create(CheckerConfiguration checkerConfiguration)
        {
            if (checkerConfiguration is null)
            {
                checkerConfiguration = CheckerConfiguration.Create();
            }

            var valueGenerator = new RandomValueGenerator(checkerConfiguration);
            var runtime = new ActorRuntime(checkerConfiguration, valueGenerator);

            // Assign the runtime to the currently executing asynchronous control flow.
            CoyoteRuntime.AssignAsyncControlFlowRuntime(runtime);
            return runtime;
        }
    }
}
