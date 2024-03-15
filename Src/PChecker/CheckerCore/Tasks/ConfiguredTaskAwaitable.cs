// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using PChecker.SystematicTesting;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemTasks = System.Threading.Tasks;

namespace PChecker.Tasks
{
    /// <summary>
    /// Provides an awaitable object that is the outcome of invoking <see cref="Task.ConfigureAwait"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct ConfiguredTaskAwaitable
    {
        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private readonly TaskController TaskController;

        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly ConfiguredTaskAwaiter Awaiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredTaskAwaitable"/> struct.
        /// </summary>
        internal ConfiguredTaskAwaitable(TaskController taskController, SystemTasks.Task awaitedTask,
            bool continueOnCapturedContext)
        {
            TaskController = taskController;
            Awaiter = new ConfiguredTaskAwaiter(taskController, awaitedTask, continueOnCapturedContext);
        }

        /// <summary>
        /// Returns an awaiter for this awaitable object.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public ConfiguredTaskAwaiter GetAwaiter()
        {
            TaskController?.OnGetAwaiter();
            return Awaiter;
        }

        /// <summary>
        /// Provides an awaiter for an awaitable object. This type is intended for compiler use only.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        public struct ConfiguredTaskAwaiter : SystemCompiler.ICriticalNotifyCompletion, SystemCompiler.INotifyCompletion
        {
            /// <summary>
            /// Responsible for controlling the execution of tasks during systematic testing.
            /// </summary>
            private readonly TaskController TaskController;

            /// <summary>
            /// The task being awaited.
            /// </summary>
            private readonly SystemTasks.Task AwaitedTask;

            /// <summary>
            /// The task awaiter.
            /// </summary>
            private readonly SystemCompiler.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter Awaiter;

            /// <summary>
            /// Gets a value that indicates whether the controlled task has completed.
            /// </summary>
            public bool IsCompleted => AwaitedTask.IsCompleted;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConfiguredTaskAwaiter"/> struct.
            /// </summary>
            internal ConfiguredTaskAwaiter(TaskController taskController, SystemTasks.Task awaitedTask,
                bool continueOnCapturedContext)
            {
                TaskController = taskController;
                AwaitedTask = awaitedTask;
                Awaiter = awaitedTask.ConfigureAwait(continueOnCapturedContext).GetAwaiter();
            }

            /// <summary>
            /// Ends the await on the completed task.
            /// </summary>
            public void GetResult()
            {
                TaskController?.OnWaitTask(AwaitedTask);
                Awaiter.GetResult();
            }

            /// <summary>
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void OnCompleted(Action continuation)
            {
                if (TaskController is null)
                {
                    Awaiter.OnCompleted(continuation);
                }
                else
                {
                    TaskController.ScheduleTaskAwaiterContinuation(AwaitedTask, continuation);
                }
            }

            /// <summary>
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void UnsafeOnCompleted(Action continuation)
            {
                if (TaskController is null)
                {
                    Awaiter.UnsafeOnCompleted(continuation);
                }
                else
                {
                    TaskController.ScheduleTaskAwaiterContinuation(AwaitedTask, continuation);
                }
            }
        }
    }

    /// <summary>
    /// Provides an awaitable object that enables configured awaits on a <see cref="Task{TResult}"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct ConfiguredTaskAwaitable<TResult>
    {
        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private readonly TaskController TaskController;

        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly ConfiguredTaskAwaiter Awaiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredTaskAwaitable{TResult}"/> struct.
        /// </summary>
        internal ConfiguredTaskAwaitable(TaskController taskController, SystemTasks.Task<TResult> awaitedTask,
            bool continueOnCapturedContext)
        {
            TaskController = taskController;
            Awaiter = new ConfiguredTaskAwaiter(taskController, awaitedTask, continueOnCapturedContext);
        }

        /// <summary>
        /// Returns an awaiter for this awaitable object.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public ConfiguredTaskAwaiter GetAwaiter()
        {
            TaskController?.OnGetAwaiter();
            return Awaiter;
        }

        /// <summary>
        /// Provides an awaiter for an awaitable object. This type is intended for compiler use only.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        public struct ConfiguredTaskAwaiter : SystemCompiler.ICriticalNotifyCompletion, SystemCompiler.INotifyCompletion
        {
            /// <summary>
            /// Responsible for controlling the execution of tasks during systematic testing.
            /// </summary>
            private readonly TaskController TaskController;

            /// <summary>
            /// The task being awaited.
            /// </summary>
            private readonly SystemTasks.Task<TResult> AwaitedTask;

            /// <summary>
            /// The task awaiter.
            /// </summary>
            private readonly SystemCompiler.ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter Awaiter;

            /// <summary>
            /// Gets a value that indicates whether the controlled task has completed.
            /// </summary>
            public bool IsCompleted => AwaitedTask.IsCompleted;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConfiguredTaskAwaiter"/> struct.
            /// </summary>
            internal ConfiguredTaskAwaiter(TaskController taskController, SystemTasks.Task<TResult> awaitedTask,
                bool continueOnCapturedContext)
            {
                TaskController = taskController;
                AwaitedTask = awaitedTask;
                Awaiter = awaitedTask.ConfigureAwait(continueOnCapturedContext).GetAwaiter();
            }

            /// <summary>
            /// Ends the await on the completed task.
            /// </summary>
            public TResult GetResult()
            {
                TaskController?.OnWaitTask(AwaitedTask);
                return Awaiter.GetResult();
            }

            /// <summary>
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void OnCompleted(Action continuation)
            {
                if (TaskController is null)
                {
                    Awaiter.OnCompleted(continuation);
                }
                else
                {
                    TaskController.ScheduleTaskAwaiterContinuation(AwaitedTask, continuation);
                }
            }

            /// <summary>
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void UnsafeOnCompleted(Action continuation)
            {
                if (TaskController is null)
                {
                    Awaiter.UnsafeOnCompleted(continuation);
                }
                else
                {
                    TaskController.ScheduleTaskAwaiterContinuation(AwaitedTask, continuation);
                }
            }
        }
    }
}