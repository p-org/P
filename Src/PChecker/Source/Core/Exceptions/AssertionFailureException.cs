// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote
{
    /// <summary>
    /// The exception that is thrown by the Coyote runtime upon assertion failure.
    /// </summary>
    internal sealed class AssertionFailureException : RuntimeException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssertionFailureException"/> class.
        /// </summary>
        /// <param name="message">Message</param>
        internal AssertionFailureException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssertionFailureException"/> class.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="innerException">Inner exception</param>
        internal AssertionFailureException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
