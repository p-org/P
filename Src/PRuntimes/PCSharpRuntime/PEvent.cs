using System;
using PChecker.Actors.Events;
using Plang.CSharpRuntime.Values;

namespace Plang.CSharpRuntime
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

        public object ToDict()
        {
            return this.GetType().Name;
        }

        public override bool Equals(object obj)
        {
            return obj is PEvent other && Equals(other);
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

    public class PHalt : PEvent
    {
        public PHalt(IPrtValue payload) : base(payload)
        {
        }
    }
}