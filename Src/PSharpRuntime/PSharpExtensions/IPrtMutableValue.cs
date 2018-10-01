namespace PSharpExtensions
{
    public interface IPrtMutableValue : IPrtValue
    {
        void Freeze();
    }
}