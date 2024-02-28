// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PChecker.Exceptions;
using PChecker.IO.Debugging;
using PChecker.Runtime;
using PChecker.SystematicTesting.Operations;
using Task = PChecker.Tasks.Task;

namespace PChecker.SystematicTesting
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
            Runtime = runtime;
            Scheduler = scheduler;
        }

        /// <summary>
        /// Schedules the specified action to be executed asynchronously.
        /// </summary>
        public Task ScheduleAction(Action action, System.Threading.Tasks.Task predecessor, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            Assert(action != null, "The task cannot execute a null action.");

            var operationId = Runtime.GetNextOperationId();
            var op = new TaskOperation(operationId, Scheduler);
            Scheduler.RegisterOperation(op);
            op.OnEnabled();

            var task = new System.Threading.Tasks.Task(() =>
            {
                try
                {
                    // Update the current asynchronous control flow with the current runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    CoyoteRuntime.AssignAsyncControlFlowRuntime(Runtime);

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
                    // way of terminating async task operations at the end of the test schedule.
                    if (!(ex is ExecutionCanceledException))
                    {
                        ReportUnhandledExceptionInOperation(op, ex);
                    }

                    // and rethrow it
                    throw;
                }
                finally
                {
                    Debug.WriteLine("<ScheduleDebug> Completed operation '{0}' on task '{1}'.", op.Name, System.Threading.Tasks.Task.CurrentId);
                    op.OnCompleted();
                }
            }, cancellationToken);

            // Schedule a task continuation that will schedule the next enabled operation upon completion.
            task.ContinueWith(t => Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Stop), TaskScheduler.Current);

            Debug.WriteLine("<CreateLog> Operation '{0}' was created to execute task '{1}'.", op.Name, task.Id);
            Scheduler.ScheduleOperation(op, task.Id);
            task.Start();
            Scheduler.WaitOperationStart(op);
            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Create);

            return new Task(this, task);
        }

        /// <summary>
        /// Schedules the specified function to be executed asynchronously.
        /// </summary>
        public Task ScheduleFunction(Func<Task> function, System.Threading.Tasks.Task predecessor, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            Assert(function != null, "The task cannot execute a null function.");

            var operationId = Runtime.GetNextOperationId();
            var op = new TaskOperation(operationId, Scheduler);
            Scheduler.RegisterOperation(op);
            op.OnEnabled();

            var task = new Task<System.Threading.Tasks.Task>(() =>
            {
                try
                {
                    // Update the current asynchronous control flow with the current runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    CoyoteRuntime.AssignAsyncControlFlowRuntime(Runtime);

                    OperationScheduler.StartOperation(op);
                    if (predecessor != null)
                    {
                        op.OnWaitTask(predecessor);
                    }

                    var resultTask = function();
                    OnWaitTask(operationId, resultTask.UncontrolledTask);
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
                    Debug.WriteLine("<ScheduleDebug> Completed operation '{0}' on task '{1}'.", op.Name, System.Threading.Tasks.Task.CurrentId);
                    op.OnCompleted();
                }
            }, cancellationToken);

            var innerTask = task.Unwrap();

            // Schedule a task continuation that will schedule the next enabled operation upon completion.
            innerTask.ContinueWith(t => Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Stop), TaskScheduler.Current);

            Debug.WriteLine("<CreateLog> Operation '{0}' was created to execute task '{1}'.", op.Name, task.Id);
            Scheduler.ScheduleOperation(op, task.Id);
            task.Start();
            Scheduler.WaitOperationStart(op);
            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Create);

            return new Task(this, innerTask);
        }

        /// <summary>
        /// Schedules the specified function to be executed asynchronously.
        /// </summary>
        public Tasks.Task<TResult> ScheduleFunction<TResult>(Func<Tasks.Task<TResult>> function, System.Threading.Tasks.Task predecessor,
            CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            Assert(function != null, "The task cannot execute a null function.");

            var operationId = Runtime.GetNextOperationId();
            var op = new TaskOperation(operationId, Scheduler);
            Scheduler.RegisterOperation(op);
            op.OnEnabled();

            var task = new Task<Task<TResult>>(() =>
            {
                try
                {
                    // Update the current asynchronous control flow with the current runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    CoyoteRuntime.AssignAsyncControlFlowRuntime(Runtime);

                    OperationScheduler.StartOperation(op);
                    if (predecessor != null)
                    {
                        op.OnWaitTask(predecessor);
                    }

                    var resultTask = function();
                    OnWaitTask(operationId, resultTask.UncontrolledTask);
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
                    Debug.WriteLine("<ScheduleDebug> Completed operation '{0}' on task '{1}'.", op.Name, System.Threading.Tasks.Task.CurrentId);
                    op.OnCompleted();
                }
            }, cancellationToken);

            var innerTask = task.Unwrap();

            // Schedule a task continuation that will schedule the next enabled operation upon completion.
            innerTask.ContinueWith(t => Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Stop), TaskScheduler.Current);

            Debug.WriteLine("<CreateLog> Operation '{0}' was created to execute task '{1}'.", op.Name, task.Id);
            Scheduler.ScheduleOperation(op, task.Id);
            task.Start();
            Scheduler.WaitOperationStart(op);
            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Create);

            return new Tasks.Task<TResult>(this, innerTask);
        }

        /// <summary>
        /// Schedules the specified delegate to be executed asynchronously.
        /// </summary>
        public Tasks.Task<TResult> ScheduleDelegate<TResult>(Delegate work, System.Threading.Tasks.Task predecessor, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            Assert(work != null, "The task cannot execute a null delegate.");

            var operationId = Runtime.GetNextOperationId();
            var op = new TaskOperation(operationId, Scheduler);
            Scheduler.RegisterOperation(op);
            op.OnEnabled();

            var task = new Task<TResult>(() =>
            {
                try
                {
                    // Update the current asynchronous control flow with the current runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    CoyoteRuntime.AssignAsyncControlFlowRuntime(Runtime);

                    OperationScheduler.StartOperation(op);
                    if (predecessor != null)
                    {
                        op.OnWaitTask(predecessor);
                    }

                    if (work is Func<System.Threading.Tasks.Task> funcWithTaskResult)
                    {
                        var resultTask = funcWithTaskResult();
                        OnWaitTask(operationId, resultTask);
                        if (resultTask is TResult typedResultTask)
                        {
                            return typedResultTask;
                        }
                    }
                    else if (work is Func<Task<TResult>> funcWithGenericTaskResult)
                    {
                        var resultTask = funcWithGenericTaskResult();
                        OnWaitTask(operationId, resultTask);
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
                    Debug.WriteLine("<ScheduleDebug> Completed operation '{0}' on task '{1}'.", op.Name, System.Threading.Tasks.Task.CurrentId);
                    op.OnCompleted();
                }
            }, cancellationToken);

            // Schedule a task continuation that will schedule the next enabled operation upon completion.
            task.ContinueWith(t => Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Stop), TaskScheduler.Current);

            Debug.WriteLine("<CreateLog> Operation '{0}' was created to execute task '{1}'.", op.Name, task.Id);
            Scheduler.ScheduleOperation(op, task.Id);
            task.Start();
            Scheduler.WaitOperationStart(op);
            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Create);

            return new Tasks.Task<TResult>(this, task);
        }

        /// <summary>
        /// Schedules the specified delay to be executed asynchronously.
        /// </summary>
        public Task ScheduleDelay(TimeSpan delay, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            if (delay.TotalMilliseconds == 0)
            {
                // If the delay is 0, then complete synchronously.
                return Task.CompletedTask;
            }

            // TODO: cache the dummy delay action to optimize memory.
            return ScheduleAction(() => { }, null, cancellationToken);
        }

        /// <summary>
        /// Schedules the specified task awaiter continuation to be executed asynchronously.
        /// </summary>
        public void ScheduleTaskAwaiterContinuation(System.Threading.Tasks.Task task, Action continuation)
        {
            try
            {
                var callerOp = Scheduler.GetExecutingOperation<TaskOperation>();
                Assert(callerOp != null,
                    "Task with id '{0}' that is not controlled by the runtime is executing controlled task '{1}'.",
                    System.Threading.Tasks.Task.CurrentId.HasValue ? System.Threading.Tasks.Task.CurrentId.Value.ToString() : "<unknown>", task.Id);

                if (callerOp.IsExecutingInRootAsyncMethod())
                {
                    Debug.WriteLine("<Task> '{0}' is executing continuation of task '{1}' on task '{2}'.",
                        callerOp.Name, task.Id, System.Threading.Tasks.Task.CurrentId);
                    continuation();
                    Debug.WriteLine("<Task> '{0}' resumed after continuation of task '{1}' on task '{2}'.",
                        callerOp.Name, task.Id, System.Threading.Tasks.Task.CurrentId);
                }
                else
                {
                    Debug.WriteLine("<Task> '{0}' is dispatching continuation of task '{1}'.", callerOp.Name, task.Id);
                    ScheduleAction(continuation, task, default);
                    Debug.WriteLine("<Task> '{0}' dispatched continuation of task '{1}'.", callerOp.Name, task.Id);
                }
            }
            catch (ExecutionCanceledException)
            {
                Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from task '{System.Threading.Tasks.Task.CurrentId}'.");
            }
        }

        /// <summary>
        /// Schedules the specified yield awaiter continuation to be executed asynchronously.
        /// </summary>
        public void ScheduleYieldAwaiterContinuation(Action continuation)
        {
            try
            {
                var callerOp = Scheduler.GetExecutingOperation<TaskOperation>();
                Assert(callerOp != null,
                    "Uncontrolled task '{0}' invoked a yield operation.",
                    System.Threading.Tasks.Task.CurrentId.HasValue ? System.Threading.Tasks.Task.CurrentId.Value.ToString() : "<unknown>");
                Debug.WriteLine("<Task> '{0}' is executing a yield operation.", callerOp.Id);
                ScheduleAction(continuation, null, default);
            }
            catch (ExecutionCanceledException)
            {
                Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from task '{System.Threading.Tasks.Task.CurrentId}'.");
            }
        }

        /// <summary>
        /// Creates a controlled task that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        public Task WhenAllTasksCompleteAsync(IEnumerable<Task> tasks)
        {
            Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = Scheduler.GetExecutingOperation<TaskOperation>();
            Assert(callerOp != null,
                "Uncontrolled task '{0}' invoked a when-all operation.",
                System.Threading.Tasks.Task.CurrentId.HasValue ? System.Threading.Tasks.Task.CurrentId.Value.ToString() : "<unknown>");
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
                return Task.FromException(new AggregateException(exceptions));
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Creates a controlled task that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        public Tasks.Task<TResult[]> WhenAllTasksCompleteAsync<TResult>(IEnumerable<Tasks.Task<TResult>> tasks)
        {
            Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = Scheduler.GetExecutingOperation<TaskOperation>();
            Assert(callerOp != null,
                "Uncontrolled task '{0}' invoked a when-all operation.",
                System.Threading.Tasks.Task.CurrentId.HasValue ? System.Threading.Tasks.Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: true);

            var idx = 0;
            var result = new TResult[tasks.Count()];
            foreach (var task in tasks)
            {
                result[idx] = task.Result;
                idx++;
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Creates a controlled task that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        public Tasks.Task<Task> WhenAnyTaskCompletesAsync(IEnumerable<Task> tasks)
        {
            Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = Scheduler.GetExecutingOperation<TaskOperation>();
            Assert(callerOp != null,
                "Uncontrolled task '{0}' invoked a when-any operation.",
                System.Threading.Tasks.Task.CurrentId.HasValue ? System.Threading.Tasks.Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: false);

            Task result = null;
            foreach (var task in tasks)
            {
                if (task.IsCompleted)
                {
                    result = task;
                    break;
                }
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Creates a controlled task that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        public Tasks.Task<Tasks.Task<TResult>> WhenAnyTaskCompletesAsync<TResult>(IEnumerable<Tasks.Task<TResult>> tasks)
        {
            Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = Scheduler.GetExecutingOperation<TaskOperation>();
            Assert(callerOp != null,
                "Uncontrolled task '{0}' invoked a when-any operation.",
                System.Threading.Tasks.Task.CurrentId.HasValue ? System.Threading.Tasks.Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: false);

            Tasks.Task<TResult> result = null;
            foreach (var task in tasks)
            {
                if (task.IsCompleted)
                {
                    result = task;
                    break;
                }
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Waits for all of the provided controlled task objects to complete execution within
        /// a specified number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
        public bool WaitAllTasksComplete(Task[] tasks)
        {
            // TODO: support cancellations during testing.
            Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = Scheduler.GetExecutingOperation<TaskOperation>();
            Assert(callerOp != null,
                "Uncontrolled task '{0}' invoked a wait-all operation.",
                System.Threading.Tasks.Task.CurrentId.HasValue ? System.Threading.Tasks.Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: true);

            // TODO: support timeouts during testing, this would become false if there is a timeout.
            return true;
        }

        /// <summary>
        /// Waits for any of the provided controlled task objects to complete execution within
        /// a specified number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
        public int WaitAnyTaskCompletes(Task[] tasks)
        {
            // TODO: support cancellations during testing.
            Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = Scheduler.GetExecutingOperation<TaskOperation>();
            Assert(callerOp != null,
                "Uncontrolled task '{0}' invoked a wait-any operation.",
                System.Threading.Tasks.Task.CurrentId.HasValue ? System.Threading.Tasks.Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: false);

            var result = -1;
            for (var i = 0; i < tasks.Length; i++)
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
        public bool WaitTaskCompletes(Task task)
        {
            // TODO: return immediately if completed without errors.
            // TODO: support timeouts and cancellation tokens.
            var callerOp = Scheduler.GetExecutingOperation<TaskOperation>();
            Debug.WriteLine("<Task> '{0}' is waiting task '{1}' to complete from task '{2}'.",
                callerOp.Name, task.Id, System.Threading.Tasks.Task.CurrentId);
            callerOp.OnWaitTask(task.UncontrolledTask);
            return true;
        }

        /// <summary>
        /// Waits for the task to complete execution and returns the result.
        /// </summary>
        public TResult WaitTaskCompletes<TResult>(Tasks.Task<TResult> task)
        {
            // TODO: return immediately if completed without errors.
            var callerOp = Scheduler.GetExecutingOperation<TaskOperation>();
            Debug.WriteLine("<Task> '{0}' is waiting task '{1}' with result type '{2}' to complete from task '{3}'.",
                callerOp.Name, task.Id, typeof(TResult), System.Threading.Tasks.Task.CurrentId);
            callerOp.OnWaitTask(task.UncontrolledTask);
            return task.UncontrolledTask.Result;
        }

        /// <summary>
        /// Callback invoked when the <see cref="Tasks.AsyncTaskMethodBuilder.Start"/> is called.
        /// </summary>
        public void OnAsyncTaskMethodBuilderStart(Type stateMachineType)
        {
            try
            {
                var callerOp = Scheduler.GetExecutingOperation<TaskOperation>();
                callerOp.SetRootAsyncTaskStateMachine(stateMachineType);
            }
            catch (RuntimeException ex)
            {
                Assert(false, ex.Message);
            }
        }

        /// <summary>
        /// Callback invoked when the <see cref="Tasks.AsyncTaskMethodBuilder.Task"/> is accessed.
        /// </summary>
        public void OnAsyncTaskMethodBuilderTask()
        {
            if (!Scheduler.IsRunning)
            {
                throw new ExecutionCanceledException();
            }

            Scheduler.CheckNoExternalConcurrencyUsed();
        }

        /// <summary>
        /// Callback invoked when the <see cref="Tasks.AsyncTaskMethodBuilder.AwaitOnCompleted"/>
        /// or <see cref="Tasks.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted"/> is called.
        /// </summary>
        public void OnAsyncTaskMethodBuilderAwaitCompleted(Type awaiterType, Type stateMachineType)
        {
            var callerOp = Scheduler.GetExecutingOperation<TaskOperation>();
            if (!callerOp.IsAwaiterControlled)
            {
                Assert(false, "Controlled task '{0}' is trying to wait for an uncontrolled " +
                              "task or awaiter to complete. Please make sure to use Coyote APIs to express concurrency " +
                              "(e.g. Microsoft.Coyote.Tasks.Task instead of System.Threading.Tasks.Task).",
                    System.Threading.Tasks.Task.CurrentId);
            }

            var sameNamespace = awaiterType.Namespace == typeof(Task).Namespace;
            if (!sameNamespace)
            {
                Assert(false,
                    "Controlled task '{0}' is trying to wait for an uncontrolled task or awaiter to complete. " +
                    "Please make sure to use Coyote APIs to express concurrency (e.g. Microsoft.Coyote.Tasks.Task " +
                    "instead of System.Threading.Tasks.Task).",
                    System.Threading.Tasks.Task.CurrentId);
            }

            callerOp.SetExecutingAsyncTaskStateMachineType(stateMachineType);
        }

        /// <summary>
        /// Callback invoked when the currently executing task operation gets a controlled awaiter.
        /// </summary>
        public void OnGetAwaiter()
        {
            var callerOp = Scheduler.GetExecutingOperation<TaskOperation>();
            callerOp.OnGetAwaiter();
        }

        /// <summary>
        /// Callback invoked when the <see cref="Tasks.YieldAwaitable.YieldAwaiter.GetResult"/> is called.
        /// </summary>
        public void OnYieldAwaiterGetResult()
        {
            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Yield);
        }

        /// <summary>
        /// Callback invoked when the executing operation is waiting for the specified task to complete.
        /// </summary>
        public void OnWaitTask(System.Threading.Tasks.Task task)
        {
            var callerOp = Scheduler.GetExecutingOperation<TaskOperation>();
            callerOp.OnWaitTask(task);
        }

        /// <summary>
        /// Callback invoked when the executing task is waiting for the task with the specified operation id to complete.
        /// </summary>
        internal void OnWaitTask(ulong operationId, System.Threading.Tasks.Task task)
        {
            Assert(task != null, "Task '{0}' is waiting for a null task to complete.", System.Threading.Tasks.Task.CurrentId);
            if (!task.IsCompleted)
            {
                var op = Scheduler.GetOperationWithId<TaskOperation>(operationId);
                op.OnWaitTask(task);
            }
        }

        /// <summary>
        /// Reports an unhandled exception in the specified asynchronous operation.
        /// </summary>
        private static void ReportUnhandledExceptionInOperation(AsyncOperation op, Exception ex)
        {
            var message = string.Format(CultureInfo.InvariantCulture,
                $"Exception '{ex.GetType()}' was thrown in operation '{op.Name}', " +
                $"'{ex.Source}':\n" +
                $"   {ex.Message}\n" +
                $"The stack trace is:\n{ex.StackTrace}");
            Debug.WriteLine($"<Exception> {message}");
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, triggers a failure.
        /// </summary>
        private void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                Scheduler.NotifyAssertionFailure(string.Format(CultureInfo.InvariantCulture, s, args));
            }
        }
    }
}