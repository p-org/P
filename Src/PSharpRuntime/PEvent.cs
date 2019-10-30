using Microsoft.Coyote;
using Plang.PrtSharp.Values;
using System;

namespace Plang.PrtSharp
{
    public class PEvent : Event, IPrtValue
    {
        public PEvent() : base()
        {
        }

        public PEvent(IPrtValue payload) : base()
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
    }

    public class PHalt : PEvent
    {
        public PHalt(IPrtValue payload) : base(payload)
        {
        }
    }
}