namespace PrtSharp.PValues
{
    // TODO: generate up to T1, ..., T8
    public interface IReadOnlyPrtTuple<out T1>
    {
        T1 Item1 { get; }
    }

    public interface IReadOnlyPrtTuple<out T1, out T2>
    {
        T1 Item1 { get; }
        T2 Item2 { get; }
    }

    public sealed class PrtTuple<T1, T2> : IPrtValue, IReadOnlyPrtTuple<T1, T2>
        where T1 : class, IPrtValue
        where T2 : class, IPrtValue
    {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }

        public PrtTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        public PrtTuple(IReadOnlyPrtTuple<T1, T2> other)
        {
            Item1 = (T1)other.Item1.Clone();
            Item2 = (T2)other.Item2.Clone();
        }

        public IPrtValue Clone()
        {
            return new PrtTuple<T1, T2>((T1)Item1.Clone(), (T2)Item2.Clone());
        }
    }

    public sealed class PrtTuple<T1> : IPrtValue, IReadOnlyPrtTuple<T1>
        where T1 : class, IPrtValue
    {
        public T1 Item1 { get; set; }

        public PrtTuple(T1 item1)
        {
            Item1 = item1;
        }

        public PrtTuple(IReadOnlyPrtTuple<T1> other)
        {
            Item1 = (T1)other.Item1.Clone();
        }

        public IPrtValue Clone()
        {
            return new PrtTuple<T1>((T1) Item1.Clone());
        }
    }
}