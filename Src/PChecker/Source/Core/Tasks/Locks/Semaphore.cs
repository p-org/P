// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Tasks
{
    /// <summary>
    /// A semaphore that limits the number of tasks that can access a resource. During testing,
    /// the semaphore is automatically replaced with a controlled mocked version.
    /// </summary>
    public class Semaphore : IDisposable
    {
        /// <summary>
        /// Limits the number of tasks that can access a resource.
        /// </summary>
        private readonly SemaphoreSlim Instance;

        /// <summary>
        /// Number of remaining tasks that can enter the semaphore.
        /// </summary>
        public virtual int CurrentCount => this.Instance.CurrentCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="Semaphore"/> class.
        /// </summary>
        protected Semaphore(SemaphoreSlim semaphore)
        {
            this.Instance = semaphore;
        }

        /// <summary>
        /// Creates a new semaphore.
        /// </summary>
        /// <returns>The semaphore.</returns>
        public static Semaphore Create(int initialCount, int maxCount) => CoyoteRuntime.IsExecutionControlled ?
            new Mock(initialCount, maxCount) : new Semaphore(new SemaphoreSlim(initialCount, maxCount));

        /// <summary>
        /// Blocks the current task until it can enter the semaphore.
        /// </summary>
        public virtual void Wait() => this.Instance.Wait();

        /// <summary>
        /// Asynchronously waits to enter the semaphore.
        /// </summary>
        public virtual Task WaitAsync() => this.Instance.WaitAsync().WrapInControlledTask();

        /// <summary>
        /// Releases the semaphore.
        /// </summary>
        public virtual void Release() => this.Instance.Release();

        /// <summary>
        /// Releases resources used by the semaphore.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            this.Instance?.Dispose();
        }

        /// <summary>
        /// Releases resources used by the semaphore.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Mock implementation of <see cref="Semaphore"/> that can be controlled during systematic testing.
        /// </summary>
        private sealed class Mock : Semaphore
        {
            /// <summary>
            /// The resource associated with this semaphore.
            /// </summary>
            private readonly Resource Resource;

            /// <summary>
            /// The maximum number of requests that can be granted concurrently.
            /// </summary>
            private readonly int MaxCount;

            /// <summary>
            /// The number of requests that have been granted concurrently.
            /// </summary>
            private int NumAcquired;

            /// <summary>
            /// Number of remaining tasks that can enter the semaphore.
            /// </summary>
            public override int CurrentCount => this.MaxCount - this.NumAcquired;

            /// <summary>
            /// Initializes a new instance of the <see cref="Mock"/> class.
            /// </summary>
            internal Mock(int initialCount, int maxCount)
                : base(default)
            {
                this.Resource = new Resource();
                this.Resource.Runtime.Assert(initialCount >= 0,
                    "Cannot create semaphore with initial count of {0}. The count must be equal or greater than 0.", initialCount);
                this.Resource.Runtime.Assert(initialCount <= maxCount,
                    "Cannot create semaphore with initial count of {0}. The count be equal or less than max count of {1}.",
                    initialCount, maxCount);
                this.Resource.Runtime.Assert(maxCount > 0,
                    "Cannot create semaphore with max count of {0}. The count must be greater than 0.", maxCount);
                this.MaxCount = maxCount;
                this.NumAcquired = maxCount - initialCount;
            }

            /// <inheritdoc/>
            public override void Wait()
            {
                this.Resource.Runtime.ScheduleNextOperation();

                // We need this loop, because when a resource gets released it notifies all asynchronous
                // operations waiting to acquire it, even if such an operation is still blocked.
                while (this.CurrentCount == 0)
                {
                    // The resource is not available yet, notify the scheduler that the executing
                    // asynchronous operation is blocked, so that it cannot be scheduled during
                    // systematic testing exploration, which could deadlock.
                    this.Resource.NotifyWait();
                    this.Resource.Runtime.ScheduleNextOperation();
                }

                this.NumAcquired++;
            }

            /// <inheritdoc/>
            public override Task WaitAsync()
            {
                this.Wait();
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public override void Release()
            {
                this.NumAcquired--;
                this.Resource.Runtime.Assert(this.NumAcquired >= 0,
                    "Cannot release semaphore as it has reached max count of {0}.", this.MaxCount);

                // Release the semaphore and notify any awaiting asynchronous operations.
                this.Resource.NotifyRelease();

                // This must be called outside the context of the semaphore, because it notifies
                // the scheduler to try schedule another asynchronous operation that could in turn
                // try to acquire this semaphore causing a deadlock.
                this.Resource.Runtime.ScheduleNextOperation();
            }
        }
    }
}
