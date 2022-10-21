// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Represents a send event configuration that is used during testing.
    /// </summary>
    public class SendOptions
    {
        /// <summary>
        /// The default send options.
        /// </summary>
        public static SendOptions Default { get; } = new SendOptions();

        /// <summary>
        /// True if this event must always be handled, else false.
        /// </summary>
        public bool MustHandle { get; private set; }

        /// <summary>
        /// Asserts that there must not be more than N instances of the
        /// event in the inbox queue of the receiver.
        /// </summary>
        public int Assert { get; private set; }

        /// <summary>
        /// User-defined hash of the event. The default value is 0. Override to
        /// improve the accuracy of stateful techniques during testing.
        /// </summary>
        public int HashedState { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SendOptions"/> class.
        /// </summary>
        public SendOptions(bool mustHandle = false, int assert = -1, int hashedState = 0)
        {
            this.MustHandle = mustHandle;
            this.Assert = assert;
            this.HashedState = hashedState;
        }

        /// <summary>
        /// A string that represents the current options.
        /// </summary>
        public override string ToString() =>
            string.Format("SendOptions[MustHandle='{0}', Assert='{1}', HashedState='{2}']",
                this.MustHandle, this.Assert, this.HashedState);
    }
}
