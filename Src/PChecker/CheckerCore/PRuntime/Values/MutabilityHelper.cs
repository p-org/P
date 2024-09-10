namespace PChecker.PRuntime.Values
{
    public static class MutabilityHelper
    {
        public static void EnsureFrozen<T>(T value)
            where T : IPValue
        {
            if (value is IPMutableValue mutable)
            {
                mutable.Freeze();
            }
        }
    }
}