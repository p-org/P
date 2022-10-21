// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;
using SystemCompiler = System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Tasks
{
    /// <summary>
    /// Represents a builder for asynchronous methods that return a <see cref="Task"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [StructLayout(LayoutKind.Auto)]
    public struct AsyncTaskMethodBuilder
    {
        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private readonly TaskController TaskController;

        /// <summary>
        /// The task builder to which most operations are delegated.
        /// </summary>
#pragma warning disable IDE0044 // Add readonly modifier
        private SystemCompiler.AsyncTaskMethodBuilder MethodBuilder;
#pragma warning restore IDE0044 // Add readonly modifier

        /// <summary>
        /// True, if completed synchronously and successfully, else false.
        /// </summary>
        private bool IsCompleted;

        /// <summary>
        /// True, if the builder should be used for setting/getting the result, else false.
        /// </summary>
        private bool UseBuilder;

        /// <summary>
        /// Gets the task for this builder.
        /// </summary>
        public Task Task
        {
            [DebuggerHidden]
            get
            {
                if (this.IsCompleted)
                {
                    IO.Debug.WriteLine("<AsyncBuilder> Creating completed builder task '{0}' (isCompleted {1}) from task '{2}'.",
                        this.MethodBuilder.Task.Id, this.MethodBuilder.Task.IsCompleted, Task.CurrentId);
                    return Task.CompletedTask;
                }
                else
                {
                    IO.Debug.WriteLine("<AsyncBuilder> Creating builder task '{0}' (isCompleted {1}) from task '{2}'.",
                        this.MethodBuilder.Task.Id, this.MethodBuilder.Task.IsCompleted, Task.CurrentId);
                    this.UseBuilder = true;
                    this.TaskController?.OnAsyncTaskMethodBuilderTask();
                    return new Task(this.TaskController, this.MethodBuilder.Task);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncTaskMethodBuilder"/> struct.
        /// </summary>
        private AsyncTaskMethodBuilder(TaskController taskManager)
        {
            this.TaskController = taskManager;
            this.MethodBuilder = default;
            this.IsCompleted = false;
            this.UseBuilder = false;
        }

        /// <summary>
        /// Creates an instance of the <see cref="AsyncTaskMethodBuilder"/> struct.
        /// </summary>
        [DebuggerHidden]
        public static AsyncTaskMethodBuilder Create()
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return new AsyncTaskMethodBuilder(ControlledRuntime.Current.TaskController);
            }

            return new AsyncTaskMethodBuilder(null);
        }

        /// <summary>
        /// Begins running the builder with the associated state machine.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            IO.Debug.WriteLine("<AsyncBuilder> Start state machine from task '{0}'.", Task.CurrentId);
            this.TaskController?.OnAsyncTaskMethodBuilderStart(stateMachine.GetType());
            this.MethodBuilder.Start(ref stateMachine);
        }

        /// <summary>
        /// Associates the builder with the specified state machine.
        /// </summary>
        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine) =>
            this.MethodBuilder.SetStateMachine(stateMachine);

        /// <summary>
        /// Marks the task as successfully completed.
        /// </summary>
        [DebuggerHidden]
        public void SetResult()
        {
            if (this.UseBuilder)
            {
                IO.Debug.WriteLine("<AsyncBuilder> Set result of task '{0}' from task '{1}'.",
                    this.MethodBuilder.Task.Id, Task.CurrentId);
                this.MethodBuilder.SetResult();
            }
            else
            {
                IO.Debug.WriteLine("<AsyncBuilder> Set result (completed) from task '{0}'.", Task.CurrentId);
                this.IsCompleted = true;
            }
        }

        /// <summary>
        /// Marks the task as failed and binds the specified exception to the task.
        /// </summary>
        [DebuggerHidden]
        public void SetException(Exception exception) => this.MethodBuilder.SetException(exception);

        /// <summary>
        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        /// </summary>
        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            this.UseBuilder = true;
            this.TaskController?.OnAsyncTaskMethodBuilderAwaitCompleted(awaiter.GetType(), stateMachine.GetType());
            this.MethodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        /// <summary>
        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        /// </summary>
        [DebuggerHidden]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            this.UseBuilder = true;
            this.TaskController?.OnAsyncTaskMethodBuilderAwaitCompleted(awaiter.GetType(), stateMachine.GetType());
            this.MethodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
    }

    /// <summary>
    /// Represents a builder for asynchronous methods that return a <see cref="Task{TResult}"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [StructLayout(LayoutKind.Auto)]
    public struct AsyncTaskMethodBuilder<TResult>
    {
        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private readonly TaskController TaskController;

        /// <summary>
        /// The task builder to which most operations are delegated.
        /// </summary>
#pragma warning disable IDE0044 // Add readonly modifier
        private SystemCompiler.AsyncTaskMethodBuilder<TResult> MethodBuilder;
#pragma warning restore IDE0044 // Add readonly modifier

        /// <summary>
        /// The result for this builder, if it's completed before any awaits occur.
        /// </summary>
        private TResult Result;

        /// <summary>
        /// True, if completed synchronously and successfully, else false.
        /// </summary>
        private bool IsCompleted;

        /// <summary>
        /// True, if the builder should be used for setting/getting the result, else false.
        /// </summary>
        private bool UseBuilder;

        /// <summary>
        /// Gets the task for this builder.
        /// </summary>
        public Task<TResult> Task
        {
            [DebuggerHidden]
            get
            {
                if (this.IsCompleted)
                {
                    IO.Debug.WriteLine("<AsyncBuilder> Creating completed builder task '{0}' (isCompleted {1}) from task '{2}'.",
                        this.MethodBuilder.Task.Id, this.MethodBuilder.Task.IsCompleted, Tasks.Task.CurrentId);
                    return Tasks.Task.FromResult(this.Result);
                }
                else
                {
                    IO.Debug.WriteLine("<AsyncBuilder> Creating builder task '{0}' (isCompleted {1}) from task '{2}'.",
                        this.MethodBuilder.Task.Id, this.MethodBuilder.Task.IsCompleted, Tasks.Task.CurrentId);
                    this.UseBuilder = true;
                    this.TaskController?.OnAsyncTaskMethodBuilderTask();
                    return new Task<TResult>(this.TaskController, this.MethodBuilder.Task);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncTaskMethodBuilder{TResult}"/> struct.
        /// </summary>
        private AsyncTaskMethodBuilder(TaskController taskManager)
        {
            this.TaskController = taskManager;
            this.MethodBuilder = default;
            this.Result = default;
            this.IsCompleted = false;
            this.UseBuilder = false;
        }

        /// <summary>
        /// Creates an instance of the <see cref="AsyncTaskMethodBuilder{TResult}"/> struct.
        /// </summary>
#pragma warning disable CA1000 // Do not declare static members on generic types
        [DebuggerHidden]
        public static AsyncTaskMethodBuilder<TResult> Create()
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return new AsyncTaskMethodBuilder<TResult>(ControlledRuntime.Current.TaskController);
            }

            return new AsyncTaskMethodBuilder<TResult>(null);
        }
#pragma warning restore CA1000 // Do not declare static members on generic types

        /// <summary>
        /// Begins running the builder with the associated state machine.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            IO.Debug.WriteLine("<AsyncBuilder> Start state machine from task '{0}'.", Tasks.Task.CurrentId);
            this.TaskController?.OnAsyncTaskMethodBuilderStart(stateMachine.GetType());
            this.MethodBuilder.Start(ref stateMachine);
        }

        /// <summary>
        /// Associates the builder with the specified state machine.
        /// </summary>
        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine) =>
            this.MethodBuilder.SetStateMachine(stateMachine);

        /// <summary>
        /// Marks the task as successfully completed.
        /// </summary>
        /// <param name="result">The result to use to complete the task.</param>
        [DebuggerHidden]
        public void SetResult(TResult result)
        {
            if (this.UseBuilder)
            {
                IO.Debug.WriteLine("<AsyncBuilder> Set result of task '{0}' from task '{1}'.",
                    this.MethodBuilder.Task.Id, Tasks.Task.CurrentId);
                this.MethodBuilder.SetResult(result);
            }
            else
            {
                IO.Debug.WriteLine("<AsyncBuilder> Set result (completed) from task '{0}'.", Tasks.Task.CurrentId);
                this.Result = result;
                this.IsCompleted = true;
            }
        }

        /// <summary>
        /// Marks the task as failed and binds the specified exception to the task.
        /// </summary>
        [DebuggerHidden]
        public void SetException(Exception exception) => this.MethodBuilder.SetException(exception);

        /// <summary>
        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        /// </summary>
        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            this.UseBuilder = true;
            this.TaskController?.OnAsyncTaskMethodBuilderAwaitCompleted(awaiter.GetType(), stateMachine.GetType());
            this.MethodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        /// <summary>
        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        /// </summary>
        [DebuggerHidden]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            this.UseBuilder = true;
            this.TaskController?.OnAsyncTaskMethodBuilderAwaitCompleted(awaiter.GetType(), stateMachine.GetType());
            this.MethodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
    }
}
