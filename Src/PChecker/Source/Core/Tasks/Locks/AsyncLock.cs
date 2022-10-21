// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;
using TCS = System.Threading.Tasks.TaskCompletionSource<object>;

namespace Microsoft.Coyote.Tasks
{
    /// <summary>
    /// A non-reentrant mutual exclusion lock that can be acquired asynchronously
    /// in a first-in first-out order. During testing, the lock is automatically
    /// replaced with a controlled mocked version.
    /// </summary>
    public class AsyncLock
    {
        /// <summary>
        /// Queue of tasks awaiting to acquire the lock.
        /// </summary>
        protected readonly Queue<TCS> Awaiters;

        /// <summary>
        /// True if the lock has been acquired, else false.
        /// </summary>
        protected internal bool IsAcquired { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLock"/> class.
        /// </summary>
        protected AsyncLock()
        {
            this.Awaiters = new Queue<TCS>();
            this.IsAcquired = false;
        }

        /// <summary>
        /// Creates a new mutual exclusion lock.
        /// </summary>
        /// <returns>The asynchronous mutual exclusion lock.</returns>
        public static AsyncLock Create() => CoyoteRuntime.IsExecutionControlled ? new Mock() : new AsyncLock();

        /// <summary>
        /// Tries to acquire the lock asynchronously, and returns a task that completes
        /// when the lock has been acquired. The returned task contains a releaser that
        /// releases the lock when disposed. This is not a reentrant operation.
        /// </summary>
        public virtual async Task<Releaser> AcquireAsync()
        {
            TCS awaiter;
            lock (this.Awaiters)
            {
                if (this.IsAcquired)
                {
                    awaiter = new TCS();
                    this.Awaiters.Enqueue(awaiter);
                }
                else
                {
                    this.IsAcquired = true;
                    awaiter = null;
                }
            }

            if (awaiter != null)
            {
                await awaiter.Task;
            }

            return new Releaser(this);
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        protected virtual void Release()
        {
            TCS awaiter = null;
            lock (this.Awaiters)
            {
                if (this.Awaiters.Count > 0)
                {
                    awaiter = this.Awaiters.Dequeue();
                }
                else
                {
                    this.IsAcquired = false;
                }
            }

            if (awaiter != null)
            {
                awaiter.SetResult(null);
            }
        }

        /// <summary>
        /// Releases the acquired <see cref="AsyncLock"/> when disposed.
        /// </summary>
        public struct Releaser : IDisposable
        {
            /// <summary>
            /// The acquired lock.
            /// </summary>
            private readonly AsyncLock AsyncLock;

            /// <summary>
            /// Initializes a new instance of the <see cref="Releaser"/> struct.
            /// </summary>
            internal Releaser(AsyncLock asyncLock)
            {
                this.AsyncLock = asyncLock;
            }

            /// <summary>
            /// Releases the acquired lock.
            /// </summary>
            public void Dispose() => this.AsyncLock?.Release();
        }

        /// <summary>
        /// Mock implementation of <see cref="AsyncLock"/> that can be controlled during systematic testing.
        /// </summary>
        private class Mock : AsyncLock
        {
            /// <summary>
            /// The resource associated with this lock.
            /// </summary>
            private readonly Resource Resource;

            /// <summary>
            /// Initializes a new instance of the <see cref="Mock"/> class.
            /// </summary>
            internal Mock()
                : base()
            {
                this.Resource = new Resource();
            }

            /// <inheritdoc/>
            public override Task<Releaser> AcquireAsync()
            {
                this.Resource.Runtime.ScheduleNextOperation();

                TCS awaiter;
                if (this.IsAcquired)
                {
                    awaiter = new TCS();
                    this.Awaiters.Enqueue(awaiter);

                    // We need this, because when a resource gets released it notifies all asynchronous
                    // operations waiting to acquire it, even if such an operation is still blocked.
                    while (!awaiter.Task.IsCompleted)
                    {
                        // The resource is not available yet, notify the scheduler that the executing
                        // asynchronous operation is blocked, so that it cannot be scheduled during
                        // systematic testing exploration, which could deadlock.
                        this.Resource.NotifyWait();
                        this.Resource.Runtime.ScheduleNextOperation();
                    }
                }
                else
                {
                    this.IsAcquired = true;
                }

                return Task.FromResult(new Releaser(this));
            }

            /// <inheritdoc/>
            protected override void Release()
            {
                TCS awaiter = null;
                if (this.Awaiters.Count > 0)
                {
                    awaiter = this.Awaiters.Dequeue();
                }
                else
                {
                    this.IsAcquired = false;
                }

                if (awaiter != null)
                {
                    // Notifies any asynchronous operations that are awaiting to acquire the
                    // lock, that the lock has been released.
                    this.Resource.NotifyRelease();
                    awaiter.SetResult(null);

                    // This must be called outside the context of the lock, because it notifies
                    // the scheduler to try schedule another asynchronous operation that could
                    // in turn try to acquire this lock causing a deadlock.
                    this.Resource.Runtime.ScheduleNextOperation();
                }
            }
        }
    }
}
