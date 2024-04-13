// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PChecker.Actors;
using PChecker.Actors.EventQueues;
using PChecker.Actors.EventQueues.Mocks;
using PChecker.Actors.Events;
using PChecker.Actors.Exceptions;
using PChecker.Actors.Managers;
using PChecker.Actors.Managers.Mocks;
using PChecker.Coverage;
using PChecker.Exceptions;
using PChecker.Feedback;
using PChecker.Random;
using PChecker.Runtime;
using PChecker.Specifications.Monitors;
using PChecker.SystematicTesting.Operations;
using PChecker.SystematicTesting.Strategies;
using PChecker.SystematicTesting.Strategies.Liveness;
using PChecker.SystematicTesting.Traces;
using Debug = PChecker.IO.Debugging.Debug;
using EventInfo = PChecker.Actors.Events.EventInfo;

namespace PChecker.SystematicTesting
{
    /// <summary>
    /// Runtime for controlling asynchronous operations.
    /// </summary>
    internal sealed class ControlledRuntime : ActorRuntime
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
        /// Map that stores all unique names and their corresponding actor ids.
        /// </summary>
        internal readonly ConcurrentDictionary<string, ActorId> NameValueToActorId;

        /// <summary>
        /// The root task id.
        /// </summary>
        internal readonly int? RootTaskId;

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
                    if (operation is ActorOperation actorOperation)
                    {
                        int operationHash = 31 + actorOperation.Actor.GetHashedState();
                        operationHash = (operationHash * 31) + actorOperation.Type.GetHashCode();
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
            IsExecutionControlled = true;

            RootTaskId = Task.CurrentId;
            NameValueToActorId = new ConcurrentDictionary<string, ActorId>();

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

        /// <inheritdoc/>
        public override ActorId CreateActorIdFromName(Type type, string name)
        {
            // It is important that all actor ids use the monotonically incrementing
            // value as the id during testing, and not the unique name.
            var id = new ActorId(type, name, this);
            return NameValueToActorId.GetOrAdd(name, id);
        }

        /// <inheritdoc/>
        public override ActorId CreateActor(Type type, Event initialEvent = null, Guid opGroupId = default) =>
            CreateActor(null, type, null, initialEvent, opGroupId);

        /// <inheritdoc/>
        public override ActorId CreateActor(Type type, string name, Event initialEvent = null, Guid opGroupId = default) =>
            CreateActor(null, type, name, initialEvent, opGroupId);

        /// <inheritdoc/>
        public override ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, Guid opGroupId = default)
        {
            Assert(id != null, "Cannot create an actor using a null actor id.");
            return CreateActor(id, type, null, initialEvent, opGroupId);
        }

        /// <inheritdoc/>
        public override Task<ActorId> CreateActorAndExecuteAsync(Type type, Event e = null, Guid opGroupId = default) =>
            CreateActorAndExecuteAsync(null, type, null, e, opGroupId);

        /// <inheritdoc/>
        public override Task<ActorId> CreateActorAndExecuteAsync(Type type, string name, Event e = null, Guid opGroupId = default) =>
            CreateActorAndExecuteAsync(null, type, name, e, opGroupId);

        /// <inheritdoc/>
        public override Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, Event e = null, Guid opGroupId = default)
        {
            Assert(id != null, "Cannot create an actor using a null actor id.");
            return CreateActorAndExecuteAsync(id, type, null, e, opGroupId);
        }

        /// <inheritdoc/>
        public override void SendEvent(ActorId targetId, Event e, Guid opGroupId = default)
        {
            var senderOp = Scheduler.GetExecutingOperation<ActorOperation>();
            SendEvent(targetId, e, senderOp?.Actor, opGroupId);
        }

        /// <inheritdoc/>
        public override Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Guid opGroupId = default)
        {
            var senderOp = Scheduler.GetExecutingOperation<ActorOperation>();
            return SendEventAndExecuteAsync(targetId, e, senderOp?.Actor, opGroupId);
        }

        /// <inheritdoc/>
        public override Guid GetCurrentOperationGroupId(ActorId currentActorId)
        {
            var callerOp = Scheduler.GetExecutingOperation<ActorOperation>();
            Assert(callerOp != null && currentActorId == callerOp.Actor.Id,
                "Trying to access the operation group id of {0}, which is not the currently executing actor.",
                currentActorId);
            return callerOp.Actor.OperationGroupId;
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

                    if (testMethod is Action<IActorRuntime> actionWithRuntime)
                    {
                        actionWithRuntime(this);
                    }
                    else if (testMethod is Action action)
                    {
                        action();
                    }
                    else if (testMethod is Func<IActorRuntime, Tasks.Task> functionWithRuntime)
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
        /// Creates a new actor of the specified <see cref="Type"/> and name, using the specified
        /// unbound actor id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        internal ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent = null,
            Guid opGroupId = default)
        {
            var creatorOp = Scheduler.GetExecutingOperation<ActorOperation>();
            return CreateActor(id, type, name, initialEvent, creatorOp?.Actor, opGroupId);
        }

        /// <inheritdoc/>
        internal override ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent, Actor creator,
            Guid opGroupId)
        {
            AssertExpectedCallerActor(creator, "CreateActor");

            var actor = CreateActor(id, type, name, creator, opGroupId);
            RunActorEventHandler(actor, initialEvent, true, null);
            return actor.Id;
        }

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, using the specified
        /// unbound actor id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled. The method returns only
        /// when the actor is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        internal Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, string name, Event initialEvent = null,
            Guid opGroupId = default)
        {
            var creatorOp = Scheduler.GetExecutingOperation<ActorOperation>();
            return CreateActorAndExecuteAsync(id, type, name, initialEvent, creatorOp?.Actor, opGroupId);
        }

        /// <inheritdoc/>
        internal override async Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, string name,
            Event initialEvent, Actor creator, Guid opGroupId)
        {
            AssertExpectedCallerActor(creator, "CreateActorAndExecuteAsync");
            Assert(creator != null, "Only an actor can call 'CreateActorAndExecuteAsync': avoid calling " +
                                    "it directly from the test method; instead call it through a test driver actor.");

            var actor = CreateActor(id, type, name, creator, opGroupId);
            RunActorEventHandler(actor, initialEvent, true, creator);

            // Wait until the actor reaches quiescence.
            await creator.ReceiveEventAsync(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).ActorId == actor.Id);
            return await Task.FromResult(actor.Id);
        }

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/>.
        /// </summary>
        private Actor CreateActor(ActorId id, Type type, string name, Actor creator, Guid opGroupId)
        {
            Assert(type.IsSubclassOf(typeof(Actor)), "Type '{0}' is not an actor.", type.FullName);

            // Using ulong.MaxValue because a Create operation cannot specify
            // the id of its target, because the id does not exist yet.
            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Create);
            ResetProgramCounter(creator);

            if (id is null)
            {
                id = new ActorId(type, name, this);
            }
            else
            {
                Assert(id.Runtime is null || id.Runtime == this, "Unbound actor id '{0}' was created by another runtime.", id.Value);
                Assert(id.Type == type.FullName, "Cannot bind actor id '{0}' of type '{1}' to an actor of type '{2}'.",
                    id.Value, id.Type, type.FullName);
                id.Bind(this);
            }

            // The operation group id of the actor is set using the following precedence:
            // (1) To the specified actor creation operation group id, if it is non-empty.
            // (2) To the operation group id of the creator actor, if it exists and is non-empty.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && creator != null)
            {
                opGroupId = creator.OperationGroupId;
            }

            var actor = ActorFactory.Create(type);
            IActorManager actorManager;
            if (actor is StateMachine stateMachine)
            {
                actorManager = new MockStateMachineManager(this, stateMachine, opGroupId);
            }
            else
            {
                actorManager = new MockActorManager(this, actor, opGroupId);
            }

            IEventQueue eventQueue = new MockEventQueue(actorManager, actor);
            actor.Configure(this, id, actorManager, eventQueue);
            actor.SetupEventHandlers();

            if (CheckerConfiguration.ReportActivityCoverage)
            {
                ReportActivityCoverageOfActor(actor);
            }

            var result = Scheduler.RegisterOperation(new ActorOperation(actor));
            Assert(result, "Actor id '{0}' is used by an existing or previously halted actor.", id.Value);
            if (actor is StateMachine)
            {
                LogWriter.LogCreateStateMachine(id, creator?.Id.Name, creator?.Id.Type);
            }
            else
            {
                LogWriter.LogCreateActor(id, creator?.Id.Name, creator?.Id.Type);
            }

            return actor;
        }

        /// <inheritdoc/>
        internal override void SendEvent(ActorId targetId, Event e, Actor sender, Guid opGroupId)
        {
            if (e is null)
            {
                var message = sender != null ?
                    string.Format("{0} is sending a null event.", sender.Id.ToString()) :
                    "Cannot send a null event.";
                Assert(false, message);
            }

            if (sender != null)
            {
                Assert(targetId != null, "{0} is sending event {1} to a null actor.", sender.Id, e);
            }
            else
            {
                Assert(targetId != null, "Cannot send event {1} to a null actor.", e);
            }

            AssertExpectedCallerActor(sender, "SendEvent");

            var enqueueStatus = EnqueueEvent(targetId, e, sender, opGroupId, out var target);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                RunActorEventHandler(target, null, false, null);
            }
        }

        /// <inheritdoc/>
        internal override async Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Actor sender, Guid opGroupId)
        {
            Assert(sender is StateMachine, "Only an actor can call 'SendEventAndExecuteAsync': avoid " +
                                           "calling it directly from the test method; instead call it through a test driver actor.");
            Assert(e != null, "{0} is sending a null event.", sender.Id);
            Assert(targetId != null, "{0} is sending event {1} to a null actor.", sender.Id, e);
            AssertExpectedCallerActor(sender, "SendEventAndExecuteAsync");

            var enqueueStatus = EnqueueEvent(targetId, e, sender, opGroupId, out var target);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                RunActorEventHandler(target, null, false, sender as StateMachine);

                // Wait until the actor reaches quiescence.
                await (sender as StateMachine).ReceiveEventAsync(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).ActorId == targetId);
                return true;
            }

            // EnqueueStatus.EventHandlerNotRunning is not returned by EnqueueEvent
            // (even when the actor was previously inactive) when the event e requires
            // no action by the actor (i.e., it implicitly handles the event).
            return enqueueStatus is EnqueueStatus.Dropped || enqueueStatus is EnqueueStatus.NextEventUnavailable;
        }

        /// <summary>
        /// Enqueues an event to the actor with the specified id.
        /// </summary>
        private EnqueueStatus EnqueueEvent(ActorId targetId, Event e, Actor sender, Guid opGroupId, out Actor target)
        {
            target = Scheduler.GetOperationWithId<ActorOperation>(targetId.Value)?.Actor;
            Assert(target != null,
                "Cannot send event '{0}' to actor id '{1}' that is not bound to an actor instance.",
                e.GetType().FullName, targetId.Value);

            Scheduler.ScheduledOperation.LastEvent = e;
            Scheduler.ScheduledOperation.LastSentReceiver = targetId.ToString();

            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Send);
            ResetProgramCounter(sender as StateMachine);

            // The operation group id of this operation is set using the following precedence:
            // (1) To the specified send operation group id, if it is non-empty.
            // (2) To the operation group id of the sender actor, if it exists and is non-empty.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && sender != null)
            {
                opGroupId = sender.OperationGroupId;
            }

            if (target.IsHalted)
            {
                LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                    (sender as StateMachine)?.CurrentStateName ?? string.Empty, e, opGroupId, isTargetHalted: true);
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
        /// Enqueues an event to the actor with the specified id.
        /// </summary>
        private EnqueueStatus EnqueueEvent(Actor actor, Event e, Actor sender, Guid opGroupId)
        {
            EventOriginInfo originInfo;

            string stateName = null;
            if (sender is StateMachine senderStateMachine)
            {
                originInfo = new EventOriginInfo(sender.Id, senderStateMachine.GetType().FullName,
                    NameResolver.GetStateNameForLogging(senderStateMachine.CurrentState));
                stateName = senderStateMachine.CurrentStateName;
            }
            else if (sender is Actor senderActor)
            {
                originInfo = new EventOriginInfo(sender.Id, senderActor.GetType().FullName, string.Empty);
            }
            else
            {
                // Message comes from the environment.
                originInfo = new EventOriginInfo(null, "Env", "Env");
            }

            var eventInfo = new EventInfo(e, originInfo);

            LogWriter.LogSendEvent(actor.Id, sender?.Id.Name, sender?.Id.Type, stateName,
                e, opGroupId, isTargetHalted: false);

            return actor.Enqueue(e, opGroupId, eventInfo);
        }

        /// <summary>
        /// Runs a new asynchronous event handler for the specified actor.
        /// This is a fire and forget invocation.
        /// </summary>
        /// <param name="actor">The actor that executes this event handler.</param>
        /// <param name="initialEvent">Optional event for initializing the actor.</param>
        /// <param name="isFresh">If true, then this is a new actor.</param>
        /// <param name="syncCaller">Caller actor that is blocked for quiscence.</param>
        private void RunActorEventHandler(Actor actor, Event initialEvent, bool isFresh, Actor syncCaller)
        {
            var op = Scheduler.GetOperationWithId<ActorOperation>(actor.Id.Value);
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
                        await actor.InitializeAsync(initialEvent);
                    }

                    await actor.RunEventHandlerAsync();
                    if (syncCaller != null)
                    {
                        EnqueueEvent(syncCaller, new QuiescentEvent(actor.Id), actor, actor.OperationGroupId);
                    }

                    if (!actor.IsHalted)
                    {
                        ResetProgramCounter(actor);
                    }

                    Debug.WriteLine("<ScheduleDebug> Completed operation {0} on task '{1}'.", actor.Id, Task.CurrentId);
                    op.OnCompleted();

                    // The actor is inactive or halted, schedule the next enabled operation.
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
        /// Asserts that the actor calling an actor method is also
        /// the actor that is currently executing.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        private void AssertExpectedCallerActor(Actor caller, string calledAPI)
        {
            if (caller is null)
            {
                return;
            }

            var op = Scheduler.GetExecutingOperation<ActorOperation>();
            if (op is null)
            {
                return;
            }

            Assert(op.Actor.Equals(caller), "{0} invoked {1} on behalf of {2}.",
                op.Actor.Id, calledAPI, caller.Id);
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
            var caller = Scheduler.GetExecutingOperation<ActorOperation>()?.Actor;
            if (caller is StateMachine callerStateMachine)
            {
                (callerStateMachine.Manager as MockStateMachineManager).ProgramCounter++;
            }
            else if (caller is Actor callerActor)
            {
                (callerActor.Manager as MockActorManager).ProgramCounter++;
            }

            var choice = Scheduler.GetNextNondeterministicBooleanChoice(maxValue);
            LogWriter.LogRandom(choice, callerName ?? caller?.Id.Name, callerType ?? caller?.Id.Type);
            return choice;
        }

        /// <inheritdoc/>
        internal override int GetNondeterministicIntegerChoice(int maxValue, string callerName, string callerType)
        {
            var caller = Scheduler.GetExecutingOperation<ActorOperation>()?.Actor;
            if (caller is StateMachine callerStateMachine)
            {
                (callerStateMachine.Manager as MockStateMachineManager).ProgramCounter++;
            }
            else if (caller is Actor callerActor)
            {
                (callerActor.Manager as MockActorManager).ProgramCounter++;
            }

            var choice = Scheduler.GetNextNondeterministicIntegerChoice(maxValue);
            LogWriter.LogRandom(choice, callerName ?? caller?.Id.Name, callerType ?? caller?.Id.Type);
            return choice;
        }

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

        /// <inheritdoc/>
        internal override void NotifyInvokedAction(Actor actor, MethodInfo action, string handlingStateName,
            string currentStateName, Event receivedEvent)
        {
            LogWriter.LogExecuteAction(actor.Id, handlingStateName, currentStateName, action.Name);
        }

        /// <inheritdoc/>
        internal override void NotifyDequeuedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            var op = Scheduler.GetOperationWithId<ActorOperation>(actor.Id.Value);

            // Skip `ReceiveEventAsync` if the last operation exited the previous event handler,
            // to avoid scheduling duplicate `ReceiveEventAsync` operations.
            if (op.SkipNextReceiveSchedulingPoint)
            {
                op.SkipNextReceiveSchedulingPoint = false;
            }
            else
            {
                Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Receive);
                ResetProgramCounter(actor);
            }

            var stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
            LogWriter.LogDequeueEvent(actor.Id, stateName, e);
        }

        /// <inheritdoc/>
        internal override void NotifyDefaultEventDequeued(Actor actor)
        {
            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Receive);
            ResetProgramCounter(actor);
        }

        /// <inheritdoc/>
        internal override void NotifyDefaultEventHandlerCheck(Actor actor)
        {
            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Default);
        }

        /// <inheritdoc/>
        internal override void NotifyRaisedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            var stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
            LogWriter.LogRaiseEvent(actor.Id, stateName, e);
        }

        /// <inheritdoc/>
        internal override void NotifyHandleRaisedEvent(Actor actor, Event e)
        {
            var stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
            LogWriter.LogHandleRaisedEvent(actor.Id, stateName, e);
        }

        /// <inheritdoc/>
        internal override void NotifyReceiveCalled(Actor actor)
        {
            AssertExpectedCallerActor(actor, "ReceiveEventAsync");
        }

        /// <inheritdoc/>
        internal override void NotifyReceivedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            var stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
            LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: true);
            var op = Scheduler.GetOperationWithId<ActorOperation>(actor.Id.Value);
            op.OnReceivedEvent();
        }

        /// <inheritdoc/>
        internal override void NotifyReceivedEventWithoutWaiting(Actor actor, Event e, EventInfo eventInfo)
        {
            var stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
            LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: false);
            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Receive);
            ResetProgramCounter(actor);
        }

        /// <inheritdoc/>
        internal override void NotifyWaitTask(Actor actor, Task task)
        {
            Assert(task != null, "{0} is waiting for a null task to complete.", actor.Id);

            var finished = task.IsCompleted || task.IsCanceled || task.IsFaulted;
            if (!finished)
            {
                Assert(finished,
                    "Controlled task '{0}' is trying to wait for an uncontrolled task or awaiter to complete. Please " +
                    "make sure to avoid using concurrency APIs (e.g. 'Task.Run', 'Task.Delay' or 'Task.Yield' from " +
                    "the 'System.Threading.Tasks' namespace) inside actor handlers. If you are using external libraries " +
                    "that are executing concurrently, you will need to mock them during testing.",
                    Task.CurrentId);
            }
        }

        /// <inheritdoc/>
        internal override void NotifyWaitEvent(Actor actor, IEnumerable<Type> eventTypes)
        {
            var stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
            var op = Scheduler.GetOperationWithId<ActorOperation>(actor.Id.Value);
            op.OnWaitEvent(eventTypes);

            var eventWaitTypesArray = eventTypes.ToArray();
            if (eventWaitTypesArray.Length == 1)
            {
                LogWriter.LogWaitEvent(actor.Id, stateName, eventWaitTypesArray[0]);
            }
            else
            {
                LogWriter.LogWaitEvent(actor.Id, stateName, eventWaitTypesArray);
            }

            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Join);
            ResetProgramCounter(actor);
        }

        /// <inheritdoc/>
        internal override void NotifyEnteredState(StateMachine stateMachine)
        {
            var stateName = stateMachine.CurrentStateName;
            LogWriter.LogStateTransition(stateMachine.Id, stateName, isEntry: true);
        }

        /// <inheritdoc/>
        internal override void NotifyExitedState(StateMachine stateMachine)
        {
            LogWriter.LogStateTransition(stateMachine.Id, stateMachine.CurrentStateName, isEntry: false);
        }

        /// <inheritdoc/>
        internal override void NotifyInvokedOnEntryAction(StateMachine stateMachine, MethodInfo action, Event receivedEvent)
        {
            var stateName = stateMachine.CurrentStateName;
            LogWriter.LogExecuteAction(stateMachine.Id, stateName, stateName, action.Name);
        }

        /// <inheritdoc/>
        internal override void NotifyInvokedOnExitAction(StateMachine stateMachine, MethodInfo action, Event receivedEvent)
        {
            var stateName = stateMachine.CurrentStateName;
            LogWriter.LogExecuteAction(stateMachine.Id, stateName, stateName, action.Name);
        }

        /// <inheritdoc/>
        internal override void NotifyEnteredState(Monitor monitor)
        {
            var monitorState = monitor.CurrentStateName;
            LogWriter.LogMonitorStateTransition(monitor.GetType().FullName, monitorState, true, monitor.GetHotState());
        }

        /// <inheritdoc/>
        internal override void NotifyExitedState(Monitor monitor)
        {
            LogWriter.LogMonitorStateTransition(monitor.GetType().FullName,
                monitor.CurrentStateName, false, monitor.GetHotState());
        }

        /// <inheritdoc/>
        internal override void NotifyInvokedAction(Monitor monitor, MethodInfo action, string stateName, Event receivedEvent)
        {
            LogWriter.LogMonitorExecuteAction(monitor.GetType().FullName, stateName, action.Name);
        }

        /// <inheritdoc/>
        internal override void NotifyRaisedEvent(Monitor monitor, Event e)
        {
            var monitorState = monitor.CurrentStateName;
            LogWriter.LogMonitorRaiseEvent(monitor.GetType().FullName, monitorState, e);
        }

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
                var builder = LogWriter.GetLogsOfType<ActorRuntimeLogGraphBuilder>().FirstOrDefault();
                if (builder != null)
                {
                    result.CoverageGraph = builder.SnapshotGraph(CheckerConfiguration.IsDgmlBugGraph);
                }

                var eventCoverage = LogWriter.GetLogsOfType<ActorRuntimeLogEventCoverage>().FirstOrDefault();
                if (eventCoverage != null)
                {
                    result.EventInfo = eventCoverage.EventCoverage;
                }
            }

            return result;
        }

        /// <summary>
        /// Reports actors that are to be covered in coverage report.
        /// </summary>
        private void ReportActivityCoverageOfActor(Actor actor)
        {
            var name = actor.GetType().FullName;
            if (CoverageInfo.IsMachineDeclared(name))
            {
                return;
            }

            if (actor is StateMachine stateMachine)
            {
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
            else
            {
                var fakeStateName = actor.GetType().Name;
                CoverageInfo.DeclareMachineState(name, fakeStateName);

                foreach (var eventId in actor.GetAllRegisteredEvents())
                {
                    CoverageInfo.DeclareStateEvent(name, fakeStateName, eventId);
                }
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
        /// Resets the program counter of the specified actor.
        /// </summary>
        private static void ResetProgramCounter(Actor actor)
        {
            if (actor is StateMachine stateMachine)
            {
                (stateMachine.Manager as MockStateMachineManager).ProgramCounter = 0;
            }
            else if (actor != null)
            {
                (actor.Manager as MockActorManager).ProgramCounter = 0;
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
                    if (operation is ActorOperation actorOperation)
                    {
                        hash *= 31 + actorOperation.Actor.GetHashedState();
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
        /// Waits until all actors have finished execution.
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
            }

            base.Dispose(disposing);
        }
    }
}