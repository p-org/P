namespace PrtSharp.Values
{
    public interface IReadOnlyPrtTuple<out T1>
    {
        T1 Item1 { get; }
    }

    public sealed class PrtTuple<T1> : IPrtValue, IReadOnlyPrtTuple<T1>
        where T1 : class, IPrtValue
    {
        public PrtTuple(T1 item1)
        {
            Item1 = item1;
        }

        public PrtTuple(IReadOnlyPrtTuple<T1> other)
        {
            Item1 = (T1) other.Item1.Clone();
        }

        public IPrtValue Clone()
        {
            return new PrtTuple<T1>((T1) Item1.Clone());
        }

        public bool Equals(IPrtValue other)
        {
            return other is IReadOnlyPrtTuple<T1> tup && Equals(Item1, tup.Item1);
        }

        public T1 Item1 { get; set; }
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
        public PrtTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        public PrtTuple(IReadOnlyPrtTuple<T1, T2> other)
        {
            Item1 = (T1) other.Item1.Clone();
            Item2 = (T2) other.Item2.Clone();
        }

        public IPrtValue Clone()
        {
            return new PrtTuple<T1, T2>((T1) Item1.Clone(), (T2) Item2.Clone());
        }

        public bool Equals(IPrtValue other)
        {
            return other is IReadOnlyPrtTuple<T1, T2> tup && Equals(Item1, tup.Item1) && Equals(Item2, tup.Item2);
        }

        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
    }

    public interface IReadOnlyPrtTuple<out T1, out T2, out T3>
    {
        T1 Item1 { get; }
        T2 Item2 { get; }
        T3 Item3 { get; }
    }

    public sealed class PrtTuple<T1, T2, T3> : IPrtValue, IReadOnlyPrtTuple<T1, T2, T3>
        where T1 : class, IPrtValue
        where T2 : class, IPrtValue
        where T3 : class, IPrtValue
    {
        public PrtTuple(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }

        public PrtTuple(IReadOnlyPrtTuple<T1, T2, T3> other)
        {
            Item1 = (T1) other.Item1.Clone();
            Item2 = (T2) other.Item2.Clone();
            Item3 = (T3) other.Item3.Clone();
        }

        public IPrtValue Clone()
        {
            return new PrtTuple<T1, T2, T3>((T1) Item1.Clone(), (T2) Item2.Clone(), (T3) Item3.Clone());
        }

        public bool Equals(IPrtValue other)
        {
            return other is IReadOnlyPrtTuple<T1, T2, T3> tup && Equals(Item1, tup.Item1) && Equals(Item2, tup.Item2) &&
                   Equals(Item3, tup.Item3);
        }

        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
    }

    public interface IReadOnlyPrtTuple<out T1, out T2, out T3, out T4>
    {
        T1 Item1 { get; }
        T2 Item2 { get; }
        T3 Item3 { get; }
        T4 Item4 { get; }
    }

    public sealed class PrtTuple<T1, T2, T3, T4> : IPrtValue, IReadOnlyPrtTuple<T1, T2, T3, T4>
        where T1 : class, IPrtValue
        where T2 : class, IPrtValue
        where T3 : class, IPrtValue
        where T4 : class, IPrtValue
    {
        public PrtTuple(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
        }

        public PrtTuple(IReadOnlyPrtTuple<T1, T2, T3, T4> other)
        {
            Item1 = (T1) other.Item1.Clone();
            Item2 = (T2) other.Item2.Clone();
            Item3 = (T3) other.Item3.Clone();
            Item4 = (T4) other.Item4.Clone();
        }

        public IPrtValue Clone()
        {
            return new PrtTuple<T1, T2, T3, T4>((T1) Item1.Clone(), (T2) Item2.Clone(), (T3) Item3.Clone(),
                (T4) Item4.Clone());
        }

        public bool Equals(IPrtValue other)
        {
            return other is IReadOnlyPrtTuple<T1, T2, T3, T4> tup && Equals(Item1, tup.Item1) &&
                   Equals(Item2, tup.Item2) && Equals(Item3, tup.Item3) && Equals(Item4, tup.Item4);
        }

        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
        public T4 Item4 { get; set; }
    }
}