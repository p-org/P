// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Actors.SharedObjects
{
    /// <summary>
    /// Event used to communicate with a shared counter actor.
    /// </summary>
    internal class SharedDictionaryEvent : Event
    {
        /// <summary>
        /// Supported shared dictionary operations.
        /// </summary>
        internal enum OperationType
        {
            Initialize,
            Get,
            Set,
            TryAdd,
            TryGet,
            TryUpdate,
            TryRemove,
            Count
        }

        /// <summary>
        /// The operation stored in this event.
        /// </summary>
        internal OperationType Operation { get; private set; }

        /// <summary>
        /// The shared dictionary key stored in this event.
        /// </summary>
        internal object Key { get; private set; }

        /// <summary>
        /// The shared dictionary value stored in this event.
        /// </summary>
        internal object Value { get; private set; }

        /// <summary>
        /// The shared dictionary comparison value stored in this event.
        /// </summary>
        internal object ComparisonValue { get; private set; }

        /// <summary>
        /// The sender actor stored in this event.
        /// </summary>
        internal ActorId Sender { get; private set; }

        /// <summary>
        /// The comparer stored in this event.
        /// </summary>
        internal object Comparer { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedDictionaryEvent"/> class.
        /// </summary>
        private SharedDictionaryEvent(OperationType op, object key, object value,
            object comparisonValue, ActorId sender, object comparer)
        {
            this.Operation = op;
            this.Key = key;
            this.Value = value;
            this.ComparisonValue = comparisonValue;
            this.Sender = sender;
            this.Comparer = comparer;
        }

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.Initialize"/> operation.
        /// </summary>
        internal static SharedDictionaryEvent InitializeEvent(object comparer) =>
            new SharedDictionaryEvent(OperationType.Initialize, null, null, null, null, comparer);

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.TryAdd"/> operation.
        /// </summary>
        internal static SharedDictionaryEvent TryAddEvent(object key, object value, ActorId sender) =>
            new SharedDictionaryEvent(OperationType.TryAdd, key, value, null, sender, null);

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.TryUpdate"/> operation.
        /// </summary>
        internal static SharedDictionaryEvent TryUpdateEvent(object key, object value, object comparisonValue, ActorId sender) =>
            new SharedDictionaryEvent(OperationType.TryUpdate, key, value, comparisonValue, sender, null);

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.Get"/> operation.
        /// </summary>
        internal static SharedDictionaryEvent GetEvent(object key, ActorId sender) =>
            new SharedDictionaryEvent(OperationType.Get, key, null, null, sender, null);

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.TryGet"/> operation.
        /// </summary>
        internal static SharedDictionaryEvent TryGetEvent(object key, ActorId sender) =>
            new SharedDictionaryEvent(OperationType.TryGet, key, null, null, sender, null);

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.Set"/> operation.
        /// </summary>
        internal static SharedDictionaryEvent SetEvent(object key, object value) =>
            new SharedDictionaryEvent(OperationType.Set, key, value, null, null, null);

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.Count"/> operation.
        /// </summary>
        internal static SharedDictionaryEvent CountEvent(ActorId sender) =>
            new SharedDictionaryEvent(OperationType.Count, null, null, null, sender, null);

        /// <summary>
        /// Creates a new event for the <see cref="OperationType.TryRemove"/> operation.
        /// </summary>
        internal static SharedDictionaryEvent TryRemoveEvent(object key, ActorId sender) =>
            new SharedDictionaryEvent(OperationType.TryRemove, key, null, null, sender, null);
    }
}
