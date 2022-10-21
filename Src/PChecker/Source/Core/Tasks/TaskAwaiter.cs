// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Coyote.SystematicTesting;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Tasks
{
    /// <summary>
    /// Implements a <see cref="Task"/> awaiter. This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly struct TaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
    {
        // WARNING: The layout must remain the same, as the struct is used to access
        // the generic TaskAwaiter<> as TaskAwaiter.

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
        private readonly SystemCompiler.TaskAwaiter Awaiter;

        /// <summary>
        /// Gets a value that indicates whether the controlled task has completed.
        /// </summary>
        public bool IsCompleted => this.AwaitedTask.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAwaiter"/> struct.
        /// </summary>
        [DebuggerStepThrough]
        internal TaskAwaiter(TaskController taskController, SystemTasks.Task awaitedTask)
        {
            this.TaskController = taskController;
            this.AwaitedTask = awaitedTask;
            this.Awaiter = awaitedTask.GetAwaiter();
        }

        /// <summary>
        /// Ends the wait for the completion of the controlled task.
        /// </summary>
        [DebuggerHidden]
        public void GetResult()
        {
            this.TaskController?.OnWaitTask(this.AwaitedTask);
            this.Awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the controlled task completes.
        /// </summary>
        [DebuggerHidden]
        public void OnCompleted(Action continuation)
        {
            if (this.TaskController is null)
            {
                this.Awaiter.OnCompleted(continuation);
            }
            else
            {
                this.TaskController.ScheduleTaskAwaiterContinuation(this.AwaitedTask, continuation);
            }
        }

        /// <summary>
        /// Schedules the continuation action that is invoked when the controlled task completes.
        /// </summary>
        [DebuggerHidden]
        public void UnsafeOnCompleted(Action continuation)
        {
            if (this.TaskController is null)
            {
                this.Awaiter.UnsafeOnCompleted(continuation);
            }
            else
            {
                this.TaskController.ScheduleTaskAwaiterContinuation(this.AwaitedTask, continuation);
            }
        }
    }

    /// <summary>
    /// Implements a <see cref="Task"/> awaiter. This type is intended for compiler use only.
    /// </summary>
    /// <typeparam name="TResult">The type of the produced result.</typeparam>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly struct TaskAwaiter<TResult> : ICriticalNotifyCompletion, INotifyCompletion
    {
        // WARNING: The layout must remain the same, as the struct is used to access
        // the generic TaskAwaiter<> as TaskAwaiter.

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
        private readonly SystemCompiler.TaskAwaiter<TResult> Awaiter;

        /// <summary>
        /// Gets a value that indicates whether the controlled task has completed.
        /// </summary>
        public bool IsCompleted => this.AwaitedTask.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAwaiter{TResult}"/> struct.
        /// </summary>
        [DebuggerStepThrough]
        internal TaskAwaiter(TaskController taskController, SystemTasks.Task<TResult> awaitedTask)
        {
            this.TaskController = taskController;
            this.AwaitedTask = awaitedTask;
            this.Awaiter = awaitedTask.GetAwaiter();
        }

        /// <summary>
        /// Ends the wait for the completion of the controlled task.
        /// </summary>
        [DebuggerHidden]
        public TResult GetResult()
        {
            this.TaskController?.OnWaitTask(this.AwaitedTask);
            return this.Awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the controlled task completes.
        /// </summary>
        [DebuggerHidden]
        public void OnCompleted(Action continuation)
        {
            if (this.TaskController is null)
            {
                this.Awaiter.OnCompleted(continuation);
            }
            else
            {
                this.TaskController.ScheduleTaskAwaiterContinuation(this.AwaitedTask, continuation);
            }
        }

        /// <summary>
        /// Schedules the continuation action that is invoked when the controlled task completes.
        /// </summary>
        [DebuggerHidden]
        public void UnsafeOnCompleted(Action continuation)
        {
            if (this.TaskController is null)
            {
                this.Awaiter.UnsafeOnCompleted(continuation);
            }
            else
            {
                this.TaskController.ScheduleTaskAwaiterContinuation(this.AwaitedTask, continuation);
            }
        }
    }
}
