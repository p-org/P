// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using PChecker.SystematicTesting;
using SystemCompiler = System.Runtime.CompilerServices;

namespace PChecker.Tasks
{
    /// <summary>
    /// Implements an awaitable that asynchronously yields back to the current context when awaited.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct YieldAwaitable
    {
        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private readonly TaskController TaskController;

        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
        public YieldAwaiter GetAwaiter()
        {
            TaskController?.OnGetAwaiter();
            return new YieldAwaiter(TaskController, default);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YieldAwaitable"/> struct.
        /// </summary>
        internal YieldAwaitable(TaskController taskController)
        {
            TaskController = taskController;
        }

        /// <summary>
        /// Provides an awaiter that switches into a target environment.
        /// This type is intended for compiler use only.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        public readonly struct YieldAwaiter : SystemCompiler.ICriticalNotifyCompletion, SystemCompiler.INotifyCompletion
        {
            /// <summary>
            /// Responsible for controlling the execution of tasks during systematic testing.
            /// </summary>
            private readonly TaskController TaskController;

            /// <summary>
            /// The internal yield awaiter.
            /// </summary>
            private readonly SystemCompiler.YieldAwaitable.YieldAwaiter Awaiter;

            /// <summary>
            /// Gets a value that indicates whether a yield is not required.
            /// </summary>
#pragma warning disable CA1822 // Mark members as static
            public bool IsCompleted => false;
#pragma warning restore CA1822 // Mark members as static

            /// <summary>
            /// Initializes a new instance of the <see cref="YieldAwaiter"/> struct.
            /// </summary>
            internal YieldAwaiter(TaskController taskController, SystemCompiler.YieldAwaitable.YieldAwaiter awaiter)
            {
                TaskController = taskController;
                Awaiter = awaiter;
            }

            /// <summary>
            /// Ends the await operation.
            /// </summary>
            public void GetResult()
            {
                TaskController?.OnYieldAwaiterGetResult();
                Awaiter.GetResult();
            }

            /// <summary>
            /// Posts the continuation action back to the current context.
            /// </summary>
            public void OnCompleted(Action continuation)
            {
                if (TaskController is null)
                {
                    Awaiter.OnCompleted(continuation);
                }
                else
                {
                    TaskController.ScheduleYieldAwaiterContinuation(continuation);
                }
            }

            /// <summary>
            /// Posts the continuation action back to the current context.
            /// </summary>
            public void UnsafeOnCompleted(Action continuation)
            {
                if (TaskController is null)
                {
                    Awaiter.UnsafeOnCompleted(continuation);
                }
                else
                {
                    TaskController.ScheduleYieldAwaiterContinuation(continuation);
                }
            }
        }
    }
}