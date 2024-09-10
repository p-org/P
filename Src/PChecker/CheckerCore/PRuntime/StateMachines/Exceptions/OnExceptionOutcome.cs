// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PChecker.StateMachines.Exceptions
{
    /// <summary>
    /// The outcome when an <see cref="StateMachine"/> throws an exception.
    /// </summary>
    public enum OnExceptionOutcome
    {
        /// <summary>
        /// The state machine throws the exception causing the runtime to fail.
        /// </summary>
        ThrowException = 0,

        /// <summary>
        /// The state machine handles the exception and resumes execution.
        /// </summary>
        HandledException = 1,

        /// <summary>
        /// The state machine handles the exception and halts.
        /// </summary>
        Halt = 2
    }
}