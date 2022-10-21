// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;
using AsyncMethodBuilder = System.Runtime.CompilerServices.AsyncMethodBuilderAttribute;
using MethodImpl = System.Runtime.CompilerServices.MethodImplAttribute;
using MethodImplOptions = System.Runtime.CompilerServices.MethodImplOptions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Tasks
{
    /// <summary>
    /// Represents an asynchronous operation. Each <see cref="Task"/> is a thin wrapper over
    /// <see cref="SystemTasks.Task"/> and each call simply invokes the wrapped task. During
    /// testing, a <see cref="Task"/> is controlled by the runtime and systematically interleaved
    /// with other asynchronous operations to find bugs.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/learn/programming-models/async/overview">Programming model: asynchronous tasks</see> for more information.
    /// </remarks>
    [AsyncMethodBuilder(typeof(AsyncTaskMethodBuilder))]
    public class Task : IDisposable
    {
        /// <summary>
        /// A <see cref="Task"/> that has completed successfully.
        /// </summary>
        public static Task CompletedTask { get; } = new Task(null, SystemTasks.Task.CompletedTask);

        /// <summary>
        /// Returns the id of the currently executing <see cref="Task"/>.
        /// </summary>
        public static int? CurrentId => SystemTasks.Task.CurrentId;

        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private protected readonly TaskController TaskController;

        /// <summary>
        /// Internal task used to execute the work.
        /// </summary>
        private protected readonly SystemTasks.Task InternalTask;

        /// <summary>
        /// The id of this task.
        /// </summary>
        public int Id => this.InternalTask.Id;

        /// <summary>
        /// The uncontrolled <see cref="SystemTasks.Task"/> that is wrapped inside this
        /// controlled <see cref="Task"/>.
        /// </summary>
        public SystemTasks.Task UncontrolledTask => this.InternalTask;

        /// <summary>
        /// Value that indicates whether the task has completed.
        /// </summary>
        public bool IsCompleted => this.InternalTask.IsCompleted;

        /// <summary>
        /// Value that indicates whether the task completed execution due to being canceled.
        /// </summary>
        public bool IsCanceled => this.InternalTask.IsCanceled;

        /// <summary>
        /// Value that indicates whether the task completed due to an unhandled exception.
        /// </summary>
        public bool IsFaulted => this.InternalTask.IsFaulted;

        /// <summary>
        /// Gets the <see cref="AggregateException"/> that caused the task
        /// to end prematurely. If the task completed successfully or has not yet
        /// thrown any exceptions, this will return null.
        /// </summary>
        public AggregateException Exception => this.InternalTask.Exception;

        /// <summary>
        /// The status of this task.
        /// </summary>
        public SystemTasks.TaskStatus Status => this.InternalTask.Status;

        /// <summary>
        /// Initializes a new instance of the <see cref="Task"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal Task(TaskController taskController, SystemTasks.Task task)
        {
            this.TaskController = taskController;
            this.InternalTask = task ?? throw new ArgumentNullException(nameof(task));
        }

        /// <summary>
        /// Creates a <see cref="Task{TResult}"/> that is completed successfully with the specified result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="result">The result to store into the completed task.</param>
        /// <returns>The successfully completed task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> FromResult<TResult>(TResult result) =>
            new Task<TResult>(null, SystemTasks.Task.FromResult(result));

        /// <summary>
        /// Creates a <see cref="Task"/> that is completed due to
        /// cancellation with a specified cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token with which to complete the task.</param>
        /// <returns>The canceled task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task FromCanceled(CancellationToken cancellationToken) =>
            new Task(null, SystemTasks.Task.FromCanceled(cancellationToken));

        /// <summary>
        /// Creates a <see cref="Task{TResult}"/> that is completed due to
        /// cancellation with a specified cancellation token.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="cancellationToken">The cancellation token with which to complete the task.</param>
        /// <returns>The canceled task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> FromCanceled<TResult>(CancellationToken cancellationToken) =>
            new Task<TResult>(null, SystemTasks.Task.FromCanceled<TResult>(cancellationToken));

        /// <summary>
        /// Creates a <see cref="Task"/> that is completed with a specified exception.
        /// </summary>
        /// <param name="exception">The exception with which to complete the task.</param>
        /// <returns>The faulted task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task FromException(Exception exception) =>
            new Task(null, SystemTasks.Task.FromException(exception));

        /// <summary>
        /// Creates a <see cref="Task{TResult}"/> that is completed with a specified exception.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="exception">The exception with which to complete the task.</param>
        /// <returns>The faulted task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> FromException<TResult>(Exception exception) =>
            new Task<TResult>(null, SystemTasks.Task.FromException<TResult>(exception));

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="Task"/>
        /// object that represents that work. A cancellation token allows the work to be cancelled.
        /// </summary>
        /// <param name="action">The work to execute asynchronously.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Run(Action action) => Run(action, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="Task"/>
        /// object that represents that work.
        /// </summary>
        /// <param name="action">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Run(Action action, CancellationToken cancellationToken)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return ControlledRuntime.Current.TaskController.ScheduleAction(action, null, cancellationToken);
            }

            return new Task(null, SystemTasks.Task.Run(action, cancellationToken));
        }

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for
        /// the <see cref="Task"/> returned by the function.
        /// </summary>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Run(Func<Task> function) => Run(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for
        /// the <see cref="Task"/> returned by the function. A cancellation
        /// token allows the work to be cancelled.
        /// </summary>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Run(Func<Task> function, CancellationToken cancellationToken)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return ControlledRuntime.Current.TaskController.ScheduleFunction(function, null, cancellationToken);
            }

            return new Task(null, SystemTasks.Task.Run(async () => await function(), cancellationToken));
        }

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for the
        /// <see cref="Task{TResult}"/> returned by the function.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function) => Run(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for the
        /// <see cref="Task{TResult}"/> returned by the function. A cancellation
        /// token allows the work to be cancelled.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return ControlledRuntime.Current.TaskController.ScheduleFunction(function, null, cancellationToken);
            }

            return new Task<TResult>(null, SystemTasks.Task.Run(async () => await function(), cancellationToken));
        }

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="Task"/>
        /// object that represents that work.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> Run<TResult>(Func<TResult> function) => Run(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="Task"/>
        /// object that represents that work. A cancellation token allows the work to be cancelled.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> Run<TResult>(Func<TResult> function, CancellationToken cancellationToken)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return ControlledRuntime.Current.TaskController.ScheduleDelegate<TResult>(function, null, cancellationToken);
            }

            return new Task<TResult>(null, SystemTasks.Task.Run(function, cancellationToken));
        }

        /// <summary>
        /// Creates a <see cref="Task"/> that completes after a time delay.
        /// </summary>
        /// <param name="millisecondsDelay">
        /// The number of milliseconds to wait before completing the returned task, or -1 to wait indefinitely.
        /// </param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Delay(int millisecondsDelay) => Delay(millisecondsDelay, default);

        /// <summary>
        /// Creates a <see cref="Task"/> that completes after a time delay.
        /// </summary>
        /// <param name="millisecondsDelay">
        /// The number of milliseconds to wait before completing the returned task, or -1 to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the delay.</param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Delay(int millisecondsDelay, CancellationToken cancellationToken)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return ControlledRuntime.Current.TaskController.ScheduleDelay(TimeSpan.FromMilliseconds(millisecondsDelay), cancellationToken);
            }

            return new Task(null, SystemTasks.Task.Delay(millisecondsDelay, cancellationToken));
        }

        /// <summary>
        /// Creates a <see cref="Task"/> that completes after a specified time interval.
        /// </summary>
        /// <param name="delay">
        /// The time span to wait before completing the returned task, or TimeSpan.FromMilliseconds(-1)
        /// to wait indefinitely.
        /// </param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Delay(TimeSpan delay) => Delay(delay, default);

        /// <summary>
        /// Creates a <see cref="Task"/> that completes after a specified time interval.
        /// </summary>
        /// <param name="delay">
        /// The time span to wait before completing the returned task, or TimeSpan.FromMilliseconds(-1)
        /// to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the delay.</param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Delay(TimeSpan delay, CancellationToken cancellationToken)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return ControlledRuntime.Current.TaskController.ScheduleDelay(delay, cancellationToken);
            }

            return new Task(null, SystemTasks.Task.Delay(delay, cancellationToken));
        }

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WhenAll(params Task[] tasks) => WhenAllTasksCompleteAsync(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WhenAll(IEnumerable<Task> tasks) => WhenAllTasksCompleteAsync(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task WhenAllTasksCompleteAsync(IEnumerable<Task> tasks)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return ControlledRuntime.Current.TaskController.WhenAllTasksCompleteAsync(tasks);
            }

            return new Task(null, SystemTasks.Task.WhenAll(tasks.Select(t => t.InternalTask)));
        }

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks) => WhenAllTasksCompleteAsync(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks) => WhenAllTasksCompleteAsync(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task<TResult[]> WhenAllTasksCompleteAsync<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return ControlledRuntime.Current.TaskController.WhenAllTasksCompleteAsync(tasks);
            }

            return new Task<TResult[]>(null, SystemTasks.Task.WhenAll(tasks.Select(t => t.UncontrolledTask)));
        }

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task> WhenAny(params Task[] tasks) => WhenAnyTaskCompletesAsync(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task> WhenAny(IEnumerable<Task> tasks) => WhenAnyTaskCompletesAsync(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task<Task> WhenAnyTaskCompletesAsync(IEnumerable<Task> tasks)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return ControlledRuntime.Current.TaskController.WhenAnyTaskCompletesAsync(tasks);
            }

            return WhenAnyTaskCompletesInProductionAsync(tasks);
        }

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        private static async Task<Task> WhenAnyTaskCompletesInProductionAsync(IEnumerable<Task> tasks)
        {
            var result = await SystemTasks.Task.WhenAny(tasks.Select(t => t.UncontrolledTask));
            return tasks.First(task => task.Id == result.Id);
        }

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks) =>
            WhenAnyTaskCompletesAsync(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks) =>
            WhenAnyTaskCompletesAsync(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task<TResult>> WhenAnyTaskCompletesAsync<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return ControlledRuntime.Current.TaskController.WhenAnyTaskCompletesAsync(tasks);
            }

            return WhenAnyTaskCompletesInProductionAsync(tasks);
        }

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        public static async Task<Task<TResult>> WhenAnyTaskCompletesInProductionAsync<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            var result = await SystemTasks.Task.WhenAny(tasks.Select(t => t.UncontrolledTask));
            return tasks.First(task => task.Id == result.Id);
        }

        /// <summary>
        /// Waits for all of the provided <see cref="Task"/> objects to complete execution.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WaitAll(params Task[] tasks) => WaitAll(tasks, Timeout.Infinite, default);

        /// <summary>
        /// Waits for all of the provided <see cref="Task"/> objects to complete
        /// execution within a specified time interval.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <param name="timeout">
        /// A time span that represents the number of milliseconds to wait, or
        /// TimeSpan.FromMilliseconds(-1) to wait indefinitely.
        /// </param>
        /// <returns>True if all tasks completed execution within the allotted time; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool WaitAll(Task[] tasks, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return WaitAll(tasks, (int)totalMilliseconds, default);
        }

        /// <summary>
        /// Waits for all of the provided <see cref="Task"/> objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <returns>True if all tasks completed execution within the allotted time; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool WaitAll(Task[] tasks, int millisecondsTimeout) => WaitAll(tasks, millisecondsTimeout, default);

        /// <summary>
        /// Waits for all of the provided <see cref="Task"/> objects to complete
        /// execution unless the wait is cancelled.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the wait.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WaitAll(Task[] tasks, CancellationToken cancellationToken) =>
            WaitAll(tasks, Timeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete
        /// execution within a specified number of milliseconds or until a cancellation
        /// token is cancelled.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the wait.</param>
        /// <returns>True if all tasks completed execution within the allotted time; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool WaitAll(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return ControlledRuntime.Current.TaskController.WaitAllTasksComplete(tasks);
            }

            return SystemTasks.Task.WaitAll(tasks.Select(t => t.UncontrolledTask).ToArray(), millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete execution.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>The index of the completed task in the tasks array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(params Task[] tasks) => WaitAny(tasks, Timeout.Infinite, default);

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete
        /// execution within a specified time interval.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <param name="timeout">
        /// A time span that represents the number of milliseconds to wait, or
        /// TimeSpan.FromMilliseconds(-1) to wait indefinitely.
        /// </param>
        /// <returns>The index of the completed task in the tasks array, or -1 if the timeout occurred.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(Task[] tasks, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return WaitAny(tasks, (int)totalMilliseconds, default);
        }

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <returns>The index of the completed task in the tasks array, or -1 if the timeout occurred.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(Task[] tasks, int millisecondsTimeout) => WaitAny(tasks, millisecondsTimeout, default);

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete
        /// execution unless the wait is cancelled.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the wait.</param>
        /// <returns>The index of the completed task in the tasks array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(Task[] tasks, CancellationToken cancellationToken) => WaitAny(tasks, Timeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete
        /// execution within a specified number of milliseconds or until a cancellation
        /// token is cancelled.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the wait.</param>
        /// <returns>The index of the completed task in the tasks array, or -1 if the timeout occurred.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return ControlledRuntime.Current.TaskController.WaitAnyTaskCompletes(tasks);
            }

            return SystemTasks.Task.WaitAny(tasks.Select(t => t.UncontrolledTask).ToArray(), millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Creates an awaitable that asynchronously yields back to the current context when awaited.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static YieldAwaitable Yield()
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return new YieldAwaitable(ControlledRuntime.Current.TaskController);
            }

            return new YieldAwaitable(null);
        }

        /// <summary>
        /// Waits for the task to complete execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait() => this.Wait(Timeout.Infinite, default);

        /// <summary>
        /// Waits for the task to complete execution within a specified time interval.
        /// </summary>
        /// <param name="timeout">
        /// A time span that represents the number of milliseconds to wait, or
        /// TimeSpan.FromMilliseconds(-1) to wait indefinitely.
        /// </param>
        /// <returns>True if the task completed execution within the allotted time; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return this.Wait((int)totalMilliseconds, default);
        }

        /// <summary>
        /// Waits for the task to complete execution within a specified number of milliseconds.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <returns>True if the task completed execution within the allotted time; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(int millisecondsTimeout) => this.Wait(millisecondsTimeout, default);

        /// <summary>
        /// Waits for the task to complete execution. The wait terminates if
        /// a cancellation token is canceled before the task completes.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait(CancellationToken cancellationToken) => this.Wait(Timeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for the task to complete execution. The wait terminates if a timeout interval
        /// elapses or a cancellation token is canceled before the task completes.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>True if the task completed execution within the allotted time; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (this.TaskController is null)
            {
                return this.InternalTask.Wait(millisecondsTimeout, cancellationToken);
            }

            return this.TaskController.WaitTaskCompletes(this);
        }

        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TaskAwaiter GetAwaiter()
        {
            this.TaskController?.OnGetAwaiter();
            return new TaskAwaiter(this.TaskController, this.InternalTask);
        }

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        /// <param name="continueOnCapturedContext">
        /// True to attempt to marshal the continuation back to the original context captured; otherwise, false.
        /// </param>
        public ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext) =>
            new ConfiguredTaskAwaitable(this.TaskController, this.InternalTask, continueOnCapturedContext);

        /// <summary>
        /// Injects a context switch point that can be systematically explored during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExploreContextSwitch()
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                ControlledRuntime.Current.ScheduleNextOperation();
            }
        }

        /// <summary>
        /// Disposes the <see cref="Task"/>, releasing all of its unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Disposes the <see cref="Task"/>, releasing all of its unmanaged resources.
        /// </summary>
        /// <remarks>
        /// Unlike most of the members of <see cref="Task"/>, this method is not thread-safe.
        /// </remarks>
        public void Dispose()
        {
            this.InternalTask.Dispose();
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Represents an asynchronous operation that can return a value. Each <see cref="Task{TResult}"/> is a thin
    /// wrapper over <see cref="SystemTasks.Task{TResult}"/> and each call simply invokes the wrapped task. During
    /// testing, a <see cref="Task"/> is controlled by the runtime and systematically interleaved with other
    /// asynchronous operations to find bugs.
    /// </summary>
    /// <typeparam name="TResult">The type of the produced result.</typeparam>
    [AsyncMethodBuilder(typeof(AsyncTaskMethodBuilder<>))]
    public class Task<TResult> : Task
    {
        /// <summary>
        /// The uncontrolled <see cref="SystemTasks.Task{TResult}"/> that is wrapped inside this
        /// controlled <see cref="Task{TResult}"/>.
        /// </summary>
        internal new SystemTasks.Task<TResult> UncontrolledTask => this.InternalTask as SystemTasks.Task<TResult>;

        /// <summary>
        /// Gets the result value of this task.
        /// </summary>
        public TResult Result
        {
            get
            {
                {
                    if (this.TaskController is null)
                    {
                        return this.UncontrolledTask.Result;
                    }

                    return this.TaskController.WaitTaskCompletes(this);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Task{TResult}"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal Task(TaskController taskController, SystemTasks.Task<TResult> task)
            : base(taskController, task)
        {
        }

        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new TaskAwaiter<TResult> GetAwaiter()
        {
            this.TaskController?.OnGetAwaiter();
            return new TaskAwaiter<TResult>(this.TaskController, this.UncontrolledTask);
        }

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        /// <param name="continueOnCapturedContext">
        /// True to attempt to marshal the continuation back to the original context captured; otherwise, false.
        /// </param>
        public new ConfiguredTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) =>
            new ConfiguredTaskAwaitable<TResult>(this.TaskController, this.UncontrolledTask, continueOnCapturedContext);
    }
}
