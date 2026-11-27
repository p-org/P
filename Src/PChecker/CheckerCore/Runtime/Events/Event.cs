// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using PChecker.Runtime.Values;

namespace PChecker.Runtime.Events
{
    /// <summary>
    /// Class representing an event.
    /// </summary>
    [DataContract]
    public class Event: IPValue
    {
        /// <summary>
        /// Initializes an event without a payload.
        /// </summary>
        public Event()
        {
        }

        public Event(IPValue payload)
        {
            Payload = payload;
        }

        /// <summary>
        /// Initializes an event whose payload is the default value of its payload type, which is
        /// built when the payload is first read: generated code creates events like this, and
        /// usually assigns their payload right after.
        /// </summary>
        protected Event(LazyDefaultPayload _)
        {
            isDefaultPayloadPending = true;
        }
        
        private IPValue payload;
        private bool isDefaultPayloadPending;

        /// <summary>
        /// The payload.
        /// </summary>
        public IPValue Payload
        {
            get
            {
                if (isDefaultPayloadPending)
                {
                    payload = CreateDefaultPayload();
                    isDefaultPayloadPending = false;
                }

                return payload;
            }
            set
            {
                payload = value;
                isDefaultPayloadPending = false;
            }
        }

        /// <summary>
        /// Returns the default value of the payload type.
        /// </summary>
        protected virtual IPValue CreateDefaultPayload() => null;
        
        public bool Equals(IPValue other)
        {
            return other != null && GetType().FullName.Equals(other.GetType().FullName);
        }

        public virtual IPValue Clone()
        {
            throw new NotImplementedException();
        }
        
        public object ToDict()
        {
            return GetType().Name;
        }

        public override bool Equals(object obj)
        {
            return obj is Event other && Equals(other);
        }

        public override int GetHashCode()
        {
            return GetType().FullName.GetHashCode();
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
    
    /// <summary>
    /// Selects the event constructor that gives the event the default payload of its type.
    /// For generated code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct LazyDefaultPayload
    {
    }

    public class PHalt : Event
    {
        public PHalt()
        {
        }

        public PHalt(IPValue payload) : base(payload)
        {
        }
    }
}