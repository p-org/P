﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PChecker.Actors.Events;
using PChecker.Actors.SharedObjects.Events;

namespace PChecker.Actors.SharedObjects
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
                    Dictionary = new Dictionary<TKey, TValue>(e.Comparer as IEqualityComparer<TKey>);
                }
                else
                {
                    throw new ArgumentException("Incorrect arguments provided to SharedDictionary.");
                }
            }
            else
            {
                Dictionary = new Dictionary<TKey, TValue>();
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
                    if (Dictionary.ContainsKey((TKey)opEvent.Key))
                    {
                        SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<bool>(false));
                    }
                    else
                    {
                        Dictionary[(TKey)opEvent.Key] = (TValue)opEvent.Value;
                        SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<bool>(true));
                    }

                    break;

                case SharedDictionaryEvent.OperationType.TryUpdate:
                    if (!Dictionary.ContainsKey((TKey)opEvent.Key))
                    {
                        SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<bool>(false));
                    }
                    else
                    {
                        var currentValue = Dictionary[(TKey)opEvent.Key];
                        if (currentValue.Equals((TValue)opEvent.ComparisonValue))
                        {
                            Dictionary[(TKey)opEvent.Key] = (TValue)opEvent.Value;
                            SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<bool>(true));
                        }
                        else
                        {
                            SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<bool>(false));
                        }
                    }

                    break;

                case SharedDictionaryEvent.OperationType.TryGet:
                    if (!Dictionary.ContainsKey((TKey)opEvent.Key))
                    {
                        SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(false, default(TValue))));
                    }
                    else
                    {
                        SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(true, Dictionary[(TKey)opEvent.Key])));
                    }

                    break;

                case SharedDictionaryEvent.OperationType.Get:
                    SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<TValue>(Dictionary[(TKey)opEvent.Key]));
                    break;

                case SharedDictionaryEvent.OperationType.Set:
                    Dictionary[(TKey)opEvent.Key] = (TValue)opEvent.Value;
                    break;

                case SharedDictionaryEvent.OperationType.Count:
                    SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<int>(Dictionary.Count));
                    break;

                case SharedDictionaryEvent.OperationType.TryRemove:
                    if (Dictionary.ContainsKey((TKey)opEvent.Key))
                    {
                        var value = Dictionary[(TKey)opEvent.Key];
                        Dictionary.Remove((TKey)opEvent.Key);
                        SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(true, value)));
                    }
                    else
                    {
                        SendEvent(opEvent.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(false, default(TValue))));
                    }

                    break;
            }
        }
    }
}
