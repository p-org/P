//-----------------------------------------------------------------------
// <copyright file="CallStateTransitions.cs" company="Microsoft">
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
    /// Class representing a collection of call state transitions.
    /// </summary>
    public sealed class CallStateTransitions : IEnumerable<KeyValuePair<Type, Type>>
    {
        /// <summary>
        /// A dictionary of call state transitions. A key represents
        /// the type of an event, and the value is the target state
        /// of the call transition.
        /// </summary>
        private Dictionary<Type, Type> Dictionary;

        /// <summary>
        /// Default constructor of the CallStateTransitions class.
        /// </summary>
        public CallStateTransitions()
        {
            this.Dictionary = new Dictionary<Type, Type>();
        }

        /// <summary>
        /// Adds the specified pair of event and state for transition.
        /// </summary>
        /// <param name="e">Type of the event</param>
        /// <param name="s">Type of the state</param>
        public void Add(Type e, Type s)
        {
            this.Dictionary.Add(e, s);
        }

        /// <summary>
        /// Returns the state to transition to when receiving the
        /// specified type of event.
        /// </summary>
        /// <param name="key">Type of the event</param>
        /// <returns>Type of the state</returns>
        public Type this[Type key]
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
        public IEnumerator<KeyValuePair<Type, Type>> GetEnumerator()
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
