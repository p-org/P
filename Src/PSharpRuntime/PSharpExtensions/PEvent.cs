using System;
using Microsoft.PSharp;

namespace PrtSharp
{
    public interface IEventWithPayload : IPrtValue
    {
        object Payload { get; }
    }

    public class PEvent : Event, IEventWithPayload
    {
        public PEvent() : base(AssertVal, AssumeVal)
        {
        }

        public PEvent(IPrtValue payload) : base(AssertVal, AssumeVal)
        {
            Payload = payload;
        }

        protected static int AssertVal { get; set; }
        protected static int AssumeVal { get; set; }

        public object Payload { get; }

        public bool Equals(IPrtValue other)
        {
            return other != null && GetType().FullName.Equals(other.GetType().FullName);
        }

        public virtual IPrtValue Clone()
        {
            throw new NotImplementedException();
        }
    }

    public class PHalt : PEvent
    {
        public PHalt(IPrtValue payload) : base(payload)
        {
        }
    }
}