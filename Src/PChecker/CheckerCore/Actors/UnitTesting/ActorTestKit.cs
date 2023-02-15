﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PChecker.Actors.Events;
using PChecker.Random;

namespace PChecker.Actors.UnitTesting
{
    /// <summary>
    /// Provides methods for testing an actor of type <typeparamref name="T"/> in isolation.
    /// </summary>
    /// <typeparam name="T">The actor type to test.</typeparam>
    public sealed class ActorTestKit<T>
        where T : Actor
    {
        /// <summary>
        /// The actor testing runtime.
        /// </summary>
        private readonly ActorUnitTestingRuntime Runtime;

        /// <summary>
        /// The instance of the actor being tested.
        /// </summary>
        public readonly T ActorInstance;

        /// <summary>
        /// True if the actor has started its execution, else false.
        /// </summary>
        private bool IsRunning;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorTestKit{T}"/> class.
        /// </summary>
        /// <param name="checkerConfiguration">The runtime checkerConfiguration to use.</param>
        public ActorTestKit(CheckerConfiguration checkerConfiguration)
        {
            if (checkerConfiguration is null)
            {
                checkerConfiguration = CheckerConfiguration.Create();
            }

            var valueGenerator = new RandomValueGenerator(checkerConfiguration);
            Runtime = new ActorUnitTestingRuntime(checkerConfiguration, typeof(T), valueGenerator);
            ActorInstance = Runtime.Instance as T;
            IsRunning = false;
            Runtime.OnFailure += ex =>
            {
                Runtime.Logger.WriteLine(ex.ToString());
            };
        }

        /// <summary>
        /// Initializes the actor, passes the optional specified event and
        /// invokes its on-entry handler, if there is one available. This method returns a task that
        /// completes when the actor reaches quiescence (typically when the event handler
        /// finishes executing because there are not more events to dequeue, or when the actor
        /// asynchronously waits to receive an event).  If the actor is a state machine
        /// it also transitions the actor to its start state.
        /// </summary>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        public Task StartActorAsync(Event initialEvent = null)
        {
            Runtime.Assert(!IsRunning, string.Format("{0} is already running.", ActorInstance.Id));
            IsRunning = true;
            return Runtime.StartAsync(initialEvent);
        }

        /// <summary>
        /// Sends an event to the actor and starts its event handler. This method returns
        /// a task that completes when the actor reaches quiescence (typically when the
        /// event handler finishes executing because there are not more events to dequeue, or
        /// when the actor asynchronously waits to receive an event).
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns>
        public Task SendEventAsync(Event e)
        {
            Runtime.Assert(IsRunning, string.Format("{0} is not running.", ActorInstance.Id));
            return Runtime.SendEventAndExecuteAsync(Runtime.Instance.Id, e, null, Guid.Empty, null);
        }

        /// <summary>
        /// Invokes the actor method with the specified name, and passing the specified
        /// optional parameters. Use this method to invoke private methods of the actor.
        /// </summary>
        /// <param name="methodName">The name of the actor method.</param>
        /// <param name="parameters">The parameters to the method.</param>
        public object Invoke(string methodName, params object[] parameters)
        {
            var method = GetMethod(methodName, false, null);
            return method.Invoke(ActorInstance, parameters);
        }

        /// <summary>
        /// Invokes the actor method with the specified name and parameter types, passing the
        /// specified optional parameters. Use this method to invoke private methods of the actor.
        /// </summary>
        /// <param name="methodName">The name of the actor method.</param>
        /// <param name="parameterTypes">The parameter types of the method.</param>
        /// <param name="parameters">The parameters to the method.</param>
        public object Invoke(string methodName, Type[] parameterTypes, params object[] parameters)
        {
            var method = GetMethod(methodName, false, parameterTypes);
            return method.Invoke(ActorInstance, parameters);
        }

        /// <summary>
        /// Invokes the asynchronous actor method with the specified name, and passing the specified
        /// optional parameters. Use this method to invoke private methods of the actor.
        /// </summary>
        /// <param name="methodName">The name of the actor method.</param>
        /// <param name="parameters">The parameters to the method.</param>
        public async Task<object> InvokeAsync(string methodName, params object[] parameters)
        {
            var method = GetMethod(methodName, true, null);
            var task = (Task)method.Invoke(ActorInstance, parameters);
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty.GetValue(task);
        }

        /// <summary>
        /// Invokes the asynchronous actor method with the specified name and parameter types, and passing
        /// the specified optional parameters. Use this method to invoke private methods of the actor.
        /// </summary>
        /// <param name="methodName">The name of the actor method.</param>
        /// <param name="parameterTypes">The parameter types of the method.</param>
        /// <param name="parameters">The parameters to the method.</param>
        public async Task<object> InvokeAsync(string methodName, Type[] parameterTypes, params object[] parameters)
        {
            var method = GetMethod(methodName, true, parameterTypes);
            var task = (Task)method.Invoke(ActorInstance, parameters);
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty.GetValue(task);
        }

        /// <summary>
        /// Uses reflection to get the actor method with the specified name and parameter types.
        /// </summary>
        /// <param name="methodName">The name of the actor method.</param>
        /// <param name="isAsync">True if the method is async, else false.</param>
        /// <param name="parameterTypes">The parameter types of the method.</param>
        private MethodInfo GetMethod(string methodName, bool isAsync, Type[] parameterTypes)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            MethodInfo method;
            if (parameterTypes is null)
            {
                method = ActorInstance.GetType().GetMethod(methodName, bindingFlags);
            }
            else
            {
                method = ActorInstance.GetType().GetMethod(methodName, bindingFlags,
                    Type.DefaultBinder, parameterTypes, null);
            }

            Runtime.Assert(method != null, string.Format("Unable to invoke method '{0}' of {1}.",
                methodName, ActorInstance.Id));
            Runtime.Assert(method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) is null != isAsync,
                string.Format("Must invoke {0}method '{1}' of {2} using '{3}'.",
                isAsync ? string.Empty : "async ", methodName, ActorInstance.Id, isAsync ? "Invoke" : "InvokeAsync"));

            return method;
        }

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        public void Assert(bool predicate)
        {
            Runtime.Assert(predicate);
        }

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        public void Assert(bool predicate, string s, object arg0)
        {
            Runtime.Assert(predicate, s, arg0);
        }

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        public void Assert(bool predicate, string s, object arg0, object arg1)
        {
            Runtime.Assert(predicate, s, arg0, arg1);
        }

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        public void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
        {
            Runtime.Assert(predicate, s, arg0, arg1, arg2);
        }

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        public void Assert(bool predicate, string s, params object[] args)
        {
            Runtime.Assert(predicate, s, args);
        }

        /// <summary>
        /// If the actor is a state machine, this asserts that the state machine has transitioned to the state with the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="S">The type of the state.</typeparam>
        public void AssertStateTransition<S>()
            where S : StateMachine.State
        {
            AssertStateTransition(typeof(S).FullName);
        }

        /// <summary>
        /// If the actor is a state machine, asserts that the state machine has transitioned to the state with the specified name
        /// (either <see cref="Type.FullName"/> or <see cref="MemberInfo.Name"/>).
        /// </summary>
        /// <param name="stateName">The name of the state.</param>
        public void AssertStateTransition(string stateName)
        {
            var sm = ActorInstance as StateMachine;
            Runtime.Assert(sm != null, "Actor is a state machine");
            var currentState = sm.CurrentState;
            Runtime.Assert(currentState != null, "Actor is initialized");
            var predicate = currentState.FullName.Equals(stateName) ||
                            currentState.FullName.Equals(
                                currentState.DeclaringType.FullName + "+" + stateName);
            Runtime.Assert(predicate, string.Format("{0} is in state '{1}', not in '{2}'.",
                ActorInstance.Id, currentState.FullName, stateName));
        }

        /// <summary>
        /// Asserts that the actor is waiting (or not) to receive an event.
        /// </summary>
        public void AssertIsWaitingToReceiveEvent(bool isWaiting)
        {
            Runtime.Assert(Runtime.IsActorWaitingToReceiveEvent == isWaiting,
                "{0} is {1}waiting to receive an event.",
                ActorInstance.Id, Runtime.IsActorWaitingToReceiveEvent ? string.Empty : "not ");
        }

        /// <summary>
        /// Asserts that the actor inbox contains the specified number of events.
        /// </summary>
        /// <param name="numEvents">The number of events in the inbox.</param>
        public void AssertInboxSize(int numEvents)
        {
            Runtime.Assert(Runtime.ActorInbox.Size == numEvents,
                "{0} contains '{1}' events in its inbox.",
                ActorInstance.Id, Runtime.ActorInbox.Size);
        }
    }
}
