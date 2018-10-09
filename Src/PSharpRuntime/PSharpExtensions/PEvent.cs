using Microsoft.PSharp;

namespace PrtSharp
{
    public interface IEventWithPayload<out T> : IPrtValue
    {
        T Payload { get; }
    }

    public class PEvent<T> : Event, IEventWithPayload<T>
    {
        protected static int AssertVal { get; set; }
        protected static int AssumeVal { get; set; }

        public PEvent() : base(AssertVal, AssumeVal)
        {
            
        }
        public PEvent(T payload): base(AssertVal, AssumeVal)
        {
            this.Payload = payload;
        }

        public T Payload { get; }

        public bool Equals(IPrtValue other)
        {
            return other != null && GetType().FullName.Equals(other.GetType().FullName);
        }

        public IPrtValue Clone()
        {
            return new PEvent<T>();
        }
    } 
}
