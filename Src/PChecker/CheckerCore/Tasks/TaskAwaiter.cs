// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using PChecker.SystematicTesting;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemTasks = System.Threading.Tasks;

namespace PChecker.Tasks
{
    /// <summary>
    /// Implements a <see cref="Task"/> awaiter. This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct TaskAwaiter : SystemCompiler.ICriticalNotifyCompletion, SystemCompiler.INotifyCompletion
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
        public bool IsCompleted => AwaitedTask.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAwaiter"/> struct.
        /// </summary>
        [DebuggerStepThrough]
        internal TaskAwaiter(TaskController taskController, SystemTasks.Task awaitedTask)
        {
            TaskController = taskController;
            AwaitedTask = awaitedTask;
            Awaiter = awaitedTask.GetAwaiter();
        }

        /// <summary>
        /// Ends the wait for the completion of the controlled task.
        /// </summary>
        [DebuggerHidden]
        public void GetResult()
        {
            TaskController?.OnWaitTask(AwaitedTask);
            Awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the controlled task completes.
        /// </summary>
        [DebuggerHidden]
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
        /// Schedules the continuation action that is invoked when the controlled task completes.
        /// </summary>
        [DebuggerHidden]
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

    /// <summary>
    /// Implements a <see cref="Task"/> awaiter. This type is intended for compiler use only.
    /// </summary>
    /// <typeparam name="TResult">The type of the produced result.</typeparam>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct TaskAwaiter<TResult> : SystemCompiler.ICriticalNotifyCompletion, SystemCompiler.INotifyCompletion
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
        public bool IsCompleted => AwaitedTask.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAwaiter{TResult}"/> struct.
        /// </summary>
        [DebuggerStepThrough]
        internal TaskAwaiter(TaskController taskController, SystemTasks.Task<TResult> awaitedTask)
        {
            TaskController = taskController;
            AwaitedTask = awaitedTask;
            Awaiter = awaitedTask.GetAwaiter();
        }

        /// <summary>
        /// Ends the wait for the completion of the controlled task.
        /// </summary>
        [DebuggerHidden]
        public TResult GetResult()
        {
            TaskController?.OnWaitTask(AwaitedTask);
            return Awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the controlled task completes.
        /// </summary>
        [DebuggerHidden]
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
        /// Schedules the continuation action that is invoked when the controlled task completes.
        /// </summary>
        [DebuggerHidden]
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