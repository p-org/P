// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Actors
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
        /// Creates a new actor runtime with the specified <see cref="Configuration"/>.
        /// </summary>
        /// <param name="configuration">The runtime configuration to use.</param>
        /// <returns>The created actor runtime.</returns>
        /// <remarks>
        /// Only one runtime can be created per async local context. This is not a thread-safe operation.
        /// </remarks>
        public static IActorRuntime Create(Configuration configuration)
        {
            if (configuration is null)
            {
                configuration = Configuration.Create();
            }

            var valueGenerator = new RandomValueGenerator(configuration);
            var runtime = new ActorRuntime(configuration, valueGenerator);

            // Assign the runtime to the currently executing asynchronous control flow.
            CoyoteRuntime.AssignAsyncControlFlowRuntime(runtime);
            return runtime;
        }
    }
}
