namespace PChecker.Runtime.Values
{
    public interface IPMutableValue : IPValue
    {
        void Freeze();
    }
}