// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// The outcome when an <see cref="Actor"/> throws an exception.
    /// </summary>
    public enum OnExceptionOutcome
    {
        /// <summary>
        /// The actor throws the exception causing the runtime to fail.
        /// </summary>
        ThrowException = 0,

        /// <summary>
        /// The actor handles the exception and resumes execution.
        /// </summary>
        HandledException = 1,

        /// <summary>
        /// The actor handles the exception and halts.
        /// </summary>
        Halt = 2
    }
}
