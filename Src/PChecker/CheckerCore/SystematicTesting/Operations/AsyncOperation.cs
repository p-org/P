// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PChecker.Actors.Events;
#if !DEBUG
using System.Diagnostics;
#endif

namespace PChecker.SystematicTesting.Operations
{
    /// <summary>
    /// An abstract asynchronous operation that can be controlled during testing.
    /// </summary>
#if !DEBUG
    [DebuggerStepThrough]
#endif
    internal abstract class AsyncOperation : IAsyncOperation
    {
        /// <inheritdoc/>
        public abstract ulong Id { get; }

        /// <inheritdoc/>
        public abstract string Name { get; }

        /// <inheritdoc/>
        public AsyncOperationStatus Status { get; internal set; }

        /// <summary>
        /// The type of the operation.
        /// </summary>
        internal AsyncOperationType Type;

        /// <summary>
        /// A value that represents the hashed program state when
        /// this operation last executed.
        /// </summary>
        public int HashedProgramState { get; internal set; }

        /// <summary>
        /// Is the source of the operation active.
        /// </summary>
        internal bool IsActive; // TODO: figure out if this can be replaced by status.

        /// <summary>
        /// True if the handler of the source of the operation is running, else false.
        /// </summary>
        internal bool IsHandlerRunning; // TODO: figure out if this can be replaced by status.

        /// <summary>
        /// True if the next awaiter is controlled, else false.
        /// </summary>
        internal bool IsAwaiterControlled;
        public Event? LastEvent = null;
        public string LastSentReceiver = "";

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncOperation"/> class.
        /// </summary>
        internal AsyncOperation()
        {
            Status = AsyncOperationStatus.None;
            IsActive = false;
            IsHandlerRunning = false;
            IsAwaiterControlled = false;
            Type = AsyncOperationType.Start;
        }

        /// <summary>
        /// Invoked when the operation has been enabled.
        /// </summary>
        internal void OnEnabled()
        {
            Status = AsyncOperationStatus.Enabled;
            IsActive = false;
            IsHandlerRunning = false;
        }

        /// <summary>
        /// Invoked when the operation completes.
        /// </summary>
        internal virtual void OnCompleted()
        {
            Status = AsyncOperationStatus.Completed;
            IsHandlerRunning = false;
        }

        /// <summary>
        /// Tries to enable the operation, if it was not already enabled.
        /// </summary>
        internal virtual void TryEnable()
        {
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is AsyncOperation op)
            {
                return Id == op.Id;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => (int)Id;
    }
}