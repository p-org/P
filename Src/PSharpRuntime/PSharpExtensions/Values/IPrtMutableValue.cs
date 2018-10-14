namespace PrtSharp
{
    public interface IPrtMutableValue : IPrtValue
    {
        void Freeze();
    }
}