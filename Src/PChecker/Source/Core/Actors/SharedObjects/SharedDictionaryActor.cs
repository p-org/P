// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Actors.SharedObjects
{
    /// <summary>
    /// A shared dictionary modeled using an actor for testing.
    /// </summary>
    [OnEventDoAction(typeof(SharedDictionaryEvent), nameof(ProcessEvent))]
    internal sealed class SharedDictionaryActor<TKey, TValue> : Actor
    {
        /// <summary>
        /// The internal shared dictionary.
        /// </summary>
        private Dictionary<TKey, TValue> Dictionary;

        /// <summary>
        /// Initializes the actor.
        /// </summary>
        protected override Task OnInitializeAsync(Event initialEvent)
        {
            if (initialEvent is SharedDictionaryEvent e)
            {
                if (e.Operation == SharedDictionaryEvent.OperationType.Initialize && e.Comparer != null)
                {
                    this.Dictionary = new Dictionary<TKey, TValue>(e.Comparer as IEqualityComparer<TKey>);
                }
                else
                {
                    throw new ArgumentException("Incorrect arguments provided to SharedDictionary.");
                }
            }
            else
            {
                this.Dictionary = new Dictionary<TKey, TValue>();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Processes the next dequeued event.
        /// </summary>
        private void ProcessEvent(Event e)
        {
            var opEvent = e as SharedDictionaryEvent;
            switch (opEvent.Operation)
            {
                case SharedDictionaryEvent.OperationType.TryAdd:
                    if (this.Dictionary.ContainsKey((TKey)opEvent.Key))
                    {
                        this.SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<bool>(false));
                    }
                    else
                    {
                        this.Dictionary[(TKey)opEvent.Key] = (TValue)opEvent.Value;
                        this.SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<bool>(true));
                    }

                    break;

                case SharedDictionaryEvent.OperationType.TryUpdate:
                    if (!this.Dictionary.ContainsKey((TKey)opEvent.Key))
                    {
                        this.SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<bool>(false));
                    }
                    else
                    {
                        var currentValue = this.Dictionary[(TKey)opEvent.Key];
                        if (currentValue.Equals((TValue)opEvent.ComparisonValue))
                        {
                            this.Dictionary[(TKey)opEvent.Key] = (TValue)opEvent.Value;
                            this.SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<bool>(true));
                        }
                        else
                        {
                            this.SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<bool>(false));
                        }
                    }

                    break;

                case SharedDictionaryEvent.OperationType.TryGet:
                    if (!this.Dictionary.ContainsKey((TKey)opEvent.Key))
                    {
                        this.SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(false, default(TValue))));
                    }
                    else
                    {
                        this.SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(true, this.Dictionary[(TKey)opEvent.Key])));
                    }

                    break;

                case SharedDictionaryEvent.OperationType.Get:
                    this.SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<TValue>(this.Dictionary[(TKey)opEvent.Key]));
                    break;

                case SharedDictionaryEvent.OperationType.Set:
                    this.Dictionary[(TKey)opEvent.Key] = (TValue)opEvent.Value;
                    break;

                case SharedDictionaryEvent.OperationType.Count:
                    this.SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<int>(this.Dictionary.Count));
                    break;

                case SharedDictionaryEvent.OperationType.TryRemove:
                    if (this.Dictionary.ContainsKey((TKey)opEvent.Key))
                    {
                        var value = this.Dictionary[(TKey)opEvent.Key];
                        this.Dictionary.Remove((TKey)opEvent.Key);
                        this.SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(true, value)));
                    }
                    else
                    {
                        this.SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(false, default(TValue))));
                    }

                    break;
            }
        }
    }
}
