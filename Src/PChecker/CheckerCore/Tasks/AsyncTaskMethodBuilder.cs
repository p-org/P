// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PChecker.Runtime;
using PChecker.SystematicTesting;
using Debug = PChecker.IO.Debugging.Debug;
using SystemCompiler = System.Runtime.CompilerServices;

namespace PChecker.Tasks
{
    /// <summary>
    /// Represents a builder for asynchronous methods that return a <see cref="Task"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
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
                if (IsCompleted)
                {
                    Debug.WriteLine("<AsyncBuilder> Creating completed builder task '{0}' (isCompleted {1}) from task '{2}'.",
                        MethodBuilder.Task.Id, MethodBuilder.Task.IsCompleted, Task.CurrentId);
                    return Task.CompletedTask;
                }
                else
                {
                    Debug.WriteLine("<AsyncBuilder> Creating builder task '{0}' (isCompleted {1}) from task '{2}'.",
                        MethodBuilder.Task.Id, MethodBuilder.Task.IsCompleted, Task.CurrentId);
                    UseBuilder = true;
                    TaskController?.OnAsyncTaskMethodBuilderTask();
                    return new Task(TaskController, MethodBuilder.Task);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncTaskMethodBuilder"/> struct.
        /// </summary>
        private AsyncTaskMethodBuilder(TaskController taskManager)
        {
            TaskController = taskManager;
            MethodBuilder = default;
            IsCompleted = false;
            UseBuilder = false;
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
        [SystemCompiler.MethodImpl(SystemCompiler.MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : SystemCompiler.IAsyncStateMachine
        {
            Debug.WriteLine("<AsyncBuilder> Start state machine from task '{0}'.", Task.CurrentId);
            TaskController?.OnAsyncTaskMethodBuilderStart(stateMachine.GetType());
            MethodBuilder.Start(ref stateMachine);
        }

        /// <summary>
        /// Associates the builder with the specified state machine.
        /// </summary>
        [DebuggerHidden]
        public void SetStateMachine(SystemCompiler.IAsyncStateMachine stateMachine) =>
            MethodBuilder.SetStateMachine(stateMachine);

        /// <summary>
        /// Marks the task as successfully completed.
        /// </summary>
        [DebuggerHidden]
        public void SetResult()
        {
            if (UseBuilder)
            {
                Debug.WriteLine("<AsyncBuilder> Set result of task '{0}' from task '{1}'.",
                    MethodBuilder.Task.Id, Task.CurrentId);
                MethodBuilder.SetResult();
            }
            else
            {
                Debug.WriteLine("<AsyncBuilder> Set result (completed) from task '{0}'.", Task.CurrentId);
                IsCompleted = true;
            }
        }

        /// <summary>
        /// Marks the task as failed and binds the specified exception to the task.
        /// </summary>
        [DebuggerHidden]
        public void SetException(Exception exception) => MethodBuilder.SetException(exception);

        /// <summary>
        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        /// </summary>
        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : SystemCompiler.INotifyCompletion
            where TStateMachine : SystemCompiler.IAsyncStateMachine
        {
            UseBuilder = true;
            TaskController?.OnAsyncTaskMethodBuilderAwaitCompleted(awaiter.GetType(), stateMachine.GetType());
            MethodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        /// <summary>
        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        /// </summary>
        [DebuggerHidden]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : SystemCompiler.ICriticalNotifyCompletion
            where TStateMachine : SystemCompiler.IAsyncStateMachine
        {
            UseBuilder = true;
            TaskController?.OnAsyncTaskMethodBuilderAwaitCompleted(awaiter.GetType(), stateMachine.GetType());
            MethodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
    }

    /// <summary>
    /// Represents a builder for asynchronous methods that return a <see cref="Task{TResult}"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
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
                if (IsCompleted)
                {
                    Debug.WriteLine("<AsyncBuilder> Creating completed builder task '{0}' (isCompleted {1}) from task '{2}'.",
                        MethodBuilder.Task.Id, MethodBuilder.Task.IsCompleted, Tasks.Task.CurrentId);
                    return Tasks.Task.FromResult(Result);
                }
                else
                {
                    Debug.WriteLine("<AsyncBuilder> Creating builder task '{0}' (isCompleted {1}) from task '{2}'.",
                        MethodBuilder.Task.Id, MethodBuilder.Task.IsCompleted, Tasks.Task.CurrentId);
                    UseBuilder = true;
                    TaskController?.OnAsyncTaskMethodBuilderTask();
                    return new Task<TResult>(TaskController, MethodBuilder.Task);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncTaskMethodBuilder{TResult}"/> struct.
        /// </summary>
        private AsyncTaskMethodBuilder(TaskController taskManager)
        {
            TaskController = taskManager;
            MethodBuilder = default;
            Result = default;
            IsCompleted = false;
            UseBuilder = false;
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
        [SystemCompiler.MethodImpl(SystemCompiler.MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : SystemCompiler.IAsyncStateMachine
        {
            Debug.WriteLine("<AsyncBuilder> Start state machine from task '{0}'.", Tasks.Task.CurrentId);
            TaskController?.OnAsyncTaskMethodBuilderStart(stateMachine.GetType());
            MethodBuilder.Start(ref stateMachine);
        }

        /// <summary>
        /// Associates the builder with the specified state machine.
        /// </summary>
        [DebuggerHidden]
        public void SetStateMachine(SystemCompiler.IAsyncStateMachine stateMachine) =>
            MethodBuilder.SetStateMachine(stateMachine);

        /// <summary>
        /// Marks the task as successfully completed.
        /// </summary>
        /// <param name="result">The result to use to complete the task.</param>
        [DebuggerHidden]
        public void SetResult(TResult result)
        {
            if (UseBuilder)
            {
                Debug.WriteLine("<AsyncBuilder> Set result of task '{0}' from task '{1}'.",
                    MethodBuilder.Task.Id, Tasks.Task.CurrentId);
                MethodBuilder.SetResult(result);
            }
            else
            {
                Debug.WriteLine("<AsyncBuilder> Set result (completed) from task '{0}'.", Tasks.Task.CurrentId);
                Result = result;
                IsCompleted = true;
            }
        }

        /// <summary>
        /// Marks the task as failed and binds the specified exception to the task.
        /// </summary>
        [DebuggerHidden]
        public void SetException(Exception exception) => MethodBuilder.SetException(exception);

        /// <summary>
        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        /// </summary>
        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : SystemCompiler.INotifyCompletion
            where TStateMachine : SystemCompiler.IAsyncStateMachine
        {
            UseBuilder = true;
            TaskController?.OnAsyncTaskMethodBuilderAwaitCompleted(awaiter.GetType(), stateMachine.GetType());
            MethodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        /// <summary>
        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        /// </summary>
        [DebuggerHidden]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : SystemCompiler.ICriticalNotifyCompletion
            where TStateMachine : SystemCompiler.IAsyncStateMachine
        {
            UseBuilder = true;
            TaskController?.OnAsyncTaskMethodBuilderAwaitCompleted(awaiter.GetType(), stateMachine.GetType());
            MethodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
    }
}