using Microsoft.Coyote.Specifications;
using System;

namespace Plang.PrtSharp.Exceptions
{
    public class PMonitorTransitionException : Exception
    {
        public readonly Monitor.Transition Transition;

        public PMonitorTransitionException(Monitor.Transition transition)
        {
            Transition = transition;
        }
    }
}