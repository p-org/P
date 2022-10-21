// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Actors.SharedObjects
{
    /// <summary>
    /// Event containing the value of a shared register.
    /// </summary>
    internal class SharedRegisterResponseEvent<T> : Event
    {
        /// <summary>
        /// Value.
        /// </summary>
        internal T Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedRegisterResponseEvent{T}"/> class.
        /// </summary>
        internal SharedRegisterResponseEvent(T value)
        {
            this.Value = value;
        }
    }
}
