//-----------------------------------------------------------------------
// <copyright file="StepStateTransitions.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Class representing a collection of step state transitions.
    /// </summary>
    public sealed class StepStateTransitions : IEnumerable<KeyValuePair<Type, Tuple<Type, Action>>>
    {
        /// <summary>
        /// A dictionary of step state transitions. A key represents
        /// the type of an event, and the value is the target state
        /// of the step transition and an optional lambda function,
        /// which can override the default OnExit function of the
        /// exiting state.
        /// </summary>
        private Dictionary<Type, Tuple<Type, Action>> Dictionary;

        /// <summary>
        /// Default constructor of the StepStateTransitions class.
        /// </summary>
        public StepStateTransitions()
        {
            this.Dictionary = new Dictionary<Type, Tuple<Type, Action>>();
        }

        /// <summary>
        /// Adds the specified pair of event, state to transition to, and
        /// an optional lambda function, which can override the default
        /// OnExit function of the exiting state, to the collection.
        /// </summary>
        /// <param name="e">Type of the event</param>
        /// <param name="s">Type of the state</param>
        /// <param name="a">Optional OnExit lambda</param>
        public void Add(Type e, Type s, Action a = null)
        {
            this.Dictionary.Add(e, new Tuple<Type, Action>(s, a));
        }

        /// <summary>
        /// Returns the state to transition to when receiving the
        /// specified type of event.
        /// </summary>
        /// <param name="key">Type of the event</param>
        /// <returns>Type of the state</returns>
        public Tuple<Type, Action> this[Type key]
        {
            internal get
            {
                return this.Dictionary[key];
            }
            set
            {
                this.Dictionary[key] = value;
            }
        }

        /// <summary>
        /// Gets a collection containing the keys.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Type> Keys()
        {
            return this.Dictionary.Keys;
        }

        /// <summary>
        /// Determines whether the collection contains the specified key.
        /// </summary>
        /// <param name="key">Type of the event</param>
        /// <returns>Boolean value</returns>
        public bool ContainsKey(Type key)
        {
            return this.Dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>IEnumerator</returns>
        public IEnumerator<KeyValuePair<Type, Tuple<Type, Action>>> GetEnumerator()
        {
            return this.Dictionary.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>IEnumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)this.Dictionary.GetEnumerator();
        }
    }
}
