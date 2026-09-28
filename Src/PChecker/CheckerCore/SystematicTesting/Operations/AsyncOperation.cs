// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        /// Resumes the operation when the scheduler run loop selects it: either the start
        /// of a new handler or the continuation parked at a <see cref="SchedulingPoint"/>.
        /// </summary>
        internal System.Action Continuation;

        /// <summary>
        /// The receiver if the operation is Send.
        /// </summary>
        public string MessageReceiver = "";

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncOperation"/> class.
        /// </summary>
        internal AsyncOperation()
        {
            Status = AsyncOperationStatus.None;
            Type = AsyncOperationType.Start;
        }

        /// <summary>
        /// Invoked when the operation has been enabled.
        /// </summary>
        internal void OnEnabled()
        {
            Status = AsyncOperationStatus.Enabled;
        }

        /// <summary>
        /// Invoked when the operation completes.
        /// </summary>
        internal virtual void OnCompleted()
        {
            Status = AsyncOperationStatus.Completed;
            Continuation = null;
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