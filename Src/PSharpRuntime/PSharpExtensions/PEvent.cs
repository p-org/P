using Microsoft.PSharp;

namespace PSharpExtensions
{
    public interface IHasPayload<out T>
    {
        T Payload { get; }
    }

    public class PEvent<T> : Event, IHasPayload<T>
    {

        public PEvent(T payload)
        {
            this.Payload = payload;
        }

        public T Payload { get; set; }
    }
}
