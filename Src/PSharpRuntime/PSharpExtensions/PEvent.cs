using Microsoft.PSharp;

namespace PSharpExtensions
{
    public interface IHasPayload<out T>
    {
        T Payload { get; }
    }

    public class PEvent<T> : Event, IHasPayload<T>
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

        public T Payload { get; set; }
    }
}
