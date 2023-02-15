﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using PChecker.SystematicTesting.Operations;

namespace PChecker.SystematicTesting
{
    /// <summary>
    /// Resource that can be used to synchronize asynchronous operations.
    /// </summary>
    internal class Resource
    {
        /// <summary>
        /// The runtime associated with this resource.
        /// </summary>
        internal readonly ControlledRuntime Runtime;

        /// <summary>
        /// Set of asynchronous operations that are waiting on the resource to be released.
        /// </summary>
        private readonly HashSet<AsyncOperation> AwaitingOperations;

        /// <summary>
        /// Initializes a new instance of the <see cref="Resource"/> class.
        /// </summary>
        internal Resource()
        {
            Runtime = ControlledRuntime.Current;
            AwaitingOperations = new HashSet<AsyncOperation>();
        }

        /// <summary>
        /// Notifies that the currently executing asynchronous operation is waiting
        /// for the resource to be released.
        /// </summary>
        internal void NotifyWait()
        {
            var op = Runtime.GetExecutingOperation<AsyncOperation>();
            op.Status = AsyncOperationStatus.BlockedOnResource;
            AwaitingOperations.Add(op);
        }

        /// <summary>
        /// Notifies all waiting asynchronous operations waiting on this resource,
        /// that the resource has been released.
        /// </summary>
        internal void NotifyRelease()
        {
            foreach (var op in AwaitingOperations)
            {
                op.Status = AsyncOperationStatus.Enabled;
            }

            // We need to clear the whole set, because we signal all awaiting asynchronous
            // operations to wake up, else we could set as enabled an operation that is not
            // any more waiting for this resource at a future point.
            AwaitingOperations.Clear();
        }
    }
}
