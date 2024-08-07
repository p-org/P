// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PChecker.Actors.EventQueues;
using PChecker.Actors.Events;
using PChecker.Actors.Exceptions;
using PChecker.Actors.Handlers;
using PChecker.Actors.Logging;
using PChecker.Actors.Managers;
using PChecker.Exceptions;
using PChecker.IO.Debugging;
using EventInfo = PChecker.Actors.Events.EventInfo;

// namespace PChecker.Actors
// {
//     /// <summary>
//     /// Type that implements an actor. Inherit from this class to declare a custom actor.
//     /// </summary>
//     public abstract class Actor
//     {
        // /// <summary>
        // /// Cache of actor types to a map of event types to action declarations.
        // /// </summary>
        // private static readonly ConcurrentDictionary<Type, Dictionary<Type, MethodInfo>> ActionCache =
        //     new ConcurrentDictionary<Type, Dictionary<Type, MethodInfo>>();
        //
        // /// <summary>
        // /// A set of lockable objects used to protect static initialization of the ActionCache while
        // /// also enabling multithreaded initialization of different Actor types.
        // /// </summary>
        // private static readonly ConcurrentDictionary<Type, object> ActionCacheLocks =
        //     new ConcurrentDictionary<Type, object>();
        //
        // /// <summary>
        // /// The runtime that executes this actor.
        // /// </summary>
        // internal ActorRuntime Runtime { get; private set; }
        //
        // /// <summary>
        // /// Unique id that identifies this actor.
        // /// </summary>
        // protected internal ActorId Id { get; private set; }
        //
        // /// <summary>
        // /// Manages the actor.
        // /// </summary>
        // internal IActorManager Manager { get; private set; }
        //
        // /// <summary>
        // /// The inbox of the actor. Incoming events are enqueued here.
        // /// Events are dequeued to be processed.
        // /// </summary>
        // private protected IEventQueue Inbox;

        // /// <summary>
        // /// Map from event types to cached action delegates.
        // /// </summary>
        // private protected readonly Dictionary<Type, CachedDelegate> ActionMap;

        // /// <summary>
        // /// The current status of the actor. It is marked volatile as
        // /// the runtime can read it concurrently.
        // /// </summary>
        // private protected volatile Status CurrentStatus;

        // /// <summary>
        // /// Gets the name of the current state, if there is one.
        // /// </summary>
        // internal string CurrentStateName { get; private protected set; }

        // /// <summary>
        // /// Checks if the actor is halted.
        // /// </summary>
        // internal bool IsHalted => CurrentStatus is Status.Halted;

        // /// <summary>
        // /// Checks if a default handler is available.
        // /// </summary>
        // internal bool IsDefaultHandlerAvailable { get; private set; }

        // /// <summary>
        // /// The installed runtime logger.
        // /// </summary>
        // protected TextWriter Logger => Runtime.Logger;
        //
        // /// <summary>
        // /// The installed runtime json logger.
        // /// </summary>
        // protected JsonWriter JsonLogger => Runtime.JsonLogger;

        // /// <summary>
        // /// Initializes a new instance of the <see cref="Actor"/> class.
        // /// </summary>
        // protected Actor()
        // {
        //     // ActionMap = new Dictionary<Type, CachedDelegate>();
        //     // CurrentStatus = Status.Active;
        //     // CurrentStateName = default;
        //     // IsDefaultHandlerAvailable = false;
        // }

        // /// <summary>
        // /// Configures the actor.
        // /// </summary>
        // internal void Configure(ActorRuntime runtime, ActorId id, IActorManager manager, IEventQueue inbox)
        // {
        //     // Runtime = runtime;
        //     // Id = id;
        //     // Manager = manager;
        //     // Inbox = inbox;
        // }

        // /// <summary>
        // /// Returns a nondeterministic boolean choice, that can be
        // /// controlled during analysis or testing.
        // /// </summary>
        // /// <returns>The controlled nondeterministic choice.</returns>
        // protected bool RandomBoolean() => Runtime.GetNondeterministicBooleanChoice(2, Id.Name, Id.Type);
        //
        // /// <summary>
        // /// Returns a nondeterministic boolean choice, that can be
        // /// controlled during analysis or testing. The value is used
        // /// to generate a number in the range [0..maxValue), where 0
        // /// triggers true.
        // /// </summary>
        // /// <param name="maxValue">The max value.</param>
        // /// <returns>The controlled nondeterministic choice.</returns>
        // protected bool RandomBoolean(int maxValue) =>
        //     Runtime.GetNondeterministicBooleanChoice(maxValue, Id.Name, Id.Type);
        //
        // /// <summary>
        // /// Returns a nondeterministic integer, that can be controlled during
        // /// analysis or testing. The value is used to generate an integer in
        // /// the range [0..maxValue).
        // /// </summary>
        // /// <param name="maxValue">The max value.</param>
        // /// <returns>The controlled nondeterministic integer.</returns>
        // protected int RandomInteger(int maxValue) =>
        //     Runtime.GetNondeterministicIntegerChoice(maxValue, Id.Name, Id.Type);
        //
        // /// <summary>
        // /// Invokes the specified monitor with the specified <see cref="Event"/>.
        // /// </summary>
        // /// <typeparam name="T">Type of the monitor.</typeparam>
        // /// <param name="e">Event to send to the monitor.</param>
        // protected void Monitor<T>(Event e) => Monitor(typeof(T), e);
        //
        // /// <summary>
        // /// Invokes the specified monitor with the specified event.
        // /// </summary>
        // /// <param name="type">Type of the monitor.</param>
        // /// <param name="e">The event to send.</param>
        // protected void Monitor(Type type, Event e)
        // {
        //     Assert(e != null, "{0} is sending a null event.", Id);
        //     Runtime.Monitor(type, e, Id.Name, Id.Type, CurrentStateName);
        // }

        // /// <summary>
        // /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        // /// </summary>
        // protected void Assert(bool predicate) => Runtime.Assert(predicate);
        //
        // /// <summary>
        // /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        // /// </summary>
        // protected void Assert(bool predicate, string s, object arg0) =>
        //     Runtime.Assert(predicate, s, arg0);
        //
        // /// <summary>
        // /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        // /// </summary>
        // protected void Assert(bool predicate, string s, object arg0, object arg1) =>
        //     Runtime.Assert(predicate, s, arg0, arg1);
        //
        // /// <summary>
        // /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        // /// </summary>
        // protected void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
        //     Runtime.Assert(predicate, s, arg0, arg1, arg2);
        //
        // /// <summary>
        // /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        // /// </summary>
        // protected void Assert(bool predicate, string s, params object[] args) =>
        //     Runtime.Assert(predicate, s, args);
        //
        // /// <summary>
        // /// Raises a <see cref='HaltEvent'/> to halt the actor at the end of the current action.
        // /// </summary>
        // protected virtual void RaiseHaltEvent()
        // {
        //     Assert(CurrentStatus is Status.Active, "{0} invoked Halt while halting.", Id);
        //     CurrentStatus = Status.Halting;
        // }

        // /// <summary>
        // /// Asynchronous callback that is invoked when the actor is initialized with an optional event.
        // /// </summary>
        // /// <param name="initialEvent">Optional event used for initialization.</param>
        // /// <returns>Task that represents the asynchronous operation.</returns>
        // protected virtual Task OnInitializeAsync(Event initialEvent) => Task.CompletedTask;
        //
        // /// <summary>
        // /// Asynchronous callback that is invoked when the actor successfully dequeues
        // /// an event from its inbox. This method is not called when the dequeue happens
        // /// via a receive statement.
        // /// </summary>
        // /// <param name="e">The event that was dequeued.</param>
        // protected virtual Task OnEventDequeuedAsync(Event e) => Task.CompletedTask;

        // /// <summary>
        // /// Asynchronous callback that is invoked when the actor finishes handling a dequeued
        // /// event, unless the handler of the dequeued event caused the actor to halt (either
        // /// normally or due to an exception). The actor will either become idle or dequeue
        // /// the next event from its inbox.
        // /// </summary>
        // /// <param name="e">The event that was handled.</param>
        // protected virtual Task OnEventHandledAsync(Event e) => Task.CompletedTask;
        //
        // /// <summary>
        // /// Asynchronous callback that is invoked when the actor receives an event that
        // /// it is not prepared to handle. The callback is invoked first, after which the
        // /// actor will necessarily throw an <see cref="UnhandledEventException"/>
        // /// </summary>
        // /// <param name="e">The event that was unhandled.</param>
        // /// <param name="state">The state when the event was dequeued.</param>
        // protected virtual Task OnEventUnhandledAsync(Event e, string state) => Task.CompletedTask;
        //
        // /// <summary>
        // /// Asynchronous callback that is invoked when the actor handles an exception.
        // /// </summary>
        // /// <param name="ex">The exception thrown by the actor.</param>
        // /// <param name="e">The event being handled when the exception was thrown.</param>
        // /// <returns>The action that the runtime should take.</returns>
        // protected virtual Task OnExceptionHandledAsync(Exception ex, Event e) => Task.CompletedTask;

        // /// <summary>
        // /// Asynchronous callback that is invoked when the actor halts.
        // /// </summary>
        // /// <param name="e">The event being handled when the actor halted.</param>
        // /// <returns>Task that represents the asynchronous operation.</returns>
        // protected virtual Task OnHaltAsync(Event e) => Task.CompletedTask;

        // /// <summary>
        // /// Enqueues the specified event and its metadata.
        // /// </summary>
        // internal EnqueueStatus Enqueue(Event e, Guid opGroupId, EventInfo info)
        // {
        //     if (CurrentStatus is Status.Halted)
        //     {
        //         return EnqueueStatus.Dropped;
        //     }
        //
        //     return Inbox.Enqueue(e, opGroupId, info);
        // }
        

        // /// <summary>
        // /// Returns the formatted string to be used with a fair nondeterministic boolean choice.
        // /// </summary>
        // private protected virtual string FormatFairRandom(string callerMemberName, string callerFilePath, int callerLineNumber) =>
        //     string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}_{3}",
        //         Id.Name, callerMemberName, callerFilePath, callerLineNumber.ToString());

        // /// <summary>
        // /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        // /// exception, and throws it to the user.
        // /// </summary>
        // private protected virtual void ReportUnhandledException(Exception ex, string actionName)
        // {
        //     // Runtime.WrapAndThrowException(ex, $"{0} (action '{1}')", Id, actionName);
        // }

        // /// <summary>
        // /// Invokes user callback when the actor throws an exception.
        // /// </summary>
        // /// <param name="ex">The exception thrown by the actor.</param>
        // /// <param name="methodName">The handler (outermost) that threw the exception.</param>
        // /// <param name="e">The event being handled when the exception was thrown.</param>
        // /// <returns>True if the exception was handled, else false if it should continue to get thrown.</returns>
        // private protected bool OnExceptionHandler(Exception ex, string methodName, Event e)
        // {
        //     if (ex is ExecutionCanceledException)
        //     {
        //         // Internal exception used during testing.
        //         return false;
        //     }
        //
        //     Runtime.LogWriter.LogExceptionThrown(Id, CurrentStateName, methodName, ex);
        //
        //     var outcome = OnException(ex, methodName, e);
        //     if (outcome is OnExceptionOutcome.ThrowException)
        //     {
        //         return false;
        //     }
        //     else if (outcome is OnExceptionOutcome.Halt)
        //     {
        //         CurrentStatus = Status.Halting;
        //     }
        //
        //     Runtime.LogWriter.LogExceptionHandled(Id, CurrentStateName, methodName, ex);
        //     return true;
        // }

        // /// <summary>
        // /// Invokes user callback when the actor receives an event that it cannot handle.
        // /// </summary>
        // /// <param name="ex">The exception thrown by the actor.</param>
        // /// <param name="e">The unhandled event.</param>
        // /// <returns>True if the the actor should gracefully halt, else false if the exception
        // /// should continue to get thrown.</returns>
        // private protected bool OnUnhandledEventExceptionHandler(UnhandledEventException ex, Event e)
        // {
        //     Runtime.LogWriter.LogExceptionThrown(Id, ex.CurrentStateName, string.Empty, ex);
        //
        //     var outcome = OnException(ex, string.Empty, e);
        //     if (outcome is OnExceptionOutcome.ThrowException)
        //     {
        //         return false;
        //     }
        //
        //     CurrentStatus = Status.Halting;
        //     Runtime.LogWriter.LogExceptionHandled(Id, ex.CurrentStateName, string.Empty, ex);
        //     return true;
        // }

        // /// <summary>
        // /// User callback when the actor throws an exception. By default,
        // /// the actor throws the exception causing the runtime to fail.
        // /// </summary>
        // /// <param name="ex">The exception thrown by the actor.</param>
        // /// <param name="methodName">The handler (outermost) that threw the exception.</param>
        // /// <param name="e">The event being handled when the exception was thrown.</param>
        // /// <returns>The action that the runtime should take.</returns>
        // protected virtual OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
        // {
        //     return OnExceptionOutcome.ThrowException;
        // }

        // /// <summary>
        // /// Halts the actor.
        // /// </summary>
        // /// <param name="e">The event being handled when the actor halts.</param>
        // private protected Task HaltAsync(Event e)
        // {
        //     CurrentStatus = Status.Halted;
        //
        //     // Close the inbox, which will stop any subsequent enqueues.
        //     Inbox.Close();
        //
        //     Runtime.LogWriter.LogHalt(Id, Inbox.Size);
        //
        //     // Dispose any held resources.
        //     Inbox.Dispose();
        //
        //     // Invoke user callback.
        //     return OnHaltAsync(e);
        // }

        // /// <summary>
        // /// Determines whether the specified object is equal to the current object.
        // /// </summary>
        // public override bool Equals(object obj)
        // {
        //     if (obj is Actor m &&
        //         GetType() == m.GetType())
        //     {
        //         return Id.Value == m.Id.Value;
        //     }
        //
        //     return false;
        // }

        // /// <summary>
        // /// Returns the hash code for this instance.
        // /// </summary>
        // public override int GetHashCode()
        // {
        //     return Id.Value.GetHashCode();
        // }

        // /// <summary>
        // /// Returns a string that represents the current actor.
        // /// </summary>
        // public override string ToString()
        // {
        //     return Id.Name;
        // }
        //
        // /// <summary>
        // /// The status of the actor.
        // /// </summary>
        // private protected enum Status
        // {
        //     /// <summary>
        //     /// The actor is active.
        //     /// </summary>
        //     Active = 0,
        //
        //     /// <summary>
        //     /// The actor is halting.
        //     /// </summary>
        //     Halting,
        //
        //     /// <summary>
        //     /// The actor is halted.
        //     /// </summary>
        //     Halted
        // }

        // /// <summary>
        // /// The type of a user callback.
        // /// </summary>
        // private protected static class UserCallbackType
        // {
        //     internal const string OnInitialize = nameof(OnInitializeAsync);
        //     internal const string OnEventDequeued = nameof(OnEventDequeuedAsync);
        //     internal const string OnEventHandled = nameof(OnEventHandledAsync);
        //     internal const string OnEventUnhandled = nameof(OnEventUnhandledAsync);
        //     internal const string OnExceptionHandled = nameof(OnExceptionHandledAsync);
        //     internal const string OnHalt = nameof(OnHaltAsync);
        // }

        // /// <summary>
        // /// Attribute for declaring which action should be invoked
        // /// to handle a dequeued event of the specified type.
        // /// </summary>
        // [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        // protected sealed class OnEventDoActionAttribute : Attribute
        // {
        //     /// <summary>
        //     /// The type of the dequeued event.
        //     /// </summary>
        //     internal Type Event;
        //
        //     /// <summary>
        //     /// The name of the action to invoke.
        //     /// </summary>
        //     internal string Action;
        //
        //     /// <summary>
        //     /// Initializes a new instance of the <see cref="OnEventDoActionAttribute"/> class.
        //     /// </summary>
        //     /// <param name="eventType">The type of the dequeued event.</param>
        //     /// <param name="actionName">The name of the action to invoke.</param>
        //     public OnEventDoActionAttribute(Type eventType, string actionName)
        //     {
        //         Event = eventType;
        //         Action = actionName;
        //     }
        // }
//     }
// }