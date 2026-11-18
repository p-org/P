using System;

namespace PChecker.Runtime.Exceptions
{
    /// <summary>
    /// No longer used, kept so that existing foreign code that refers to it still compiles.
    /// </summary>
    [Obsolete("No longer used: a raise or goto no longer throws an exception.")]
    public enum NonStandardReturn
    {
        Raise,
        Goto,
        Pop
    }

    /// <summary>
    /// No longer thrown, kept so that existing foreign code that catches it still compiles.
    /// </summary>
    /// <remarks>
    /// The runtime used to throw this exception to unwind the current action after a raise or goto.
    /// Now <see cref="StateMachines.StateMachine.RaiseEvent"/> and
    /// <see cref="StateMachines.StateMachine.RaiseGotoStateEvent{S}"/> only record the transition and
    /// return, so foreign code must return right after calling them.
    /// </remarks>
    [Obsolete("No longer thrown: a raise or goto records the transition and returns, so foreign code " +
              "must return right after calling RaiseEvent or RaiseGotoStateEvent.")]
    public class PNonStandardReturnException : Exception
    {
        public PNonStandardReturnException()
        {
        }

        public PNonStandardReturnException(string message) : base(message)
        {
        }

        public PNonStandardReturnException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public NonStandardReturn ReturnKind { get; set; }
    }
}
