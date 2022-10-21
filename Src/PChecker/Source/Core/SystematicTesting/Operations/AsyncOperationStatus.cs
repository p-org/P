// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// The status of an asynchronous operation.
    /// </summary>
    internal enum AsyncOperationStatus
    {
        /// <summary>
        /// The operation does not have a status yet.
        /// </summary>
        None = 0,

        /// <summary>
        /// The operation is enabled.
        /// </summary>
        Enabled,

        /// <summary>
        /// The operation is waiting for all of its dependencies to complete.
        /// </summary>
        BlockedOnWaitAll,

        /// <summary>
        /// The operation is waiting for any of its dependencies to complete.
        /// </summary>
        BlockedOnWaitAny,

        /// <summary>
        /// The operation is waiting to receive an event.
        /// </summary>
        BlockedOnReceive,

        /// <summary>
        /// The operation is waiting to acquire a resource.
        /// </summary>
        BlockedOnResource,

        /// <summary>
        /// The operation is completed.
        /// </summary>
        Completed,

        /// <summary>
        /// The operation is canceled.
        /// </summary>
        Canceled
    }
}
