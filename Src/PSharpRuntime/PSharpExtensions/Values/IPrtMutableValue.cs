namespace PrtSharp.Values
{
    public interface IPrtMutableValue : IPrtValue
    {
        void Freeze();
    }
}