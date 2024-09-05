﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using PChecker.StateMachines;
using PChecker.StateMachines.EventQueues;
using PChecker.StateMachines.EventQueues.Mocks;
using PChecker.StateMachines.Events;
using PChecker.StateMachines.Exceptions;
using PChecker.StateMachines.Logging;
using PChecker.StateMachines.Managers;
using PChecker.StateMachines.Managers.Mocks;
using PChecker.Coverage;
using PChecker.Exceptions;
using PChecker.PRuntime;
using PChecker.PRuntime.Values;
using PChecker.Random;
using PChecker.Runtime;
using PChecker.Specifications.Monitors;
using PChecker.SystematicTesting.Operations;
using PChecker.SystematicTesting.Strategies;
using PChecker.SystematicTesting.Strategies.Liveness;
using PChecker.SystematicTesting.Traces;
using Debug = PChecker.IO.Debugging.Debug;
using EventInfo = PChecker.StateMachines.Events.EventInfo;
using PMachineValue = PChecker.PRuntime.Values.PMachineValue;

namespace PChecker.SystematicTesting
{
    /// <summary>
    /// Runtime for controlling asynchronous operations.
    /// </summary>
    internal sealed class ControlledRuntime : CoyoteRuntime, IStateMachineRuntime
    {
        
        /// <summary>
        /// The currently executing runtime.
        /// </summary>
        internal static new ControlledRuntime Current => CoyoteRuntime.Current as ControlledRuntime;

        /// <summary>
        /// The asynchronous operation scheduler.
        /// </summary>
        internal readonly OperationScheduler Scheduler;

        /// <summary>
        /// Responsible for controlling the execution of tasks.
        /// </summary>
        internal TaskController TaskController { get; private set; }

        /// <summary>
        /// Data structure containing information regarding testing coverage.
        /// </summary>
        internal CoverageInfo CoverageInfo;

        /// <summary>
        /// Map that stores all unique names and their corresponding state machine ids.
        /// </summary>
        internal readonly ConcurrentDictionary<string, StateMachineId> NameValueToStateMachineId;

        /// <summary>
        /// The root task id.
        /// </summary>
        internal readonly int? RootTaskId;
        
        /// <summary>
        /// Cache storing state machine constructors.
        /// </summary>
        private static readonly Dictionary<Type, Func<StateMachine>> StateMachineConstructorCache =
            new Dictionary<Type, Func<StateMachine>>();
        
        /// <summary>
        /// Map from unique state machine ids to state machines.
        /// </summary>
        private readonly ConcurrentDictionary<StateMachineId, StateMachine> StateMachineMap;
        
        /// <summary>
        /// Callback that is fired when a Coyote event is dropped.
        /// </summary>
        public event OnEventDroppedHandler OnEventDropped;
        
        /// <summary>
        /// Responsible for writing to all registered <see cref="IStateMachineRuntimeLog"/> objects.
        /// </summary>
        protected internal LogWriter LogWriter { get; private set; }

        /// <summary>
        /// Used to log text messages. Use <see cref="ICoyoteRuntime.SetLogger"/>
        /// to replace the logger with a custom one.
        /// </summary>
        public override TextWriter Logger => LogWriter.Logger;

        /// <summary>
        /// Used to log json trace outputs.
        /// </summary>
        public JsonWriter JsonLogger => LogWriter.JsonLogger;


        /// <summary>
        /// Returns the current hashed state of the monitors.
        /// </summary>
        /// <remarks>
        /// The hash is updated in each execution step.
        /// </remarks>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        private int GetHashedMonitorState()
        {
            unchecked
            {
                int hash = 19;

                foreach (var monitor in Monitors)
                {
                    hash = (hash * 397) + monitor.GetHashedState();
                }

                return hash;
            }
        }

        /// <summary>
        /// Returns the current hashed state of the execution.
        /// </summary>
        /// <remarks>
        /// The hash is updated in each execution step.
        /// </remarks>
        [DebuggerStepThrough]
        internal int GetHashedProgramState()
        {
            unchecked
            {
                int hash = 19;

                foreach (var operation in Scheduler.GetRegisteredOperations().OrderBy(op => op.Id))
                {
                    if (operation is StateMachineOperation stateMachineOperation)
                    {
                        int operationHash = 31 + stateMachineOperation.StateMachine.GetHashedState();
                        operationHash = (operationHash * 31) + stateMachineOperation.Type.GetHashCode();
                        hash *= operationHash;
                    }
                    else if (operation is TaskOperation taskOperation)
                    {
                        hash *= 31 + taskOperation.Type.GetHashCode();
                    }
                }

                hash = (hash * 31) + GetHashedMonitorState();
                return hash;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledRuntime"/> class.
        /// </summary>
        internal ControlledRuntime(CheckerConfiguration checkerConfiguration, ISchedulingStrategy strategy,
            IRandomValueGenerator valueGenerator)
            : base(checkerConfiguration, valueGenerator)
        {
            StateMachineMap = new ConcurrentDictionary<StateMachineId, StateMachine>();
            LogWriter = new LogWriter(checkerConfiguration);
            
            IsExecutionControlled = true;

            RootTaskId = Task.CurrentId;
            NameValueToStateMachineId = new ConcurrentDictionary<string, StateMachineId>();

            CoverageInfo = new CoverageInfo();

            var scheduleTrace = new ScheduleTrace();
            if (checkerConfiguration.IsLivenessCheckingEnabled)
            {
                strategy = new TemperatureCheckingStrategy(checkerConfiguration, Monitors, strategy);
            }

            Scheduler = new OperationScheduler(this, strategy, scheduleTrace, CheckerConfiguration);
            TaskController = new TaskController(this, Scheduler);

            // Update the current asynchronous control flow with this runtime instance,
            // allowing future retrieval in the same asynchronous call stack.
            AssignAsyncControlFlowRuntime(this);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledRuntime"/> class.
        /// </summary>
        internal ControlledRuntime(CheckerConfiguration checkerConfiguration,
            IRandomValueGenerator valueGenerator)
            : base(checkerConfiguration, valueGenerator)
        {
            StateMachineMap = new ConcurrentDictionary<StateMachineId, StateMachine>();
            LogWriter = new LogWriter(checkerConfiguration);
            
            IsExecutionControlled = true;

            RootTaskId = Task.CurrentId;
            NameValueToStateMachineId = new ConcurrentDictionary<string, StateMachineId>();

            CoverageInfo = new CoverageInfo();
            
            // Update the current asynchronous control flow with this runtime instance,
            // allowing future retrieval in the same asynchronous call stack.
            AssignAsyncControlFlowRuntime(this);
        }
        
        /// <summary>
        /// Creates a fresh state machine id that has not yet been bound to any state machine.
        /// </summary>
        public StateMachineId CreateStateMachineId(Type type, string name = null) => new StateMachineId(type, name, this);

        /// <summary>
        /// Creates an state machine id that is uniquely tied to the specified unique name. The
        /// returned state machine id can either be a fresh id (not yet bound to any state machine), or
        /// it can be bound to a previously created state machine. In the second case, this state machine
        /// id can be directly used to communicate with the corresponding state machine.
        /// </summary>
        public StateMachineId CreateStateMachineIdFromName(Type type, string name)
        {
            // It is important that all state machine ids use the monotonically incrementing
            // value as the id during testing, and not the unique name.
            var id = new StateMachineId(type, name, this);
            return NameValueToStateMachineId.GetOrAdd(name, id);
        }

        /// <summary>
        /// Creates a new state machine of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event can only be used to access its payload,
        /// and cannot be handled.
        /// </summary>
        public StateMachineId CreateStateMachine(Type type, Event initialEvent = null, Guid opGroupId = default) =>
            CreateStateMachine(null, type, null, initialEvent, opGroupId);

        /// <summary>
        /// Creates a new state machine of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled.
        /// </summary>
        public StateMachineId CreateStateMachine(Type type, string name, Event initialEvent = null, Guid opGroupId = default) =>
            CreateStateMachine(null, type, name, initialEvent, opGroupId);

        /// <summary>
        /// Creates a new state machine of the specified type, using the specified <see cref="StateMachineId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new state machine, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        public StateMachineId CreateStateMachine(StateMachineId id, Type type, Event initialEvent = null, Guid opGroupId = default)
        {
            Assert(id != null, "Cannot create an state machine using a null state machine id.");
            return CreateStateMachine(id, type, null, initialEvent, opGroupId);
        }

        /// <summary>
        /// Creates a new state machine of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event can only be used to access its payload,
        /// and cannot be handled. The method returns only when the state machine is initialized and
        /// the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public Task<StateMachineId> CreateStateMachineAndExecuteAsync(Type type, Event e = null, Guid opGroupId = default) =>
            CreateStateMachineAndExecuteAsync(null, type, null, e, opGroupId);

        /// <summary>
        /// Creates a new state machine of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled. The method returns only when the state machine is
        /// initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public Task<StateMachineId> CreateStateMachineAndExecuteAsync(Type type, string name, Event e = null, Guid opGroupId = default) =>
            CreateStateMachineAndExecuteAsync(null, type, name, e, opGroupId);

        /// <summary>
        /// Creates a new state machine of the specified <see cref="Type"/>, using the specified unbound
        /// state machine id, and passes the specified optional <see cref="Event"/>. This event can only
        /// be used to access its payload, and cannot be handled. The method returns only when
        /// the state machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        public Task<StateMachineId> CreateStateMachineAndExecuteAsync(StateMachineId id, Type type, Event e = null, Guid opGroupId = default)
        {
            Assert(id != null, "Cannot create an state machine using a null state machine id.");
            return CreateStateMachineAndExecuteAsync(id, type, null, e, opGroupId);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a state machine.
        /// </summary>
        public void SendEvent(StateMachineId targetId, Event e, Guid opGroupId = default)
        {
            var senderOp = Scheduler.GetExecutingOperation<StateMachineOperation>();
            SendEvent(targetId, e, senderOp?.StateMachine, opGroupId);
        }

        /// <summary>
        /// Sends an <see cref="Event"/> to a state machine. Returns immediately if the target was already
        /// running. Otherwise, blocks until the target handles the event and reaches quiescence.
        /// </summary>
        public Task<bool> SendEventAndExecuteAsync(StateMachineId targetId, Event e, Guid opGroupId = default)
        {
            var senderOp = Scheduler.GetExecutingOperation<StateMachineOperation>();
            return SendEventAndExecuteAsync(targetId, e, senderOp?.StateMachine, opGroupId);
        }

        /// <summary>
        /// Returns the operation group id of the state machine with the specified id. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="StateMachineId"/> is not associated with this runtime. During
        /// testing, the runtime asserts that the specified state machine is currently executing.
        /// </summary>
        public Guid GetCurrentOperationGroupId(StateMachineId currentStateMachineId)
        {
            var callerOp = Scheduler.GetExecutingOperation<StateMachineOperation>();
            Assert(callerOp != null && currentStateMachineId == callerOp.StateMachine.Id,
                "Trying to access the operation group id of {0}, which is not the currently executing state machine.",
                currentStateMachineId);
            return callerOp.StateMachine.OperationGroupId;
        }

        /// <summary>
        /// Runs the specified test method.
        /// </summary>
        internal void RunTest(Delegate testMethod, string testName)
        {
            testName = string.IsNullOrEmpty(testName) ? string.Empty : $" '{testName}'";
            Logger.WriteLine($"<TestLog> Running test{testName}.");
            Assert(testMethod != null, "Unable to execute a null test method.");
            Assert(Task.CurrentId != null, "The test must execute inside a controlled task.");

            var operationId = GetNextOperationId();
            var op = new TaskOperation(operationId, Scheduler);
            Scheduler.RegisterOperation(op);
            op.OnEnabled();

            var task = new Task(async () =>
            {
                try
                {
                    // Update the current asynchronous control flow with the current runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    AssignAsyncControlFlowRuntime(this);

                    OperationScheduler.StartOperation(op);

                    if (testMethod is Action<IStateMachineRuntime> actionWithRuntime)
                    {
                        actionWithRuntime(this);
                    }
                    else if (testMethod is Action action)
                    {
                        action();
                    }
                    else if (testMethod is Func<IStateMachineRuntime, Tasks.Task> functionWithRuntime)
                    {
                        await functionWithRuntime(this);
                    }
                    else if (testMethod is Func<Tasks.Task> function)
                    {
                        await function();
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unsupported test delegate of type '{testMethod.GetType()}'.");
                    }

                    Debug.WriteLine("<ScheduleDebug> Completed operation {0} on task '{1}'.", op.Name, Task.CurrentId);
                    op.OnCompleted();

                    // Task has completed, schedule the next enabled operation.
                    Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Stop);
                }
                catch (Exception ex)
                {
                    ProcessUnhandledExceptionInOperation(op, ex);
                }
            });

            Scheduler.ScheduleOperation(op, task.Id);
            task.Start();
            Scheduler.WaitOperationStart(op);
        }

        /// <summary>
        /// Creates a new state machine of the specified <see cref="Type"/> and name, using the specified
        /// unbound state machine id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        internal StateMachineId CreateStateMachine(StateMachineId id, Type type, string name, Event initialEvent = null,
            Guid opGroupId = default)
        {
            var creatorOp = Scheduler.GetExecutingOperation<StateMachineOperation>();
            return CreateStateMachine(id, type, name, initialEvent, creatorOp?.StateMachine, opGroupId);
        }

        /// <summary>
        /// Creates a new <see cref="StateMachine"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal StateMachineId CreateStateMachine(StateMachineId id, Type type, string name, Event initialEvent, StateMachine creator,
            Guid opGroupId)
        {
            AssertExpectedCallerStateMachine(creator, "CreateStateMachine");

            var stateMachine = CreateStateMachine(id, type, name, creator, opGroupId);
            LogWriter.LogCreateStateMachine(stateMachine.Id, creator?.Id.Name, creator?.Id.Type);
            RunStateMachineEventHandler(stateMachine, initialEvent, true, null);
            return stateMachine.Id;
        }

        /// <summary>
        /// Creates a new state machine of the specified <see cref="Type"/> and name, using the specified
        /// unbound state machine id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled. The method returns only
        /// when the state machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        internal Task<StateMachineId> CreateStateMachineAndExecuteAsync(StateMachineId id, Type type, string name, Event initialEvent = null,
            Guid opGroupId = default)
        {
            var creatorOp = Scheduler.GetExecutingOperation<StateMachineOperation>();
            return CreateStateMachineAndExecuteAsync(id, type, name, initialEvent, creatorOp?.StateMachine, opGroupId);
        }

        /// <summary>
        /// Creates a new <see cref="StateMachine"/> of the specified <see cref="Type"/>. The method
        /// returns only when the state machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        internal async Task<StateMachineId> CreateStateMachineAndExecuteAsync(StateMachineId id, Type type, string name,
            Event initialEvent, StateMachine creator, Guid opGroupId)
        {
            AssertExpectedCallerStateMachine(creator, "CreateStateMachineAndExecuteAsync");
            Assert(creator != null, "Only a state machine can call 'CreateStateMachineAndExecuteAsync': avoid calling " +
                                    "it directly from the test method; instead call it through a test driver state machine.");

            var stateMachine = CreateStateMachine(id, type, name, creator, opGroupId);
            RunStateMachineEventHandler(stateMachine, initialEvent, true, creator);

            // Wait until the state machine reaches quiescence.
            await creator.ReceiveEventAsync(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).StateMachineId == stateMachine.Id);
            return await Task.FromResult(stateMachine.Id);
        }

        /// <summary>
        /// Creates a new state machine of the specified <see cref="Type"/>.
        /// </summary>
        private StateMachine CreateStateMachine(StateMachineId id, Type type, string name, StateMachine creator, Guid opGroupId)
        {
            Assert(type.IsSubclassOf(typeof(StateMachine)), "Type '{0}' is not a state machine.", type.FullName);

            // Using ulong.MaxValue because a Create operation cannot specify
            // the id of its target, because the id does not exist yet.
            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Create);
            ResetProgramCounter(creator);

            if (id is null)
            {
                id = new StateMachineId(type, name, this);
            }
            else
            {
                Assert(id.Runtime is null || id.Runtime == this, "Unbound state machine id '{0}' was created by another runtime.", id.Value);
                Assert(id.Type == type.FullName, "Cannot bind state machine id '{0}' of type '{1}' to an state machine of type '{2}'.",
                    id.Value, id.Type, type.FullName);
                id.Bind(this);
            }

            // The operation group id of the state machine is set using the following precedence:
            // (1) To the specified state machine creation operation group id, if it is non-empty.
            // (2) To the operation group id of the creator state machine, if it exists and is non-empty.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && creator != null)
            {
                opGroupId = creator.OperationGroupId;
            }

            var stateMachine = Create(type);
            IStateMachineManager stateMachineManager = new StateMachineManager(this, stateMachine, opGroupId);

            IEventQueue eventQueue = new EventQueue(stateMachineManager, stateMachine);
            stateMachine.Configure(this, id, stateMachineManager, eventQueue);
            stateMachine.SetupEventHandlers();
            stateMachine.self = new PMachineValue(id, stateMachine.receives.ToList());
            stateMachine.interfaceName = "I_" + name;

            if (CheckerConfiguration.ReportActivityCoverage)
            {
                ReportActivityCoverageOfStateMachine(stateMachine);
            }

            var result = Scheduler.RegisterOperation(new StateMachineOperation(stateMachine));
            Assert(result, "StateMachine id '{0}' is used by an existing or previously halted state machine.", id.Value);
            LogWriter.LogCreateStateMachine(id, creator?.Id.Name, creator?.Id.Type);

            return stateMachine;
        }
        
        /// <summary>
        /// Creates a new <see cref="StateMachine"/> instance of the specified type.
        /// </summary>
        /// <param name="type">The type of the state machines.</param>
        /// <returns>The created state machine instance.</returns>
        public static StateMachine Create(Type type)
        {
            Func<StateMachine> constructor = null;
            lock (StateMachineConstructorCache)
            {
                if (!StateMachineConstructorCache.TryGetValue(type, out constructor))
                {
                    var constructorInfo = type.GetConstructor(Type.EmptyTypes);
                    if (constructorInfo == null)
                    {
                        throw new Exception("Could not find empty constructor for type " + type.FullName);
                    }

                    constructor = Expression.Lambda<Func<StateMachine>>(
                        Expression.New(constructorInfo)).Compile();
                    StateMachineConstructorCache.Add(type, constructor);
                }
            }

            return constructor();
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a state machine.
        /// </summary>
        internal void SendEvent(StateMachineId targetId, Event e, StateMachine sender, Guid opGroupId)
        {
            if (e is null)
            {
                var message = sender != null ?
                    string.Format("{0} is sending a null event.", sender.Id) :
                    "Cannot send a null event.";
                Assert(false, message);
            }

            if (sender != null)
            {
                Assert(targetId != null, "{0} is sending event {1} to a null state machine.", sender.Id, e);
            }
            else
            {
                Assert(targetId != null, "Cannot send event {1} to a null state machine.", e);
            }

            AssertExpectedCallerStateMachine(sender, "SendEvent");

            var enqueueStatus = EnqueueEvent(targetId, e, sender, opGroupId, out var target);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                RunStateMachineEventHandler(target, null, false, null);
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a state machine. Returns immediately if the target was
        /// already running. Otherwise, blocks until the target handles the event and reaches quiescence.
        /// </summary>
        internal async Task<bool> SendEventAndExecuteAsync(StateMachineId targetId, Event e, StateMachine sender,
            Guid opGroupId)
        {
            Assert(e != null, "{0} is sending a null event.", sender.Id);
            Assert(targetId != null, "{0} is sending event {1} to a null state machine.", sender.Id, e);
            AssertExpectedCallerStateMachine(sender, "SendEventAndExecuteAsync");

            var enqueueStatus = EnqueueEvent(targetId, e, sender, opGroupId, out var target);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                RunStateMachineEventHandler(target, null, false, sender);

                // Wait until the state machine reaches quiescence.
                await sender.ReceiveEventAsync(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).StateMachineId == targetId);
                return true;
            }

            // EnqueueStatus.EventHandlerNotRunning is not returned by EnqueueEvent
            // (even when the state machine was previously inactive) when the event e requires
            // no action by the state machine (i.e., it implicitly handles the event).
            return enqueueStatus is EnqueueStatus.Dropped || enqueueStatus is EnqueueStatus.NextEventUnavailable;
        }

        /// <summary>
        /// Enqueues an event to the state machine with the specified id.
        /// </summary>
        private EnqueueStatus EnqueueEvent(StateMachineId targetId, Event e, StateMachine sender, Guid opGroupId, out StateMachine target)
        {
            target = Scheduler.GetOperationWithId<StateMachineOperation>(targetId.Value)?.StateMachine;
            Assert(target != null,
                "Cannot send event '{0}' to state machine id '{1}' that is not bound to an state machine instance.",
                e.GetType().FullName, targetId.Value);

            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Send);
            ResetProgramCounter(sender);

            // The operation group id of this operation is set using the following precedence:
            // (1) To the specified send operation group id, if it is non-empty.
            // (2) To the operation group id of the sender state machine, if it exists and is non-empty.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && sender != null)
            {
                opGroupId = sender.OperationGroupId;
            }

            if (target.IsHalted)
            {
                LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                    (sender)?.CurrentStateName ?? string.Empty, e, opGroupId, isTargetHalted: true);
                TryHandleDroppedEvent(e, targetId);
                return EnqueueStatus.Dropped;
            }

            var enqueueStatus = EnqueueEvent(target, e, sender, opGroupId);
            if (enqueueStatus == EnqueueStatus.Dropped)
            {
                TryHandleDroppedEvent(e, targetId);
            }

            return enqueueStatus;
        }

        /// <summary>
        /// Enqueues an event to the state machine with the specified id.
        /// </summary>
        private EnqueueStatus EnqueueEvent(StateMachine stateMachine, Event e, StateMachine sender, Guid opGroupId)
        {
            // Directly use sender as a StateMachine
            var originInfo = new EventOriginInfo(sender.Id, sender.GetType().FullName,
                NameResolver.GetStateNameForLogging(sender.CurrentState.GetType()));

            var eventInfo = new EventInfo(e, originInfo);

            LogWriter.LogSendEvent(stateMachine.Id, sender.Id.Name, sender.Id.Type, sender.CurrentStateName,
                e, opGroupId, isTargetHalted: false);
    
            return stateMachine.Enqueue(e, opGroupId, eventInfo);
        }

        /// <summary>
        /// Runs a new asynchronous event handler for the specified state machine.
        /// This is a fire-and-forget invocation.
        /// </summary>
        /// <param name="stateMachine">The state machine that executes this event handler.</param>
        /// <param name="initialEvent">Optional event for initializing the state machine.</param>
        /// <param name="isFresh">If true, then this is a new state machine.</param>
        /// <param name="syncCaller">Caller state machine that is blocked for quiescence.</param>
        private void RunStateMachineEventHandler(StateMachine stateMachine, Event initialEvent, bool isFresh, StateMachine syncCaller)
        {
            var op = Scheduler.GetOperationWithId<StateMachineOperation>(stateMachine.Id.Value);
            op.OnEnabled();

            var task = new Task(async () =>
            {
                try
                {
                    // Update the current asynchronous control flow with this runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    AssignAsyncControlFlowRuntime(this);

                    OperationScheduler.StartOperation(op);

                    if (isFresh)
                    {
                        await stateMachine.InitializeAsync(initialEvent);
                    }

                    await stateMachine.RunEventHandlerAsync();
                    if (syncCaller != null)
                    {
                        EnqueueEvent(syncCaller, new QuiescentEvent(stateMachine.Id), stateMachine, stateMachine.OperationGroupId);
                    }

                    if (!stateMachine.IsHalted)
                    {
                        ResetProgramCounter(stateMachine);
                    }

                    Debug.WriteLine("<ScheduleDebug> Completed operation {0} on task '{1}'.", stateMachine.Id, Task.CurrentId);
                    op.OnCompleted();

                    // The state machine is inactive or halted, schedule the next enabled operation.
                    Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Stop);
                }
                catch (Exception ex)
                {
                    ProcessUnhandledExceptionInOperation(op, ex);
                }
            });

            Scheduler.ScheduleOperation(op, task.Id);
            task.Start();
            Scheduler.WaitOperationStart(op);
        }

        /// <summary>
        /// Processes an unhandled exception in the specified asynchronous operation.
        /// </summary>
        private void ProcessUnhandledExceptionInOperation(AsyncOperation op, Exception ex)
        {
            var innerException = ex;
            while (innerException is TargetInvocationException)
            {
                innerException = innerException.InnerException;
            }

            if (innerException is AggregateException)
            {
                innerException = innerException.InnerException;
            }

            if (innerException is ExecutionCanceledException || innerException is TaskSchedulerException)
            {
                Debug.WriteLine("<Exception> {0} was thrown from operation '{1}'.",
                    innerException.GetType().Name, op.Name);
            }
            else if (innerException is ObjectDisposedException)
            {
                Debug.WriteLine("<Exception> {0} was thrown from operation '{1}' with reason '{2}'.",
                    innerException.GetType().Name, op.Name, ex.Message);
            }
            else
            {
                // Report the unhandled exception.
                var message = string.Format(CultureInfo.InvariantCulture,
                    $"Exception '{ex.GetType()}' was thrown in operation {op.Name}, " +
                    $"'{ex.Source}':\n" +
                    $"   {ex.Message}\n" +
                    $"The stack trace is:\n{ex.StackTrace}");
                Scheduler.NotifyAssertionFailure(message, killTasks: true, cancelExecution: false);
            }
        }

        /// <inheritdoc/>
        internal override void TryCreateMonitor(Type type)
        {
            if (Monitors.Any(m => m.GetType() == type))
            {
                // Idempotence: only one monitor per type can exist.
                return;
            }

            Assert(type.IsSubclassOf(typeof(Monitor)), "Type '{0}' is not a subclass of Monitor.", type.FullName);

            var monitor = Activator.CreateInstance(type) as Monitor;
            monitor.Initialize(this);
            monitor.InitializeStateInformation();

            LogWriter.LogCreateMonitor(type.FullName);

            if (CheckerConfiguration.ReportActivityCoverage)
            {
                ReportActivityCoverageOfMonitor(monitor);
            }

            Monitors.Add(monitor);

            monitor.GotoStartState();
        }

        /// <inheritdoc/>
        internal override void Monitor(Type type, Event e, string senderName, string senderType, string senderStateName)
        {
            foreach (var monitor in Monitors)
            {
                if (monitor.GetType() == type)
                {
                    monitor.MonitorEvent(e, senderName, senderType, senderStateName);
                    break;
                }
            }
        }

        /// <inheritdoc/>
#if !DEBUG
        [DebuggerHidden]
#endif
        public override void Assert(bool predicate)
        {
            if (!predicate)
            {
                Scheduler.NotifyAssertionFailure("Detected an assertion failure.");
            }
        }

        /// <inheritdoc/>
#if !DEBUG
        [DebuggerHidden]
#endif
        public override void Assert(bool predicate, string s, object arg0)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString());
                Scheduler.NotifyAssertionFailure(msg);
            }
        }

        /// <inheritdoc/>
#if !DEBUG
        [DebuggerHidden]
#endif
        public override void Assert(bool predicate, string s, object arg0, object arg1)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString(), arg1?.ToString());
                Scheduler.NotifyAssertionFailure(msg);
            }
        }

        /// <inheritdoc/>
#if !DEBUG
        [DebuggerHidden]
#endif
        public override void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString(), arg1?.ToString(), arg2?.ToString());
                Scheduler.NotifyAssertionFailure(msg);
            }
        }

        /// <inheritdoc/>
#if !DEBUG
        [DebuggerHidden]
#endif
        public override void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, args);
                Scheduler.NotifyAssertionFailure(msg);
            }
        }

        /// <summary>
        /// Asserts that the state machine calling an state machine method is also
        /// the state machine that is currently executing.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        private void AssertExpectedCallerStateMachine(StateMachine caller, string calledAPI)
        {
            if (caller is null)
            {
                return;
            }

            var op = Scheduler.GetExecutingOperation<StateMachineOperation>();
            if (op is null)
            {
                return;
            }

            Assert(op.StateMachine.Equals(caller), "{0} invoked {1} on behalf of {2}.",
                op.StateMachine.Id, calledAPI, caller.Id);
        }

        /// <summary>
        /// Checks that no monitor is in a hot state upon program termination.
        /// If the program is still running, then this method returns without
        /// performing a check.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void CheckNoMonitorInHotStateAtTermination()
        {
            if (!Scheduler.HasFullyExploredSchedule)
            {
                return;
            }

            foreach (var monitor in Monitors)
            {
                if (monitor.IsInHotState(out var stateName))
                {
                    var message = string.Format(CultureInfo.InvariantCulture,
                        "{0} detected liveness bug in hot state '{1}' at the end of program execution.",
                        monitor.GetType().FullName, stateName);
                    Scheduler.NotifyAssertionFailure(message, killTasks: false, cancelExecution: false);
                }
            }
        }

        /// <inheritdoc/>
        internal override bool GetNondeterministicBooleanChoice(int maxValue, string callerName, string callerType)
        {
            var caller = Scheduler.GetExecutingOperation<StateMachineOperation>()?.StateMachine;
            if (caller != null)
            {
                (caller.Manager as StateMachineManager).ProgramCounter++;
            }

            var choice = Scheduler.GetNextNondeterministicBooleanChoice(maxValue);
            LogWriter.LogRandom(choice, callerName ?? caller?.Id.Name, callerType ?? caller?.Id.Type);
            return choice;
        }

        /// <inheritdoc/>
        internal override int GetNondeterministicIntegerChoice(int maxValue, string callerName, string callerType)
        {
            var caller = Scheduler.GetExecutingOperation<StateMachineOperation>()?.StateMachine;
            if (caller != null)
            {
                (caller.Manager as StateMachineManager).ProgramCounter++;
            }

            var choice = Scheduler.GetNextNondeterministicIntegerChoice(maxValue);
            LogWriter.LogRandom(choice, callerName ?? caller?.Id.Name, callerType ?? caller?.Id.Type);
            return choice;
        }
        
        /// <summary>
        /// Gets the state machine of type <typeparamref name="TStateMachine"/> with the specified id,
        /// or null if no such state machine exists.
        /// </summary>
        private TStateMachine GetStateMachineWithId<TStateMachine>(StateMachineId id)
            where TStateMachine : StateMachine =>
            id != null && StateMachineMap.TryGetValue(id, out var value) &&
            value is TStateMachine stateMachine ? stateMachine : null;

        /// <summary>
        /// Gets the <see cref="IAsyncOperation"/> that is executing on the current
        /// synchronization context, or null if no such operation is executing.
        /// </summary>
        internal TAsyncOperation GetExecutingOperation<TAsyncOperation>()
            where TAsyncOperation : IAsyncOperation =>
            Scheduler.GetExecutingOperation<TAsyncOperation>();


        /// <summary>
        /// Checks if the scheduling steps bound has been reached. If yes,
        /// it stops the scheduler and kills all enabled machines.
        /// </summary>
        private void CheckIfSchedulingStepsBoundIsReached()
        {
            Scheduler.CheckIfSchedulingStepsBoundIsReached();
        }

        /// <summary>
        /// Schedules the next controlled asynchronous operation. This method
        /// is only used during testing.
        /// </summary>
        /// <param name="type">Type of the operation.</param>
        internal void ScheduleNextOperation(AsyncOperationType type)
        {
            var callerOp = Scheduler.GetExecutingOperation<AsyncOperation>();
            if (callerOp != null)
            {
                Scheduler.ScheduleNextEnabledOperation(type);
            }
        }

        /// <summary>
        /// Notifies that a state machine invoked an action.
        /// </summary>
        internal void NotifyInvokedAction(StateMachine stateMachine, MethodInfo action, string handlingStateName,
            string currentStateName, Event receivedEvent)
        {
            LogWriter.LogExecuteAction(stateMachine.Id, handlingStateName, currentStateName, action.Name);
        }

        /// <summary>
        /// Notifies that a state machine dequeued an <see cref="Event"/>.
        /// </summary>
        internal void NotifyDequeuedEvent(StateMachine stateMachine, Event e, EventInfo eventInfo)
        {
            var op = Scheduler.GetOperationWithId<StateMachineOperation>(stateMachine.Id.Value);

            // Skip `ReceiveEventAsync` if the last operation exited the previous event handler,
            // to avoid scheduling duplicate `ReceiveEventAsync` operations.
            if (op.SkipNextReceiveSchedulingPoint)
            {
                op.SkipNextReceiveSchedulingPoint = false;
            }
            else
            {
                Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Receive);
                ResetProgramCounter(stateMachine);
            }

            var stateName = stateMachine.CurrentStateName;
            LogWriter.LogDequeueEvent(stateMachine.Id, stateName, e);
        }

        /// <summary>
        /// Notifies that a state machine dequeued the default <see cref="Event"/>.
        /// </summary>
        internal void NotifyDefaultEventDequeued(StateMachine stateMachine)
        {
            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Receive);
            ResetProgramCounter(stateMachine);
        }

        /// <summary>
        /// Notifies that the inbox of the specified state machine is about to be
        /// checked to see if the default event handler should fire.
        /// </summary>
        internal void NotifyDefaultEventHandlerCheck(StateMachine stateMachine)
        {
            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Default);
        }

        /// <summary>
        /// Notifies that a state machine raised an <see cref="Event"/>.
        /// </summary>
        internal void NotifyRaisedEvent(StateMachine stateMachine, Event e, EventInfo eventInfo)
        {
            var stateName = stateMachine.CurrentStateName;
            LogWriter.LogRaiseEvent(stateMachine.Id, stateName, e);
        }

        /// <summary>
        /// Notifies that a state machine is handling a raised <see cref="Event"/>.
        /// </summary>
        internal void NotifyHandleRaisedEvent(StateMachine stateMachine, Event e)
        {
            var stateName = stateMachine.CurrentStateName;
            LogWriter.LogHandleRaisedEvent(stateMachine.Id, stateName, e);
        }

        /// <summary>
        /// Notifies that a state machine called <see cref="StateMachine.ReceiveEventAsync(Type[])"/>
        /// or one of its overloaded methods.
        /// </summary>
        internal void NotifyReceiveCalled(StateMachine stateMachine)
        {
            AssertExpectedCallerStateMachine(stateMachine, "ReceiveEventAsync");
        }

        /// <summary>
        /// Notifies that a state machine enqueued an event that it was waiting to receive.
        /// </summary>
        internal void NotifyReceivedEvent(StateMachine stateMachine, Event e, EventInfo eventInfo)
        {
            var stateName = stateMachine.CurrentStateName;
            LogWriter.LogReceiveEvent(stateMachine.Id, stateName, e, wasBlocked: true);
            var op = Scheduler.GetOperationWithId<StateMachineOperation>(stateMachine.Id.Value);
            op.OnReceivedEvent();
        }

        /// <summary>
        /// Notifies that a state machine received an event without waiting because the event
        /// was already in the inbox when the state machine invoked the receiving statement.
        /// </summary>
        internal void NotifyReceivedEventWithoutWaiting(StateMachine stateMachine, Event e, EventInfo eventInfo)
        {
            var stateName = stateMachine.CurrentStateName;
            LogWriter.LogReceiveEvent(stateMachine.Id, stateName, e, wasBlocked: false);
            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Receive);
            ResetProgramCounter(stateMachine);
        }

        /// <summary>
        /// Notifies that a state machine is waiting for the specified task to complete.
        /// </summary>
        internal void NotifyWaitTask(StateMachine stateMachine, Task task)
        {
            Assert(task != null, "{0} is waiting for a null task to complete.", stateMachine.Id);

            var finished = task.IsCompleted || task.IsCanceled || task.IsFaulted;
            if (!finished)
            {
                Assert(finished,
                    "Controlled task '{0}' is trying to wait for an uncontrolled task or awaiter to complete. Please " +
                    "make sure to avoid using concurrency APIs (e.g. 'Task.Run', 'Task.Delay' or 'Task.Yield' from " +
                    "the 'System.Threading.Tasks' namespace) inside state machine handlers. If you are using external libraries " +
                    "that are executing concurrently, you will need to mock them during testing.",
                    Task.CurrentId);
            }
        }

        /// <summary>
        /// Notifies that a state machine is waiting to receive an event of one of the specified types.
        /// </summary>
        internal void NotifyWaitEvent(StateMachine stateMachine, IEnumerable<Type> eventTypes)
        {
            var stateName = stateMachine.CurrentStateName;
            var op = Scheduler.GetOperationWithId<StateMachineOperation>(stateMachine.Id.Value);
            op.OnWaitEvent(eventTypes);

            var eventWaitTypesArray = eventTypes.ToArray();
            if (eventWaitTypesArray.Length == 1)
            {
                LogWriter.LogWaitEvent(stateMachine.Id, stateName, eventWaitTypesArray[0]);
            }
            else
            {
                LogWriter.LogWaitEvent(stateMachine.Id, stateName, eventWaitTypesArray);
            }

            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Join);
            ResetProgramCounter(stateMachine);
        }

        /// <summary>
        /// Notifies that a state machine entered a state.
        /// </summary>
        internal void NotifyEnteredState(StateMachine stateMachine)
        {
            var stateName = stateMachine.CurrentStateName;
            LogWriter.LogStateTransition(stateMachine.Id, stateName, isEntry: true);
        }

        /// <summary>
        /// Notifies that a state machine exited a state.
        /// </summary>
        internal void NotifyExitedState(StateMachine stateMachine)
        {
            LogWriter.LogStateTransition(stateMachine.Id, stateMachine.CurrentStateName, isEntry: false);
        }

        /// <summary>
        /// Notifies that a state machine invoked an action.
        /// </summary>
        internal void NotifyInvokedOnEntryAction(StateMachine stateMachine, MethodInfo action, Event receivedEvent)
        {
            var stateName = stateMachine.CurrentStateName;
            LogWriter.LogExecuteAction(stateMachine.Id, stateName, stateName, action.Name);
        }

        /// <summary>
        /// Notifies that a state machine invoked an action.
        /// </summary>
        internal void NotifyInvokedOnExitAction(StateMachine stateMachine, MethodInfo action, Event receivedEvent)
        {
            var stateName = stateMachine.CurrentStateName;
            LogWriter.LogExecuteAction(stateMachine.Id, stateName, stateName, action.Name);
        }

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        internal void NotifyEnteredState(Monitor monitor)
        {
            var monitorState = monitor.CurrentStateName;
            LogWriter.LogMonitorStateTransition(monitor.GetType().FullName, monitorState, true, monitor.GetHotState());
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        internal void NotifyExitedState(Monitor monitor)
        {
            LogWriter.LogMonitorStateTransition(monitor.GetType().FullName,
                monitor.CurrentStateName, false, monitor.GetHotState());
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        internal void NotifyInvokedAction(Monitor monitor, MethodInfo action, string stateName, Event receivedEvent)
        {
            LogWriter.LogMonitorExecuteAction(monitor.GetType().FullName, stateName, action.Name);
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        internal void NotifyRaisedEvent(Monitor monitor, Event e)
        {
            var monitorState = monitor.CurrentStateName;
            LogWriter.LogMonitorRaiseEvent(monitor.GetType().FullName, monitorState, e);
        }
        
        /// <summary>
        /// Notifies that a monitor found an error.
        /// </summary>
        internal void NotifyMonitorError(Monitor monitor)
        {
            if (CheckerConfiguration.IsVerbose)
            {
                var monitorState = monitor.CurrentStateNameWithTemperature;
                LogWriter.LogMonitorError(monitor.GetType().FullName, monitorState, monitor.GetHotState());
            }
        }
        
        /// <summary>
        /// Tries to handle the specified dropped <see cref="Event"/>.
        /// </summary>
        internal void TryHandleDroppedEvent(Event e, StateMachineId id) => OnEventDropped?.Invoke(e, id);
        
        /// <inheritdoc/>
        public override TextWriter SetLogger(TextWriter logger) => LogWriter.SetLogger(logger);

        /// <summary>
        /// Sets the JsonLogger in LogWriter.cs
        /// </summary>
        /// <param name="jsonLogger">jsonLogger instance</param>
        public void SetJsonLogger(JsonWriter jsonLogger) => LogWriter.SetJsonLogger(jsonLogger);
        
        /// <summary>
        /// Use this method to register an <see cref="IStateMachineRuntimeLog"/>.
        /// </summary>
        public void RegisterLog(IStateMachineRuntimeLog log) => LogWriter.RegisterLog(log);

        /// <summary>
        /// Use this method to unregister a previously registered <see cref="IStateMachineRuntimeLog"/>.
        /// </summary>
        public void RemoveLog(IStateMachineRuntimeLog log) => LogWriter.RemoveLog(log);

        /// <summary>
        /// Get the coverage graph information (if any). This information is only available
        /// when <see cref="CheckerConfiguration.ReportActivityCoverage"/> is enabled.
        /// </summary>
        /// <returns>A new CoverageInfo object.</returns>
        public CoverageInfo GetCoverageInfo()
        {
            var result = CoverageInfo;
            if (result != null)
            {
                var builder = LogWriter.GetLogsOfType<StateMachineRuntimeLogGraphBuilder>().FirstOrDefault();
                if (builder != null)
                {
                    result.CoverageGraph = builder.SnapshotGraph(CheckerConfiguration.IsDgmlBugGraph);
                }

                var eventCoverage = LogWriter.GetLogsOfType<StateMachineRuntimeLogEventCoverage>().FirstOrDefault();
                if (eventCoverage != null)
                {
                    result.EventInfo = eventCoverage.EventCoverage;
                }
            }

            return result;
        }

        /// <summary>
        /// Reports state machines that are to be covered in coverage report.
        /// </summary>
        private void ReportActivityCoverageOfStateMachine(StateMachine stateMachine)
        {
            var name = stateMachine.GetType().FullName;
            if (CoverageInfo.IsMachineDeclared(name))
            {
                return;
            }
        
            // Fetch states.
            var states = stateMachine.GetAllStates();
            foreach (var state in states)
            {
                CoverageInfo.DeclareMachineState(name, state);
            }

            // Fetch registered events.
            var pairs = stateMachine.GetAllStateEventPairs();
            foreach (var tup in pairs)
            {
                CoverageInfo.DeclareStateEvent(name, tup.Item1, tup.Item2);
            }
        }

        /// <summary>
        /// Reports coverage for the specified monitor.
        /// </summary>
        private void ReportActivityCoverageOfMonitor(Monitor monitor)
        {
            var monitorName = monitor.GetType().FullName;
            if (CoverageInfo.IsMachineDeclared(monitorName))
            {
                return;
            }

            // Fetch states.
            var states = monitor.GetAllStates();

            foreach (var state in states)
            {
                CoverageInfo.DeclareMachineState(monitorName, state);
            }

            // Fetch registered events.
            var pairs = monitor.GetAllStateEventPairs();

            foreach (var tup in pairs)
            {
                CoverageInfo.DeclareStateEvent(monitorName, tup.Item1, tup.Item2);
            }
        }

        /// <summary>
        /// Resets the program counter of the specified stateMachine.
        /// </summary>
        private static void ResetProgramCounter(StateMachine stateMachine)
        {
            if (stateMachine != null)
            {
                (stateMachine.Manager as StateMachineManager).ProgramCounter = 0;
            }
        }

        /// <summary>
        /// Returns the current hashed state of the execution using the specified
        /// level of abstraction. The hash is updated in each execution step.
        /// </summary>
        [DebuggerStepThrough]
        internal int GetProgramState()
        {
            unchecked
            {
                var hash = 19;

                foreach (var operation in Scheduler.GetRegisteredOperations().OrderBy(op => op.Id))
                {
                    if (operation is StateMachineOperation stateMachineOperation)
                    {
                        hash *= 31 + stateMachineOperation.StateMachine.GetHashedState();
                    }
                }

                foreach (var monitor in Monitors)
                {
                    hash = (hash * 397) + monitor.GetHashedState();
                }

                return hash;
            }
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        internal override void WrapAndThrowException(Exception exception, string s, params object[] args)
        {
            var msg = string.Format(CultureInfo.InvariantCulture, s, args);
            var message = string.Format(CultureInfo.InvariantCulture,
                "Exception '{0}' was thrown in {1}: {2}\n" +
                "from location '{3}':\n" +
                "The stack trace is:\n{4}",
                exception.GetType(), msg, exception.Message, exception.Source, exception.StackTrace);

            Scheduler.NotifyAssertionFailure(message);
        }

        /// <summary>
        /// Waits until all stateMachines have finished execution.
        /// </summary>
        [DebuggerStepThrough]
        internal async Task WaitAsync()
        {
            await Scheduler.WaitAsync();
            IsRunning = false;
        }

        /// <inheritdoc/>
        protected internal override void RaiseOnFailureEvent(Exception exception)
        {
            if (exception is ExecutionCanceledException ||
                (exception is ActionExceptionFilterException ae && ae.InnerException is ExecutionCanceledException))
            {
                // Internal exception used during testing.
                return;
            }

            base.RaiseOnFailureEvent(exception);
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Monitors.Clear();
                StateMachineMap.Clear();
            }

            base.Dispose(disposing);
        }
    }
}