namespace Plang.PrtSharp.Values
{
    public interface IPrtMutableValue : IPrtValue
    {
        void Freeze();
    }
}