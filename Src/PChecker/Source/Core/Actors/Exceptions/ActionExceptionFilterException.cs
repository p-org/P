// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Exception that is thrown by the runtime upon an <see cref="Actor"/> action failure.
    /// </summary>
    internal sealed class ActionExceptionFilterException : RuntimeException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActionExceptionFilterException"/> class.
        /// </summary>
        /// <param name="message">Message</param>
        internal ActionExceptionFilterException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionExceptionFilterException"/> class.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="innerException">Inner exception</param>
        internal ActionExceptionFilterException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
