namespace Plang.CoyoteRuntime.Values
{
    public interface IPrtMutableValue : IPrtValue
    {
        void Freeze();
    }
}