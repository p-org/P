// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace PChecker.Exceptions
{
    /// <summary>
    /// Exception that is thrown upon cancellation of testing execution by the runtime.
    /// </summary>
    [DebuggerStepThrough]
    public sealed class ExecutionCanceledException : RuntimeException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionCanceledException"/> class.
        /// </summary>
        internal ExecutionCanceledException()
        {
        }
    }
}