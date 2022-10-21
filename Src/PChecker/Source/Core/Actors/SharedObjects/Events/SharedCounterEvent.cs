// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Actors.SharedObjects
{
    /// <summary>
    /// Event used to communicate with a shared counter actor.
    /// </summary>
    internal class SharedCounterEvent : Event
    {
        /// <summary>
        /// Supported shared counter operations.
        /// </summary>
        internal enum OperationType
        {
            Get,
            Set,
            Increment,
            Decrement,
            Add,
            CompareExchange
        }

        /// <summary>
        /// The operation stored in this event.
        /// </summary>
        public OperationType Operation { get; private set; }

        /// <summary>
        /// The shared counter value stored in this event.
        /// </summary>
        public int Value { get; private set; }

        /// <summary>
        /// Comparand value stored in this event.
        /// </summary>
        public int Comparand { get; private set; }

        /// <summary>
        /// The sender actor stored in this event.
        /// </summary>
        public ActorId Sender { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedCounterEvent"/> class.
        /// </summary>
        private SharedCounterEvent(OperationType op, int value, int comparand, ActorId sender)
        {
            this.Operation = op;
            this.Value = value;
            this.Comparand = comparand;
            this.Sender = sender;
        }

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.Increment"/> operation.
        /// </summary>
        public static SharedCounterEvent IncrementEvent()
        {
            return new SharedCounterEvent(OperationType.Increment, 0, 0, null);
        }

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.Decrement"/> operation.
        /// </summary>
        public static SharedCounterEvent DecrementEvent()
        {
            return new SharedCounterEvent(OperationType.Decrement, 0, 0, null);
        }

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.Set"/> operation.
        /// </summary>
        public static SharedCounterEvent SetEvent(ActorId sender, int value)
        {
            return new SharedCounterEvent(OperationType.Set, value, 0, sender);
        }

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.Get"/> operation.
        /// </summary>
        public static SharedCounterEvent GetEvent(ActorId sender)
        {
            return new SharedCounterEvent(OperationType.Get, 0, 0, sender);
        }

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.Add"/> operation.
        /// </summary>
        public static SharedCounterEvent AddEvent(ActorId sender, int value)
        {
            return new SharedCounterEvent(OperationType.Add, value, 0, sender);
        }

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.CompareExchange"/> operation.
        /// </summary>
        public static SharedCounterEvent CompareExchangeEvent(ActorId sender, int value, int comparand)
        {
            return new SharedCounterEvent(OperationType.CompareExchange, value, comparand, sender);
        }
    }
}
