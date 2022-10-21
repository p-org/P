// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Actors.SharedObjects
{
    /// <summary>
    /// A thread-safe dictionary that can be shared in-memory by actors.
    /// </summary>
    /// <remarks>
    /// See also <see href="/coyote/learn/programming-models/actors/sharing-objects">Sharing Objects</see>.
    /// </remarks>
    public static class SharedDictionary
    {
        /// <summary>
        /// Creates a new shared dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="runtime">The actor runtime.</param>
        public static SharedDictionary<TKey, TValue> Create<TKey, TValue>(IActorRuntime runtime)
        {
            if (runtime is ControlledRuntime controlledRuntime)
            {
                return new Mock<TKey, TValue>(controlledRuntime, null);
            }

            return new SharedDictionary<TKey, TValue>(new ConcurrentDictionary<TKey, TValue>());
        }

        /// <summary>
        /// Creates a new shared dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="comparer">The key comparer.</param>
        /// <param name="runtime">The actor runtime.</param>
        public static SharedDictionary<TKey, TValue> Create<TKey, TValue>(IEqualityComparer<TKey> comparer, IActorRuntime runtime)
        {
            if (runtime is ControlledRuntime controlledRuntime)
            {
                return new Mock<TKey, TValue>(controlledRuntime, comparer);
            }

            return new SharedDictionary<TKey, TValue>(new ConcurrentDictionary<TKey, TValue>(comparer));
        }

        /// <summary>
        /// Mock implementation of <see cref="SharedDictionary{TKey, TValue}"/> that can be controlled during systematic testing.
        /// </summary>
        private sealed class Mock<TKey, TValue> : SharedDictionary<TKey, TValue>
        {
            // TODO: port to the new resource API or controlled locks once we integrate actors with tasks.

            /// <summary>
            /// Actor modeling the shared dictionary.
            /// </summary>
            private readonly ActorId DictionaryActor;

            /// <summary>
            /// The controlled runtime hosting this shared dictionary.
            /// </summary>
            private readonly ControlledRuntime Runtime;

            /// <summary>
            /// Initializes a new instance of the <see cref="Mock{TKey, TValue}"/> class.
            /// </summary>
            internal Mock(ControlledRuntime runtime, IEqualityComparer<TKey> comparer)
                : base(null)
            {
                this.Runtime = runtime;
                if (comparer != null)
                {
                    this.DictionaryActor = this.Runtime.CreateActor(
                        typeof(SharedDictionaryActor<TKey, TValue>),
                        SharedDictionaryEvent.InitializeEvent(comparer));
                }
                else
                {
                    this.DictionaryActor = this.Runtime.CreateActor(typeof(SharedDictionaryActor<TKey, TValue>));
                }
            }

            /// <summary>
            /// Adds a new key to the dictionary, if it doesn't already exist in the dictionary.
            /// </summary>
            public override bool TryAdd(TKey key, TValue value)
            {
                var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
                this.Runtime.SendEvent(this.DictionaryActor, SharedDictionaryEvent.TryAddEvent(key, value, op.Actor.Id));
                var e = op.Actor.ReceiveEventAsync(typeof(SharedDictionaryResponseEvent<bool>)).Result as SharedDictionaryResponseEvent<bool>;
                return e.Value;
            }

            /// <summary>
            /// Updates the value for an existing key in the dictionary, if that key has a specific value.
            /// </summary>
            public override bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
            {
                var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
                this.Runtime.SendEvent(this.DictionaryActor, SharedDictionaryEvent.TryUpdateEvent(key, newValue, comparisonValue, op.Actor.Id));
                var e = op.Actor.ReceiveEventAsync(typeof(SharedDictionaryResponseEvent<bool>)).Result as SharedDictionaryResponseEvent<bool>;
                return e.Value;
            }

            /// <summary>
            /// Attempts to get the value associated with the specified key.
            /// </summary>
            public override bool TryGetValue(TKey key, out TValue value)
            {
                var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
                this.Runtime.SendEvent(this.DictionaryActor, SharedDictionaryEvent.TryGetEvent(key, op.Actor.Id));
                var e = op.Actor.ReceiveEventAsync(typeof(SharedDictionaryResponseEvent<Tuple<bool, TValue>>)).Result
                    as SharedDictionaryResponseEvent<Tuple<bool, TValue>>;
                value = e.Value.Item2;
                return e.Value.Item1;
            }

            /// <summary>
            /// Gets or sets the value associated with the specified key.
            /// </summary>
            public override TValue this[TKey key]
            {
                get
                {
                    var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
                    this.Runtime.SendEvent(this.DictionaryActor, SharedDictionaryEvent.GetEvent(key, op.Actor.Id));
                    var e = op.Actor.ReceiveEventAsync(typeof(SharedDictionaryResponseEvent<TValue>)).Result as SharedDictionaryResponseEvent<TValue>;
                    return e.Value;
                }

                set
                {
                    this.Runtime.SendEvent(this.DictionaryActor, SharedDictionaryEvent.SetEvent(key, value));
                }
            }

            /// <summary>
            /// Removes the specified key from the dictionary.
            /// </summary>
            public override bool TryRemove(TKey key, out TValue value)
            {
                var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
                this.Runtime.SendEvent(this.DictionaryActor, SharedDictionaryEvent.TryRemoveEvent(key, op.Actor.Id));
                var e = op.Actor.ReceiveEventAsync(typeof(SharedDictionaryResponseEvent<Tuple<bool, TValue>>)).Result
                    as SharedDictionaryResponseEvent<Tuple<bool, TValue>>;
                value = e.Value.Item2;
                return e.Value.Item1;
            }

            /// <summary>
            /// Gets the number of elements in the dictionary.
            /// </summary>
            public override int Count
            {
                get
                {
                    var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
                    this.Runtime.SendEvent(this.DictionaryActor, SharedDictionaryEvent.CountEvent(op.Actor.Id));
                    var e = op.Actor.ReceiveEventAsync(typeof(SharedDictionaryResponseEvent<int>)).Result as SharedDictionaryResponseEvent<int>;
                    return e.Value;
                }
            }
        }
    }

    /// <summary>
    /// A thread-safe dictionary that can be shared in-memory by actors.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class SharedDictionary<TKey, TValue>
    {
        /// <summary>
        /// The dictionary.
        /// </summary>
        private readonly ConcurrentDictionary<TKey, TValue> Dictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedDictionary{TKey, TValue}"/> class.
        /// </summary>
        internal SharedDictionary(ConcurrentDictionary<TKey, TValue> dictionary)
        {
            this.Dictionary = dictionary;
        }

        /// <summary>
        /// Adds a new key to the dictionary, if it doesn't already exist in the dictionary.
        /// </summary>
        public virtual bool TryAdd(TKey key, TValue value) => this.Dictionary.TryAdd(key, value);

        /// <summary>
        /// Updates the value for an existing key in the dictionary, if that key has a specific value.
        /// </summary>
        public virtual bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue) =>
            this.Dictionary.TryUpdate(key, newValue, comparisonValue);

        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// </summary>
        public virtual bool TryGetValue(TKey key, out TValue value) => this.Dictionary.TryGetValue(key, out value);

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        public virtual TValue this[TKey key]
        {
            get => this.Dictionary[key];

            set
            {
                this.Dictionary[key] = value;
            }
        }

        /// <summary>
        /// Removes the specified key from the dictionary.
        /// </summary>
        public virtual bool TryRemove(TKey key, out TValue value) => this.Dictionary.TryRemove(key, out value);

        /// <summary>
        /// Gets the number of elements in the dictionary.
        /// </summary>
        public virtual int Count
        {
            get => this.Dictionary.Count;
        }
    }
}
