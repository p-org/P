using Microsoft.PSharp;

namespace PrtSharp
{
    public interface IEventWithPayload : IPrtValue
    {
        object Payload { get; }
    }

    public class PEvent<T> : Event, IEventWithPayload
    {
        public PEvent() : base(AssertVal, AssumeVal)
        {
        }

        public PEvent(T payload) : base(AssertVal, AssumeVal)
        {
            Payload = payload;
        }

        protected static int AssertVal { get; set; }
        protected static int AssumeVal { get; set; }

        public object Payload { get; }
        public T PayloadT => (T) Payload;

        public bool Equals(IPrtValue other)
        {
            return other != null && GetType().FullName.Equals(other.GetType().FullName);
        }

        public IPrtValue Clone()
        {
            return new PEvent<T>();
        }
    }

    public class PHalt : PEvent<IPrtValue>
    {
    }
}