using System.Collections.Generic;
using System.Linq;
using Microsoft.PSharp;
using Plang.PrtSharp.Exceptions;

namespace Plang.PrtSharp
{
    public class PMonitor : Monitor
    {
        public static List<string> observes;

        public object gotoPayload;

        public void RaiseEvent(PMachine source, Event ev, object payload = null)
        {
            Assert(!(ev is Default), "Monitor cannot raise a null event");
            var oneArgConstructor = ev.GetType().GetConstructors().First(x => x.GetParameters().Length > 0);
            var @event = (Event) oneArgConstructor.Invoke(new[] {payload});
            Raise(@event);
            throw new PNonStandardReturnException {ReturnKind = NonStandardReturn.Raise};
        }

        public void GotoState<T>(object payload) where T : MonitorState
        {
            //todo: goto parameter has to be initialized correctly
            gotoPayload = payload;
            Goto<T>();
            throw new PNonStandardReturnException {ReturnKind = NonStandardReturn.Goto};
        }
    }
}