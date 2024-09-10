namespace PChecker.PRuntime.Values
{
    public interface IPMutableValue : IPValue
    {
        void Freeze();
    }
}