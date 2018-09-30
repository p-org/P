using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace PSharpExtensions
{
    public class PMonitor : Monitor
    {
        public static List<string> observes;

        public object gotoPayload;

        public void RaiseEvent(PMachine source, Event ev, object payload = null)
        {
            this.Assert(!(ev is Default), "Monitor cannot raise a null event");
            var oneArgConstructor = ev.GetType().GetConstructors().First(x => x.GetParameters().Length > 0);
            var @event = (Event)oneArgConstructor.Invoke(new object[] { payload });
            this.Raise(@event);
            throw new PNonStandardReturnException() { ReturnKind = NonStandardReturn.Raise };
        }

        public void GotoState<T>(object payload) where T : MonitorState
        {
            //todo: goto parameter has to be initialized correctly
            this.gotoPayload = payload;
            this.Goto<T>();
            throw new PNonStandardReturnException() { ReturnKind = NonStandardReturn.Goto };
        }
    }
}
