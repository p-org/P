using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.PSharp;
using System.Linq.Expressions;
using Microsoft.PSharp.Runtime;

namespace PSharpExtensions
{
    public class PMachine : Machine
    {
        public string interfaceName;
        public List<string> sends = new List<string>();
        public List<string> creates = new List<string>();
        public List<string> receives = new List<string>();
        public PMachineId self;
        protected object gotoPayload = null;

        public class InitializeParameters
        {
            public string InterfaceName { get; }
            public object Payload { get; }

            public InitializeParameters(string interfaceName, object payload)
            {
                this.InterfaceName = interfaceName;
                this.Payload = payload;
            }
        }

        public class IntializeParametersEvent : PEvent<InitializeParameters>
        {
            public IntializeParametersEvent(InitializeParameters payload) : base(payload)
            {
            }
        }

        protected class ContructorEvent : PEvent<object>
        {
            public ContructorEvent(object payload) : base(payload)
            {
            }
        }

        protected void InitializeParametersFunction()
        {
            if (ReceivedEvent is IntializeParametersEvent @event)
            {
                interfaceName = (@event.Payload as InitializeParameters).InterfaceName;
                self = new PMachineId(this.Id, this.receives.ToList());
                this.RaiseEvent(this, new ContructorEvent((@event.Payload as InitializeParameters).Payload));
            }
            else
            {
                throw new ArgumentException("Event type is incorrect:" + ReceivedEvent.GetType().Name);
            }
        }

        protected override OnExceptionOutcome OnException(string methodName, Exception ex)
        {
            return ex is PNonStandardReturnException ? OnExceptionOutcome.HandledException : base.OnException(methodName, ex);
        }

        public PMachineId CreateInterface(PMachine creator, string createInterface, object payload = null)
        {
            var createdInterface = PModule.linkMap[creator.interfaceName][createInterface];
            this.Assert(this.creates.Contains(createdInterface), $"Machine {this.GetType().Name} cannot create interface {createdInterface}, not in its creates set");
            var createMachine = PModule.interfaceDefinitionMap[createdInterface];
            var machineId = this.CreateMachine(createMachine, new IntializeParametersEvent(new InitializeParameters(createdInterface, payload)));
            return new PMachineId(machineId, PInterfaces.GetPermissions(createdInterface));
        }

        public void SendEvent(PMachine source, PMachineId target, Event ev, object payload = null)
        {
            this.Assert(!(ev is Default), "Machine cannot send a null event");
            this.Assert(this.sends.Contains(ev.GetType().Name), $"Event {ev.GetType().Name} is not in the sends set of the Machine {source.GetType().Name}");
            this.Assert(target.Permissions.Contains(ev.GetType().Name), $"Event {ev.GetType().Name} is not in the permissions set of the target machine");
            var oneArgConstructor = ev.GetType().GetConstructors().First(x => x.GetParameters().Length>0);
            var @event = (Event)oneArgConstructor.Invoke(new object[]{payload});
            this.Send(target.Id, @event);
        }

        public void RaiseEvent(PMachine source, Event ev, object payload = null)
        {
            this.Assert(!(ev is Default), "Machine cannot raise a null event");
            var oneArgConstructor = ev.GetType().GetConstructors().First(x => x.GetParameters().Length > 0);
            var @event = (Event)oneArgConstructor.Invoke(new object[] { payload });
            this.Raise(@event);
            throw new PNonStandardReturnException() {ReturnKind = NonStandardReturn.Raise};
        }

        public void GotoState<T>(object payload) where T : MachineState
        {
            this.gotoPayload = payload;
            this.Goto<T>();
            throw new PNonStandardReturnException() { ReturnKind = NonStandardReturn.Goto };
        }

        public void PopState()
        {
            this.Pop();
            throw new PNonStandardReturnException() { ReturnKind = NonStandardReturn.Pop };
        }
    }
}
