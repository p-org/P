// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Actors.SharedObjects
{
    /// <summary>
    /// Event used to communicate with a shared register actor.
    /// </summary>
    internal class SharedRegisterEvent : Event
    {
        /// <summary>
        /// Supported shared register operations.
        /// </summary>
        internal enum OperationType
        {
            Get,
            Set,
            Update
        }

        /// <summary>
        /// The operation stored in this event.
        /// </summary>
        public OperationType Operation { get; private set; }

        /// <summary>
        /// The shared register value stored in this event.
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// The shared register func stored in this event.
        /// </summary>
        public object Func { get; private set; }

        /// <summary>
        /// The sender actor stored in this event.
        /// </summary>
        public ActorId Sender { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedRegisterEvent"/> class.
        /// </summary>
        private SharedRegisterEvent(OperationType op, object value, object func, ActorId sender)
        {
            this.Operation = op;
            this.Value = value;
            this.Func = func;
            this.Sender = sender;
        }

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.Update"/> operation.
        /// </summary>
        public static SharedRegisterEvent UpdateEvent(object func, ActorId sender)
        {
            return new SharedRegisterEvent(OperationType.Update, null, func, sender);
        }

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.Set"/> operation.
        /// </summary>
        public static SharedRegisterEvent SetEvent(object value)
        {
            return new SharedRegisterEvent(OperationType.Set, value, null, null);
        }

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.Get"/> operation.
        /// </summary>
        public static SharedRegisterEvent GetEvent(ActorId sender)
        {
            return new SharedRegisterEvent(OperationType.Get, null, null, sender);
        }
    }
}
