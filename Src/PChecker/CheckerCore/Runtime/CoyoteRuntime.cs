// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PChecker.Actors.Events;
using PChecker.Exceptions;
using PChecker.Random;
using Monitor = PChecker.Specifications.Monitors.Monitor;

namespace PChecker.Runtime
{
    /// <summary>
    /// Runtime for executing asynchronous operations.
    /// </summary>
    internal abstract class CoyoteRuntime : ICoyoteRuntime
    {
        /// <summary>
        /// Provides access to the runtime associated with each asynchronous control flow.
        /// </summary>
        /// <remarks>
        /// In testing mode, each testing schedule uses a unique runtime instance. To safely
        /// retrieve it from static methods, we store it in each asynchronous control flow.
        /// </remarks>
        private static readonly AsyncLocal<CoyoteRuntime> AsyncLocalInstance = new AsyncLocal<CoyoteRuntime>();

        /// <summary>
        /// The currently executing runtime.
        /// </summary>
        internal static CoyoteRuntime Current => AsyncLocalInstance.Value ??
                                                 (IsExecutionControlled ? throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                         "Uncontrolled task '{0}' invoked a runtime method. Please make sure to avoid using concurrency APIs " +
                                                         "(e.g. 'Task.Run', 'Task.Delay' or 'Task.Yield' from the 'System.Threading.Tasks' namespace) inside " +
                                                         "actor handlers or controlled tasks. If you are using external libraries that are executing concurrently, " +
                                                         "you will need to mock them during testing.",
                                                         Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>")) :
                                                     RuntimeFactory.InstalledRuntime);

        /// <summary>
        /// If true, the program execution is controlled by the runtime to
        /// explore interleavings and sources of nondeterminism, else false.
        /// </summary>
        internal static bool IsExecutionControlled { get; private protected set; } = false;

        /// <summary>
        /// The checkerConfiguration used by the runtime.
        /// </summary>
        protected internal readonly CheckerConfiguration CheckerConfiguration;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        protected readonly List<Monitor> Monitors;

        /// <summary>
        /// Responsible for generating random values.
        /// </summary>
        protected readonly IRandomValueGenerator ValueGenerator;

        /// <summary>
        /// Monotonically increasing operation id counter.
        /// </summary>
        private long OperationIdCounter;

        /// <summary>
        /// Records if the runtime is running.
        /// </summary>
        protected internal volatile bool IsRunning;

        /// <summary>
        /// Used to log text messages. Use <see cref="SetLogger"/>
        /// to replace the logger with a custom one.
        /// </summary>
        public abstract TextWriter Logger { get; }

        /// <summary>
        /// Callback that is fired when the Coyote program throws an exception which includes failed assertions.
        /// </summary>
        public event OnFailureHandler OnFailure;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoyoteRuntime"/> class.
        /// </summary>
        protected CoyoteRuntime(CheckerConfiguration checkerConfiguration, IRandomValueGenerator valueGenerator)
        {
            CheckerConfiguration = checkerConfiguration;
            Monitors = new List<Monitor>();
            ValueGenerator = valueGenerator;
            OperationIdCounter = 0;
            IsRunning = true;
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
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        public bool RandomBoolean() => GetNondeterministicBooleanChoice(2, null, null);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing. The value is used to generate a number
        /// in the range [0..maxValue), where 0 triggers true.
        /// </summary>
        public bool RandomBoolean(int maxValue) => GetNondeterministicBooleanChoice(maxValue, null, null);

        /// <summary>
        /// Returns a nondeterministic integer, that can be controlled during
        /// analysis or testing. The value is used to generate an integer in
        /// the range [0..maxValue).
        /// </summary>
        public int RandomInteger(int maxValue) => GetNondeterministicIntegerChoice(maxValue, null, null);

        /// <summary>
        /// Returns the next available unique operation id.
        /// </summary>
        /// <returns>Value representing the next available unique operation id.</returns>
        internal ulong GetNextOperationId() =>
            // Atomically increments and safely wraps the value into an unsigned long.
            (ulong)Interlocked.Increment(ref OperationIdCounter) - 1;

        /// <summary>
        /// Tries to create a new <see cref="Specifications.Monitors.Monitor"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal abstract void TryCreateMonitor(Type type);

        /// <summary>
        /// Invokes the specified <see cref="Specifications.Monitors.Monitor"/> with the specified <see cref="Event"/>.
        /// </summary>
        internal virtual void Monitor(Type type, Event e, string senderName, string senderType, string senderState)
        {
            Monitor monitor = null;

            lock (Monitors)
            {
                foreach (var m in Monitors)
                {
                    if (m.GetType() == type)
                    {
                        monitor = m;
                        break;
                    }
                }
            }

            if (monitor != null)
            {
                lock (monitor)
                {
                    monitor.MonitorEvent(e, senderName, senderType, senderState);
                }
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public virtual void Assert(bool predicate)
        {
            if (!predicate)
            {
                throw new AssertionFailureException("Detected an assertion failure.");
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public virtual void Assert(bool predicate, string s, object arg0)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString()));
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public virtual void Assert(bool predicate, string s, object arg0, object arg1)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString(), arg1?.ToString()));
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public virtual void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString(), arg1?.ToString(), arg2?.ToString()));
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public virtual void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, args));
            }
        }

        /// <summary>
        /// Returns a controlled nondeterministic boolean choice.
        /// </summary>
        internal abstract bool GetNondeterministicBooleanChoice(int maxValue, string callerName, string callerType);

        /// <summary>
        /// Returns a controlled nondeterministic integer choice.
        /// </summary>
        internal abstract int GetNondeterministicIntegerChoice(int maxValue, string callerName, string callerType);

        /// <summary>
        /// Assigns the specified runtime as the default for the current asynchronous control flow.
        /// </summary>
        internal static void AssignAsyncControlFlowRuntime(CoyoteRuntime runtime) => AsyncLocalInstance.Value = runtime;

        /// <summary>
        /// Use this method to override the default <see cref="TextWriter"/> for logging messages.
        /// </summary>
        public abstract TextWriter SetLogger(TextWriter logger);

        /// <summary>
        /// Raises the <see cref="OnFailure"/> event with the specified <see cref="Exception"/>.
        /// </summary>
        protected internal virtual void RaiseOnFailureEvent(Exception exception)
        {
            OnFailure?.Invoke(exception);
        }

        /// <summary>
        /// Throws an <see cref="AssertionFailureException"/> exception
        /// containing the specified exception.
        /// </summary>
        internal virtual void WrapAndThrowException(Exception exception, string s, params object[] args)
        {
            var msg = string.Format(CultureInfo.InvariantCulture, s, args);
            var message = string.Format(CultureInfo.InvariantCulture,
                "Exception '{0}' was thrown in {1}: {2}\n" +
                "from location '{3}':\n" +
                "The stack trace is:\n{4}",
                exception.GetType(), msg, exception.Message, exception.Source, exception.StackTrace);

            throw new AssertionFailureException(message, exception);
        }

        /// <summary>
        /// Terminates the runtime and notifies each active actor to halt execution.
        /// </summary>
        public void Stop() => IsRunning = false;

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                OperationIdCounter = 0;
            }
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}