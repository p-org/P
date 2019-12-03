using Microsoft.Coyote.Actors;
using System;

namespace Plang.PrtSharp.Exceptions
{
    public class PMachineTransitionException : Exception
    {
        public readonly StateMachine.Transition Transition;

        public PMachineTransitionException(StateMachine.Transition transition)
        {
            Transition = transition;
        }
    }
}