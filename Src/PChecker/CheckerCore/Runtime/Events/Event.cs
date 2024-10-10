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
        
        public IPValue Payload { get; set; }
        
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
            return this.GetType().Name;
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
        public PHalt(IPValue payload) : base(payload)
        {
        }
    }
}