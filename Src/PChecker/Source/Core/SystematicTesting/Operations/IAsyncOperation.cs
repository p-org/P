// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Interface of an asynchronous operation that can be controlled during testing.
    /// </summary>
    internal interface IAsyncOperation
    {
        /// <summary>
        /// The unique id of the operation.
        /// </summary>
        ulong Id { get; }

        /// <summary>
        /// The unique name of the operation.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The status of the operation. An operation can be scheduled only
        /// if it is <see cref="AsyncOperationStatus.Enabled"/>.
        /// </summary>
        AsyncOperationStatus Status { get; }

        /// <summary>
        /// A value that represents the hashed program state when
        /// this operation last executed.
        /// </summary>
        int HashedProgramState { get; }
    }
}
