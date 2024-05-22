namespace Plang.CSharpRuntime.Values
{
    public interface IPrtMutableValue : IPrtValue
    {
        void Freeze();
    }
}