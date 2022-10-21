// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
#if !DEBUG
using System.Diagnostics;
#endif
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using CoyoteTasks = Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Responsible for controlling the execution of tasks during systematic testing.
    /// </summary>
    internal sealed class TaskController
    {
        /// <summary>
        /// The executing runtime.
        /// </summary>
        private readonly ControlledRuntime Runtime;

        /// <summary>
        /// The asynchronous operation scheduler.
        /// </summary>
        private readonly OperationScheduler Scheduler;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskController"/> class.
        /// </summary>
        internal TaskController(ControlledRuntime runtime, OperationScheduler scheduler)
        {
            this.Runtime = runtime;
            this.Scheduler = scheduler;
        }

        /// <summary>
        /// Schedules the specified action to be executed asynchronously.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        public CoyoteTasks.Task ScheduleAction(Action action, Task predecessor, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            this.Assert(action != null, "The task cannot execute a null action.");

            ulong operationId = this.Runtime.GetNextOperationId();
            var op = new TaskOperation(operationId, this.Scheduler);
            this.Scheduler.RegisterOperation(op);
            op.OnEnabled();

            var task = new Task(() =>
            {
                try
                {
                    // Update the current asynchronous control flow with the current runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    CoyoteRuntime.AssignAsyncControlFlowRuntime(this.Runtime);

                    OperationScheduler.StartOperation(op);
                    if (predecessor != null)
                    {
                        op.OnWaitTask(predecessor);
                    }

                    action();
                }
                catch (Exception ex)
                {
                    // Report the unhandled exception unless it is our ExecutionCanceledException which is our
                    // way of terminating async task operations at the end of the test iteration.
                    if (!(ex is ExecutionCanceledException))
                    {
                        ReportUnhandledExceptionInOperation(op, ex);
                    }

                    // and rethrow it
                    throw;
                }
                finally
                {
                    IO.Debug.WriteLine("<ScheduleDebug> Completed operation '{0}' on task '{1}'.", op.Name, Task.CurrentId);
                    op.OnCompleted();
                }
            }, cancellationToken);

            // Schedule a task continuation that will schedule the next enabled operation upon completion.
            task.ContinueWith(t => this.Scheduler.ScheduleNextEnabledOperation(), TaskScheduler.Current);

            IO.Debug.WriteLine("<CreateLog> Operation '{0}' was created to execute task '{1}'.", op.Name, task.Id);
            this.Scheduler.ScheduleOperation(op, task.Id);
            task.Start();
            this.Scheduler.WaitOperationStart(op);
            this.Scheduler.ScheduleNextEnabledOperation();

            return new CoyoteTasks.Task(this, task);
        }

        /// <summary>
        /// Schedules the specified function to be executed asynchronously.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        public CoyoteTasks.Task ScheduleFunction(Func<CoyoteTasks.Task> function, Task predecessor, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            this.Assert(function != null, "The task cannot execute a null function.");

            ulong operationId = this.Runtime.GetNextOperationId();
            var op = new TaskOperation(operationId, this.Scheduler);
            this.Scheduler.RegisterOperation(op);
            op.OnEnabled();

            var task = new Task<Task>(() =>
            {
                try
                {
                    // Update the current asynchronous control flow with the current runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    CoyoteRuntime.AssignAsyncControlFlowRuntime(this.Runtime);

                    OperationScheduler.StartOperation(op);
                    if (predecessor != null)
                    {
                        op.OnWaitTask(predecessor);
                    }

                    CoyoteTasks.Task resultTask = function();
                    this.OnWaitTask(operationId, resultTask.UncontrolledTask);
                    return resultTask.UncontrolledTask;
                }
                catch (Exception ex)
                {
                    // Report the unhandled exception and rethrow it.
                    ReportUnhandledExceptionInOperation(op, ex);
                    throw;
                }
                finally
                {
                    IO.Debug.WriteLine("<ScheduleDebug> Completed operation '{0}' on task '{1}'.", op.Name, Task.CurrentId);
                    op.OnCompleted();
                }
            }, cancellationToken);

            Task innerTask = task.Unwrap();

            // Schedule a task continuation that will schedule the next enabled operation upon completion.
            innerTask.ContinueWith(t => this.Scheduler.ScheduleNextEnabledOperation(), TaskScheduler.Current);

            IO.Debug.WriteLine("<CreateLog> Operation '{0}' was created to execute task '{1}'.", op.Name, task.Id);
            this.Scheduler.ScheduleOperation(op, task.Id);
            task.Start();
            this.Scheduler.WaitOperationStart(op);
            this.Scheduler.ScheduleNextEnabledOperation();

            return new CoyoteTasks.Task(this, innerTask);
        }

        /// <summary>
        /// Schedules the specified function to be executed asynchronously.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        public CoyoteTasks.Task<TResult> ScheduleFunction<TResult>(Func<CoyoteTasks.Task<TResult>> function, Task predecessor,
            CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            this.Assert(function != null, "The task cannot execute a null function.");

            ulong operationId = this.Runtime.GetNextOperationId();
            var op = new TaskOperation(operationId, this.Scheduler);
            this.Scheduler.RegisterOperation(op);
            op.OnEnabled();

            var task = new Task<Task<TResult>>(() =>
            {
                try
                {
                    // Update the current asynchronous control flow with the current runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    CoyoteRuntime.AssignAsyncControlFlowRuntime(this.Runtime);

                    OperationScheduler.StartOperation(op);
                    if (predecessor != null)
                    {
                        op.OnWaitTask(predecessor);
                    }

                    CoyoteTasks.Task<TResult> resultTask = function();
                    this.OnWaitTask(operationId, resultTask.UncontrolledTask);
                    return resultTask.UncontrolledTask;
                }
                catch (Exception ex)
                {
                    // Report the unhandled exception and rethrow it.
                    ReportUnhandledExceptionInOperation(op, ex);
                    throw;
                }
                finally
                {
                    IO.Debug.WriteLine("<ScheduleDebug> Completed operation '{0}' on task '{1}'.", op.Name, Task.CurrentId);
                    op.OnCompleted();
                }
            }, cancellationToken);

            Task<TResult> innerTask = task.Unwrap();

            // Schedule a task continuation that will schedule the next enabled operation upon completion.
            innerTask.ContinueWith(t => this.Scheduler.ScheduleNextEnabledOperation(), TaskScheduler.Current);

            IO.Debug.WriteLine("<CreateLog> Operation '{0}' was created to execute task '{1}'.", op.Name, task.Id);
            this.Scheduler.ScheduleOperation(op, task.Id);
            task.Start();
            this.Scheduler.WaitOperationStart(op);
            this.Scheduler.ScheduleNextEnabledOperation();

            return new CoyoteTasks.Task<TResult>(this, innerTask);
        }

        /// <summary>
        /// Schedules the specified delegate to be executed asynchronously.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        public CoyoteTasks.Task<TResult> ScheduleDelegate<TResult>(Delegate work, Task predecessor, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            this.Assert(work != null, "The task cannot execute a null delegate.");

            ulong operationId = this.Runtime.GetNextOperationId();
            var op = new TaskOperation(operationId, this.Scheduler);
            this.Scheduler.RegisterOperation(op);
            op.OnEnabled();

            var task = new Task<TResult>(() =>
            {
                try
                {
                    // Update the current asynchronous control flow with the current runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    CoyoteRuntime.AssignAsyncControlFlowRuntime(this.Runtime);

                    OperationScheduler.StartOperation(op);
                    if (predecessor != null)
                    {
                        op.OnWaitTask(predecessor);
                    }

                    if (work is Func<Task> funcWithTaskResult)
                    {
                        Task resultTask = funcWithTaskResult();
                        this.OnWaitTask(operationId, resultTask);
                        if (resultTask is TResult typedResultTask)
                        {
                            return typedResultTask;
                        }
                    }
                    else if (work is Func<Task<TResult>> funcWithGenericTaskResult)
                    {
                        Task<TResult> resultTask = funcWithGenericTaskResult();
                        this.OnWaitTask(operationId, resultTask);
                        return resultTask.Result;
                    }
                    else if (work is Func<TResult> funcWithGenericResult)
                    {
                        return funcWithGenericResult();
                    }

                    return default;
                }
                catch (Exception ex)
                {
                    // Report the unhandled exception and rethrow it.
                    ReportUnhandledExceptionInOperation(op, ex);
                    throw;
                }
                finally
                {
                    IO.Debug.WriteLine("<ScheduleDebug> Completed operation '{0}' on task '{1}'.", op.Name, Task.CurrentId);
                    op.OnCompleted();
                }
            }, cancellationToken);

            // Schedule a task continuation that will schedule the next enabled operation upon completion.
            task.ContinueWith(t => this.Scheduler.ScheduleNextEnabledOperation(), TaskScheduler.Current);

            IO.Debug.WriteLine("<CreateLog> Operation '{0}' was created to execute task '{1}'.", op.Name, task.Id);
            this.Scheduler.ScheduleOperation(op, task.Id);
            task.Start();
            this.Scheduler.WaitOperationStart(op);
            this.Scheduler.ScheduleNextEnabledOperation();

            return new CoyoteTasks.Task<TResult>(this, task);
        }

        /// <summary>
        /// Schedules the specified delay to be executed asynchronously.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        public CoyoteTasks.Task ScheduleDelay(TimeSpan delay, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            if (delay.TotalMilliseconds == 0)
            {
                // If the delay is 0, then complete synchronously.
                return CoyoteTasks.Task.CompletedTask;
            }

            // TODO: cache the dummy delay action to optimize memory.
            return this.ScheduleAction(() => { }, null, cancellationToken);
        }

        /// <summary>
        /// Schedules the specified task awaiter continuation to be executed asynchronously.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        public void ScheduleTaskAwaiterContinuation(Task task, Action continuation)
        {
            try
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                this.Assert(callerOp != null,
                    "Task with id '{0}' that is not controlled by the runtime is executing controlled task '{1}'.",
                    Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>", task.Id);

                if (callerOp.IsExecutingInRootAsyncMethod())
                {
                    IO.Debug.WriteLine("<Task> '{0}' is executing continuation of task '{1}' on task '{2}'.",
                        callerOp.Name, task.Id, Task.CurrentId);
                    continuation();
                    IO.Debug.WriteLine("<Task> '{0}' resumed after continuation of task '{1}' on task '{2}'.",
                        callerOp.Name, task.Id, Task.CurrentId);
                }
                else
                {
                    IO.Debug.WriteLine("<Task> '{0}' is dispatching continuation of task '{1}'.", callerOp.Name, task.Id);
                    this.ScheduleAction(continuation, task, default);
                    IO.Debug.WriteLine("<Task> '{0}' dispatched continuation of task '{1}'.", callerOp.Name, task.Id);
                }
            }
            catch (ExecutionCanceledException)
            {
                IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from task '{Task.CurrentId}'.");
            }
        }

        /// <summary>
        /// Schedules the specified yield awaiter continuation to be executed asynchronously.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        public void ScheduleYieldAwaiterContinuation(Action continuation)
        {
            try
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                this.Assert(callerOp != null,
                    "Uncontrolled task '{0}' invoked a yield operation.",
                    Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
                IO.Debug.WriteLine("<Task> '{0}' is executing a yield operation.", callerOp.Id);
                this.ScheduleAction(continuation, null, default);
            }
            catch (ExecutionCanceledException)
            {
                IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from task '{Task.CurrentId}'.");
            }
        }

        /// <summary>
        /// Creates a controlled task that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        public CoyoteTasks.Task WhenAllTasksCompleteAsync(IEnumerable<CoyoteTasks.Task> tasks)
        {
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task '{0}' invoked a when-all operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: true);

            List<Exception> exceptions = null;
            foreach (var task in tasks)
            {
                if (task.IsFaulted)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(task.Exception);
                }
            }

            if (exceptions != null)
            {
                return CoyoteTasks.Task.FromException(new AggregateException(exceptions));
            }
            else
            {
                return CoyoteTasks.Task.CompletedTask;
            }
        }

        /// <summary>
        /// Creates a controlled task that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        public CoyoteTasks.Task<TResult[]> WhenAllTasksCompleteAsync<TResult>(IEnumerable<CoyoteTasks.Task<TResult>> tasks)
        {
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task '{0}' invoked a when-all operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: true);

            int idx = 0;
            TResult[] result = new TResult[tasks.Count()];
            foreach (var task in tasks)
            {
                result[idx] = task.Result;
                idx++;
            }

            return CoyoteTasks.Task.FromResult(result);
        }

        /// <summary>
        /// Creates a controlled task that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        public CoyoteTasks.Task<CoyoteTasks.Task> WhenAnyTaskCompletesAsync(IEnumerable<CoyoteTasks.Task> tasks)
        {
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task '{0}' invoked a when-any operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: false);

            CoyoteTasks.Task result = null;
            foreach (var task in tasks)
            {
                if (task.IsCompleted)
                {
                    result = task;
                    break;
                }
            }

            return CoyoteTasks.Task.FromResult(result);
        }

        /// <summary>
        /// Creates a controlled task that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        public CoyoteTasks.Task<CoyoteTasks.Task<TResult>> WhenAnyTaskCompletesAsync<TResult>(IEnumerable<CoyoteTasks.Task<TResult>> tasks)
        {
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task '{0}' invoked a when-any operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: false);

            CoyoteTasks.Task<TResult> result = null;
            foreach (var task in tasks)
            {
                if (task.IsCompleted)
                {
                    result = task;
                    break;
                }
            }

            return CoyoteTasks.Task.FromResult(result);
        }

        /// <summary>
        /// Waits for all of the provided controlled task objects to complete execution within
        /// a specified number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
        public bool WaitAllTasksComplete(CoyoteTasks.Task[] tasks)
        {
            // TODO: support cancellations during testing.
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task '{0}' invoked a wait-all operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: true);

            // TODO: support timeouts during testing, this would become false if there is a timeout.
            return true;
        }

        /// <summary>
        /// Waits for any of the provided controlled task objects to complete execution within
        /// a specified number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        public int WaitAnyTaskCompletes(CoyoteTasks.Task[] tasks)
        {
            // TODO: support cancellations during testing.
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task '{0}' invoked a wait-any operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: false);

            int result = -1;
            for (int i = 0; i < tasks.Length; i++)
            {
                if (tasks[i].IsCompleted)
                {
                    result = i;
                    break;
                }
            }

            // TODO: support timeouts during testing, this would become false if there is a timeout.
            return result;
        }

        /// <summary>
        /// Waits for the task to complete execution. The wait terminates if a timeout interval
        /// elapses or a cancellation token is canceled before the task completes.
        /// </summary>
        public bool WaitTaskCompletes(CoyoteTasks.Task task)
        {
            // TODO: return immediately if completed without errors.
            // TODO: support timeouts and cancellation tokens.
            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            IO.Debug.WriteLine("<Task> '{0}' is waiting task '{1}' to complete from task '{2}'.",
                callerOp.Name, task.Id, Task.CurrentId);
            callerOp.OnWaitTask(task.UncontrolledTask);
            return true;
        }

        /// <summary>
        /// Waits for the task to complete execution and returns the result.
        /// </summary>
        public TResult WaitTaskCompletes<TResult>(CoyoteTasks.Task<TResult> task)
        {
            // TODO: return immediately if completed without errors.
            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            IO.Debug.WriteLine("<Task> '{0}' is waiting task '{1}' with result type '{2}' to complete from task '{3}'.",
                callerOp.Name, task.Id, typeof(TResult), Task.CurrentId);
            callerOp.OnWaitTask(task.UncontrolledTask);
            return task.UncontrolledTask.Result;
        }

        /// <summary>
        /// Callback invoked when the <see cref="CoyoteTasks.AsyncTaskMethodBuilder.Start"/> is called.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        public void OnAsyncTaskMethodBuilderStart(Type stateMachineType)
        {
            try
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                callerOp.SetRootAsyncTaskStateMachine(stateMachineType);
            }
            catch (RuntimeException ex)
            {
                this.Assert(false, ex.Message);
            }
        }

        /// <summary>
        /// Callback invoked when the <see cref="CoyoteTasks.AsyncTaskMethodBuilder.Task"/> is accessed.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        public void OnAsyncTaskMethodBuilderTask()
        {
            if (!this.Scheduler.IsRunning)
            {
                throw new ExecutionCanceledException();
            }

            this.Scheduler.CheckNoExternalConcurrencyUsed();
        }

        /// <summary>
        /// Callback invoked when the <see cref="CoyoteTasks.AsyncTaskMethodBuilder.AwaitOnCompleted"/>
        /// or <see cref="CoyoteTasks.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted"/> is called.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        public void OnAsyncTaskMethodBuilderAwaitCompleted(Type awaiterType, Type stateMachineType)
        {
            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            if (!callerOp.IsAwaiterControlled)
            {
                this.Assert(false, "Controlled task '{0}' is trying to wait for an uncontrolled " +
                    "task or awaiter to complete. Please make sure to use Coyote APIs to express concurrency " +
                    "(e.g. Microsoft.Coyote.Tasks.Task instead of System.Threading.Tasks.Task).",
                    Task.CurrentId);
            }

            bool sameNamespace = awaiterType.Namespace == typeof(CoyoteTasks.Task).Namespace;
            if (!sameNamespace)
            {
                this.Assert(false,
                    "Controlled task '{0}' is trying to wait for an uncontrolled task or awaiter to complete. " +
                    "Please make sure to use Coyote APIs to express concurrency (e.g. Microsoft.Coyote.Tasks.Task " +
                    "instead of System.Threading.Tasks.Task).",
                    Task.CurrentId);
            }

            callerOp.SetExecutingAsyncTaskStateMachineType(stateMachineType);
        }

        /// <summary>
        /// Callback invoked when the currently executing task operation gets a controlled awaiter.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        public void OnGetAwaiter()
        {
            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            callerOp.OnGetAwaiter();
        }

        /// <summary>
        /// Callback invoked when the <see cref="CoyoteTasks.YieldAwaitable.YieldAwaiter.GetResult"/> is called.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        public void OnYieldAwaiterGetResult()
        {
            this.Scheduler.ScheduleNextEnabledOperation();
        }

        /// <summary>
        /// Callback invoked when the executing operation is waiting for the specified task to complete.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        public void OnWaitTask(Task task)
        {
            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            callerOp.OnWaitTask(task);
        }

        /// <summary>
        /// Callback invoked when the executing task is waiting for the task with the specified operation id to complete.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal void OnWaitTask(ulong operationId, Task task)
        {
            this.Assert(task != null, "Task '{0}' is waiting for a null task to complete.", Task.CurrentId);
            if (!task.IsCompleted)
            {
                var op = this.Scheduler.GetOperationWithId<TaskOperation>(operationId);
                op.OnWaitTask(task);
            }
        }

        /// <summary>
        /// Reports an unhandled exception in the specified asynchronous operation.
        /// </summary>
        private static void ReportUnhandledExceptionInOperation(AsyncOperation op, Exception ex)
        {
            string message = string.Format(CultureInfo.InvariantCulture,
                $"Exception '{ex.GetType()}' was thrown in operation '{op.Name}', " +
                $"'{ex.Source}':\n" +
                $"   {ex.Message}\n" +
                $"The stack trace is:\n{ex.StackTrace}");
            IO.Debug.WriteLine($"<Exception> {message}");
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, triggers a failure.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        private void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                this.Scheduler.NotifyAssertionFailure(string.Format(CultureInfo.InvariantCulture, s, args));
            }
        }
    }
}
