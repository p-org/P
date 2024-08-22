// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;
using PChecker.PRuntime.Values;

namespace PChecker.StateMachines.Events
{
    /// <summary>
    /// Class representing an event.
    /// </summary>
    [DataContract]
    public class Event: IPrtValue
    {
        public Event()
        {
        }

        public Event(IPrtValue payload)
        {
            Payload = payload;
        }
        
        public IPrtValue Payload { get; }
        
        public bool Equals(IPrtValue other)
        {
            return other != null && GetType().FullName.Equals(other.GetType().FullName);
        }

        public virtual IPrtValue Clone()
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
        public PHalt(IPrtValue payload) : base(payload)
        {
        }
    }
}