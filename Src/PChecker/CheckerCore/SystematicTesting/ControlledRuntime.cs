// Copyright (c) Microsoft Corporation.
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
using System.Threading;
using System.Threading.Tasks;
using PChecker.Coverage;
using PChecker.Exceptions;
using PChecker.Random;
using PChecker.Runtime;
using PChecker.Runtime.Events;
using PChecker.Runtime.Logging;
using PChecker.Runtime.StateMachines;
using PChecker.Runtime.StateMachines.EventQueues;
using PChecker.Runtime.StateMachines.Exceptions;
using PChecker.Runtime.StateMachines.Managers;
using PChecker.SystematicTesting.Operations;
using PChecker.SystematicTesting.Strategies;
using PChecker.SystematicTesting.Strategies.Liveness;
using PChecker.SystematicTesting.Traces;
using Debug = PChecker.IO.Debugging.Debug;
using EventInfo = PChecker.Runtime.Events.EventInfo;
using Monitor = PChecker.Runtime.Specifications.Monitor;
using PMachineValue = PChecker.Runtime.Values.PMachineValue;

namespace PChecker.SystematicTesting
{
    /// <summary>
    /// Runtime for controlling asynchronous operations.
    /// </summary>
    public sealed class ControlledRuntime : IDisposable
    {
        private static ControlledRuntime CreateWithConfiguration(CheckerConfiguration checkerConfiguration)
        {
            if (checkerConfiguration is null)
            {
                checkerConfiguration = CheckerConfiguration.Create();
            }

            var valueGenerator = new RandomValueGenerator(checkerConfiguration);
            return new ControlledRuntime(checkerConfiguration, valueGenerator);
        }
        
        /// <summary>
        /// The checkerConfiguration used by the runtime.
        /// </summary>
        internal readonly CheckerConfiguration CheckerConfiguration;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        readonly List<Monitor> Monitors;

        /// <summary>
        /// Monotonically increasing operation id counter.
        /// </summary>
        private long OperationIdCounter;

        /// <summary>
        /// Records if the runtime is running.
        /// </summary>
        internal volatile bool IsRunning;

        /// <summary>
        /// Callback that is fired when the program throws an exception which includes failed assertions.
        /// </summary>
        public event OnFailureHandler OnFailure;
        
        /// <summary>
        /// The asynchronous operation scheduler.
        /// </summary>
        internal readonly OperationScheduler Scheduler;

        /// <summary>
        /// Data structure containing information regarding testing coverage.
        /// </summary>
        internal CoverageInfo CoverageInfo;

        /// <summary>
        /// Map that stores all unique names and their corresponding state machine ids.
        /// </summary>
        internal readonly ConcurrentDictionary<string, StateMachineId> NameValueToStateMachineId;

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
        /// Callback that is fired when an event is dropped.
        /// </summary>
        public event OnEventDroppedHandler OnEventDropped;
        
        /// <summary>
        /// Responsible for writing to all registered <see cref="IControlledRuntimeLog"/> objects.
        /// </summary>
        internal LogWriter LogWriter { get; private set; }

        /// <summary>
        /// Used to log text messages. Use <see cref="ControlledRuntime.SetLogger"/>
        /// to replace the logger with a custom one.
        /// </summary>
        public TextWriter Logger => LogWriter.Logger;

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
        internal ControlledRuntime(CheckerConfiguration checkerConfiguration, ISchedulingStrategy strategy)
        {
            CheckerConfiguration = checkerConfiguration;
            Monitors = new List<Monitor>();
            OperationIdCounter = 0;
            IsRunning = true;
            
            StateMachineMap = new ConcurrentDictionary<StateMachineId, StateMachine>();
            LogWriter = new LogWriter(checkerConfiguration);

            NameValueToStateMachineId = new ConcurrentDictionary<string, StateMachineId>();

            CoverageInfo = new CoverageInfo();

            var scheduleTrace = new ScheduleTrace();
            if (checkerConfiguration.IsLivenessCheckingEnabled)
            {
                strategy = new TemperatureCheckingStrategy(checkerConfiguration, Monitors, strategy);
            }

            Scheduler = new OperationScheduler(this, strategy, scheduleTrace, CheckerConfiguration);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledRuntime"/> class.
        /// </summary>
        internal ControlledRuntime(CheckerConfiguration checkerConfiguration,
            IRandomValueGenerator valueGenerator)
        {
            StateMachineMap = new ConcurrentDictionary<StateMachineId, StateMachine>();
            LogWriter = new LogWriter(checkerConfiguration);

            NameValueToStateMachineId = new ConcurrentDictionary<string, StateMachineId>();

            CoverageInfo = new CoverageInfo();
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
        public StateMachineId CreateStateMachine(Type type, Event initialEvent = null) =>
            CreateStateMachine(null, type, null, initialEvent);

        /// <summary>
        /// Creates a new state machine of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled.
        /// </summary>
        public StateMachineId CreateStateMachine(Type type, string name, Event initialEvent = null) =>
            CreateStateMachine(null, type, name, initialEvent);

        /// <summary>
        /// Creates a new state machine of the specified type, using the specified <see cref="StateMachineId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new state machine, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        public StateMachineId CreateStateMachine(StateMachineId id, Type type, Event initialEvent = null)
        {
            Assert(id != null, "Cannot create an state machine using a null state machine id.");
            return CreateStateMachine(id, type, null, initialEvent);
        }

        /// <summary>
        /// Runs the specified test method, and then every operation it transitively creates,
        /// until the schedule terminates.
        /// </summary>
        internal void RunTest(Delegate testMethod, string testName)
        {
            testName = string.IsNullOrEmpty(testName) ? string.Empty : $" '{testName}'";
            Logger.WriteLine($"<TestLog> Running test{testName}.");
            Assert(testMethod != null, "Unable to execute a null test method.");

            var op = new TaskOperation(GetNextOperationId());
            Scheduler.RegisterOperation(op);
            op.OnEnabled();
            op.Continuation = () =>
            {
                try
                {
                    if (testMethod is Action<ControlledRuntime> actionWithRuntime)
                    {
                        actionWithRuntime(this);
                    }
                    else if (testMethod is Action action)
                    {
                        action();
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unsupported test delegate of type '{testMethod.GetType()}'.");
                    }

                    op.OnCompleted();
                    _ = Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Stop);
                }
                catch (Exception ex)
                {
                    ProcessUnhandledExceptionInOperation(op, ex);
                }
            };

            Scheduler.Run();
        }

        /// <summary>
        /// Returns the next available unique operation id.
        /// </summary>
        /// <returns>Value representing the next available unique operation id.</returns>
        internal ulong GetNextOperationId() =>
            // Atomically increments and safely wraps the value into an unsigned long.
            (ulong)Interlocked.Increment(ref OperationIdCounter) - 1;

        /// <summary>
        /// Creates a new state machine from the test entry method, or on behalf of the executing
        /// state machine when called from its foreign code.
        /// </summary>
        /// <remarks>
        /// The test entry method and foreign code run synchronously, so they cannot be suspended at
        /// a scheduling point. The test entry takes the Create scheduling point only while it is the
        /// only enabled operation, where the scheduler cannot switch away from it (this keeps the
        /// usual single create of a P test identical to master). Later creates skip it: the machines
        /// they create start running once the test entry returns.
        /// </remarks>
        internal StateMachineId CreateStateMachine(StateMachineId id, Type type, string name, Event initialEvent = null)
        {
            var creator = Scheduler.GetExecutingOperation<StateMachineOperation>()?.StateMachine;
            if (creator != null)
            {
                return CreateStateMachine(id, type, name, initialEvent, creator);
            }

            CheckSchedulerIsRunning();
            if (Scheduler.IsOnlyEnabledOperation(Scheduler.ScheduledOperation) &&
                !Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Create).IsCompleted)
            {
                // The schedule ended at this point (e.g. the steps bound was reached).
                Assert(!Scheduler.IsRunning, "The test entry method was suspended at a scheduling point.");
                return null;
            }

            var stateMachine = CreateStateMachine(id, type, name, (StateMachine)null);
            RunStateMachineEventHandler(stateMachine, initialEvent, true);
            return stateMachine.Id;
        }

        /// <summary>
        /// Creates a new <see cref="StateMachine"/> of the specified <see cref="Type"/> on behalf of
        /// the specified creator, without a scheduling point. Used by foreign code, which cannot be
        /// suspended.
        /// </summary>
        internal StateMachineId CreateStateMachine(StateMachineId id, Type type, string name, Event initialEvent,
            StateMachine creator)
        {
            AssertCallerIsExecuting(creator, "CreateInterface");

            var stateMachine = CreateStateMachine(id, type, name, creator);
            RunStateMachineEventHandler(stateMachine, initialEvent, true);
            return stateMachine.Id;
        }

        /// <summary>
        /// Creates a new <see cref="StateMachine"/> of the specified <see cref="Type"/> on behalf of
        /// the specified creator.
        /// </summary>
        internal async ValueTask<StateMachineId> CreateStateMachineAsync(StateMachineId id, Type type, string name,
            Event initialEvent, StateMachine creator)
        {
            AssertExpectedCallerStateMachine(creator, "CreateStateMachine");

            await Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Create);
            ResetProgramCounter(creator);

            var stateMachine = CreateStateMachine(id, type, name, creator);
            RunStateMachineEventHandler(stateMachine, initialEvent, true);
            return stateMachine.Id;
        }

        /// <summary>
        /// Creates a new state machine of the specified <see cref="Type"/>.
        /// </summary>
        private StateMachine CreateStateMachine(StateMachineId id, Type type, string name, StateMachine creator)
        {
            Assert(type.IsSubclassOf(typeof(StateMachine)), "Type '{0}' is not a state machine.", type.FullName);

            if (id is null)
            {
                id = new StateMachineId(type, name, this);
            }
            
            var stateMachine = Create(type);
            IStateMachineManager stateMachineManager = new StateMachineManager(this, stateMachine);

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
        internal async ValueTask SendEventAsync(StateMachineId targetId, Event e, StateMachine sender)
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

            var target = Scheduler.GetOperationWithId<StateMachineOperation>(targetId.Value)?.StateMachine;
            Assert(target != null,
                "Cannot send event '{0}' to state machine id '{1}' that is not bound to an state machine instance.",
                e.GetType().FullName, targetId.Value);

            Scheduler.ScheduledOperation.MessageReceiver = targetId.ToString();

            await Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Send);
            ResetProgramCounter(sender);

            DeliverEvent(target, e, sender);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a state machine without a scheduling point.
        /// Used by foreign code, which cannot be suspended.
        /// </summary>
        internal void SendEvent(StateMachineId targetId, Event e, StateMachine sender)
        {
            AssertCallerIsExecuting(sender, "SendEvent");

            var target = Scheduler.GetOperationWithId<StateMachineOperation>(targetId.Value)?.StateMachine;
            Assert(target != null,
                "Cannot send event '{0}' to state machine id '{1}' that is not bound to an state machine instance.",
                e.GetType().FullName, targetId.Value);

            DeliverEvent(target, e, sender);
        }

        /// <summary>
        /// Enqueues the event to the target, or drops it if the target has halted.
        /// </summary>
        private void DeliverEvent(StateMachine target, Event e, StateMachine sender)
        {
            if (target.IsHalted)
            {
                LogWriter.LogSendEvent(target.Id, sender?.Id.Name, sender?.Id.Type,
                    (sender)?.CurrentStateName ?? string.Empty, e, isTargetHalted: true);
                TryHandleDroppedEvent(e, target.Id);
                return;
            }

            var enqueueStatus = EnqueueEvent(target, e, sender);
            if (enqueueStatus == EnqueueStatus.Dropped)
            {
                TryHandleDroppedEvent(e, target.Id);
            }
            else if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                RunStateMachineEventHandler(target, null, false);
            }
        }

        /// <summary>
        /// Enqueues an event to the state machine with the specified id.
        /// </summary>
        private EnqueueStatus EnqueueEvent(StateMachine stateMachine, Event e, StateMachine sender)
        {
            // Directly use sender as a StateMachine
            var originInfo = new EventOriginInfo(sender.Id, sender.GetType().FullName,
                sender.CurrentState.GetType().Name);

            var eventInfo = new EventInfo(e, originInfo, sender.VectorTime);

            LogWriter.LogSendEvent(stateMachine.Id, sender.Id.Name, sender.Id.Type, sender.CurrentStateName,
                e, isTargetHalted: false);
    
            return stateMachine.Enqueue(e, eventInfo);
        }

        /// <summary>
        /// Enables the operation of the specified state machine, so that the scheduler
        /// can start running its event handler.
        /// </summary>
        /// <param name="stateMachine">The state machine that executes this event handler.</param>
        /// <param name="initialEvent">Optional event for initializing the state machine.</param>
        /// <param name="isFresh">If true, then this is a new state machine.</param>
        private void RunStateMachineEventHandler(StateMachine stateMachine, Event initialEvent, bool isFresh)
        {
            var op = Scheduler.GetOperationWithId<StateMachineOperation>(stateMachine.Id.Value);
            op.OnEnabled();
            op.Continuation = () => _ = RunStateMachineEventHandlerAsync(op, stateMachine, initialEvent, isFresh);
        }

        private async Task RunStateMachineEventHandlerAsync(StateMachineOperation op, StateMachine stateMachine,
            Event initialEvent, bool isFresh)
        {
            try
            {
                if (isFresh)
                {
                    await stateMachine.InitializeAsync(initialEvent);
                }

                await stateMachine.RunEventHandlerAsync();

                if (!stateMachine.IsHalted)
                {
                    ResetProgramCounter(stateMachine);
                }

                op.OnCompleted();

                // The state machine is inactive or halted, schedule the next enabled operation.
                _ = Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Stop);
            }
            catch (Exception ex)
            {
                ProcessUnhandledExceptionInOperation(op, ex);
            }
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
        
        /// <summary>
        /// Registers a new specification monitor of the specified <see cref="Type"/>.
        /// </summary>
        public void RegisterMonitor<T>()
            where T : Monitor =>
            TryCreateMonitor(typeof(T));

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        public void Monitor<T>(Event e)
            where T : Monitor
        {
            // If the event is null then report an error and exit.
            Assert(e != null, "Cannot monitor a null event.");
            Monitor(typeof(T), e, null, null, null);
        }

        /// <summary>
        /// Tries to create a new <see cref="Runtime.Specifications.Monitor"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal void TryCreateMonitor(Type type)
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

        /// <summary>
        /// Invokes the specified <see cref="Runtime.Specifications.Monitor"/> with the specified <see cref="Event"/>.
        /// </summary>
        internal void Monitor(Type type, Event e, string senderName, string senderType, string senderStateName)
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

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        public void Assert(bool predicate)
        {
            if (!predicate)
            {
                Scheduler.NotifyAssertionFailure("Detected an assertion failure.");
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        public void Assert(bool predicate, string s, object arg0)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString());
                Scheduler.NotifyAssertionFailure(msg);
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        public void Assert(bool predicate, string s, object arg0, object arg1)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString(), arg1?.ToString());
                Scheduler.NotifyAssertionFailure(msg);
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        public void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString(), arg1?.ToString(), arg2?.ToString());
                Scheduler.NotifyAssertionFailure(msg);
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        public void Assert(bool predicate, string s, params object[] args)
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

            // A caller running outside of the scheduler would otherwise be reported as a bug.
            Scheduler.CheckNoPendingContinuation();

            var op = Scheduler.GetExecutingOperation<StateMachineOperation>();
            if (op is null)
            {
                return;
            }

            Assert(op.StateMachine.Equals(caller), "{0} invoked {1} on behalf of {2}.",
                op.StateMachine.Id, calledAPI, caller.Id);
        }

        /// <summary>
        /// Asserts that the caller of a runtime method that takes no scheduling point (i.e. one meant
        /// for foreign code) is the state machine that is currently executing. Acting on behalf of
        /// any other state machine, e.g. from the test entry method, would bypass the scheduler.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        private void AssertCallerIsExecuting(StateMachine caller, string calledAPI)
        {
            CheckSchedulerIsRunning();
            Scheduler.CheckNoPendingContinuation();
            var op = Scheduler.GetExecutingOperation<StateMachineOperation>();
            if (op is null || !op.StateMachine.Equals(caller))
            {
                Assert(false, "{0} invoked {1} while {2} is executing. Foreign code may only call {1} on the " +
                              "state machine that called it.", caller.Id, calledAPI,
                    Scheduler.ScheduledOperation?.Name);
            }
        }

        /// <summary>
        /// Unwinds the caller if the schedule has ended. Runtime methods that take no scheduling point
        /// use it to stop acting once the scheduler has stopped.
        /// </summary>
        private void CheckSchedulerIsRunning()
        {
            if (!Scheduler.IsRunning)
            {
                throw new ExecutionCanceledException();
            }
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
                // Coverage (scenario) monitors are exempt: an unsatisfied scenario left in
                // a hot state is uncovered, not a liveness bug.
                if (PModule.coverageMonitors.Contains(monitor.GetType()))
                {
                    continue;
                }

                if (monitor.IsInHotState(out var stateName))
                {
                    var message = string.Format(CultureInfo.InvariantCulture,
                        "{0} detected liveness bug in hot state '{1}' at the end of program execution.",
                        monitor.GetType().FullName, stateName);
                    Scheduler.NotifyAssertionFailure(message, killTasks: false, cancelExecution: false);
                }
            }
        }
        
        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        public bool RandomBoolean() => GetNondeterministicBooleanChoice(2, null, null);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing. The value is used to generate a number
        /// in the range [0..maxValue), where 0 triggers true.
        /// </summary>
        internal bool GetNondeterministicBooleanChoice(int maxValue, string callerName, string callerType)
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

        /// <summary>
        /// Returns a nondeterministic integer, that can be controlled during
        /// analysis or testing. The value is used to generate an integer in
        /// the range [0..maxValue).
        /// </summary>
        internal int GetNondeterministicIntegerChoice(int maxValue, string callerName, string callerType)
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
        internal async ValueTask NotifyDequeuedEventAsync(StateMachine stateMachine, Event e, EventInfo eventInfo)
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
                await Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Receive);
                ResetProgramCounter(stateMachine);
            }

            var stateName = stateMachine.CurrentStateName;
            // stateMachine.VectorTime is the receive-event timestamp here: NotifyDequeuedEvent
            // is called after StateMachine merges the delivered event's clock (StateMachine.cs).
            LogWriter.LogDequeueEvent(stateMachine.Id, stateName, e,
                eventInfo?.OriginInfo?.SenderStateMachineId, stateMachine.VectorTime);
        }

        /// <summary>
        /// Notifies that a state machine dequeued the default <see cref="Event"/>.
        /// </summary>
        internal async ValueTask NotifyDefaultEventDequeuedAsync(StateMachine stateMachine)
        {
            await Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Receive);
            ResetProgramCounter(stateMachine);
        }

        /// <summary>
        /// Notifies that the inbox of the specified state machine is about to be
        /// checked to see if the default event handler should fire.
        /// </summary>
        internal SchedulingPoint NotifyDefaultEventHandlerCheck(StateMachine stateMachine) =>
            Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Default);

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
        internal async ValueTask NotifyReceivedEventWithoutWaitingAsync(StateMachine stateMachine, Event e, EventInfo eventInfo)
        {
            var stateName = stateMachine.CurrentStateName;
            LogWriter.LogReceiveEvent(stateMachine.Id, stateName, e, wasBlocked: false);
            await Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Receive);
            ResetProgramCounter(stateMachine);
        }

        /// <summary>
        /// Notifies that a state machine is waiting to receive an event of one of the specified types.
        /// </summary>
        internal async ValueTask NotifyWaitEventAsync(StateMachine stateMachine, IEnumerable<Type> eventTypes)
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

            await Scheduler.ScheduleNextEnabledOperation(AsyncOperationType.Join);
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
        public TextWriter SetLogger(TextWriter logger) => LogWriter.SetLogger(logger);

        /// <summary>
        /// Sets the JsonLogger in LogWriter.cs
        /// </summary>
        /// <param name="jsonLogger">jsonLogger instance</param>
        public void SetJsonLogger(JsonWriter jsonLogger) => LogWriter.SetJsonLogger(jsonLogger);
        
        /// <summary>
        /// Use this method to register an <see cref="IControlledRuntimeLog"/>.
        /// </summary>
        public void RegisterLog(IControlledRuntimeLog log) => LogWriter.RegisterLog(log);

        /// <summary>
        /// Use this method to unregister a previously registered <see cref="IControlledRuntimeLog"/>.
        /// </summary>
        public void RemoveLog(IControlledRuntimeLog log) => LogWriter.RemoveLog(log);

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
                var builder = LogWriter.GetLogsOfType<ControlledRuntimeLogGraphBuilder>().FirstOrDefault();
                if (builder != null)
                {
                    result.CoverageGraph = builder.SnapshotGraph(CheckerConfiguration.IsDgmlBugGraph);
                }

                var eventCoverage = LogWriter.GetLogsOfType<ControlledRuntimeLogEventCoverage>().FirstOrDefault();
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

        /// <summary>
        /// Throws an <see cref="AssertionFailureException"/> exception
        /// containing the specified exception.
        /// </summary>
        [DebuggerStepThrough]
        internal void WrapAndThrowException(Exception exception, string s, params object[] args)
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
        /// Terminates the runtime and notifies each active state machine to halt execution.
        /// </summary>
        public void Stop() => IsRunning = false;

        /// <summary>
        /// Raises the <see cref="OnFailure"/> event with the specified <see cref="Exception"/>.
        /// </summary>
        internal void RaiseOnFailureEvent(Exception exception)
        {
            if (exception is ExecutionCanceledException ||
                (exception is ActionExceptionFilterException ae && ae.InnerException is ExecutionCanceledException))
            {
                // Internal exception used during testing.
                return;
            }

            OnFailure?.Invoke(exception);
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        [DebuggerStepThrough]
        void Dispose(bool disposing)
        {
            if (disposing)
            {
                Monitors.Clear();
                StateMachineMap.Clear();
            }

            if (disposing)
            {
                OperationIdCounter = 0;
            }
        }
        
        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        [DebuggerStepThrough]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}