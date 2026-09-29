// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
        public Event()
        {
        }

        public Event(IPValue payload)
        {
            Payload = payload;
        }
        
        private IPValue payload;
        private bool isPayloadSet;

        /// <summary>
        /// The payload. Until assigned, it is the default value of the event's payload type,
        /// built on first read: events are usually created only to have a payload assigned.
        /// </summary>
        public IPValue Payload
        {
            get
            {
                if (!isPayloadSet)
                {
                    payload = CreateDefaultPayload();
                    isPayloadSet = true;
                }

                return payload;
            }
            set
            {
                payload = value;
                isPayloadSet = true;
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