using System;

namespace PrtSharp.Values
{
    public interface IReadOnlyPrtTuple<out T1>
    {
        T1 Item1 { get; }
    }

    public class PrtTuple<T1> : IPrtValue, IReadOnlyPrtTuple<T1>
        where T1 : IPrtValue
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

    public class PrtTuple<T1, T2> : IPrtValue, IReadOnlyPrtTuple<T1, T2>
        where T1 : IPrtValue
        where T2 : IPrtValue
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

    public class PrtTuple<T1, T2, T3> : IPrtValue, IReadOnlyPrtTuple<T1, T2, T3>
        where T1 : IPrtValue
        where T2 : IPrtValue
        where T3 : IPrtValue
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

    public class PrtTuple<T1, T2, T3, T4> : IPrtValue, IReadOnlyPrtTuple<T1, T2, T3, T4>
        where T1 : IPrtValue
        where T2 : IPrtValue
        where T3 : IPrtValue
        where T4 : IPrtValue
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

    public interface IReadOnlyPrtTuple<out T1, out T2, out T3, out T4, out T5>
    {
        T1 Item1 { get; }
        T2 Item2 { get; }
        T3 Item3 { get; }
        T4 Item4 { get; }
        T5 Item5 { get; }
    }

    public class PrtTuple<T1, T2, T3, T4, T5> : IPrtValue, IReadOnlyPrtTuple<T1, T2, T3, T4, T5>
        where T1 : IPrtValue
        where T2 : IPrtValue
        where T3 : IPrtValue
        where T4 : IPrtValue
        where T5 : IPrtValue
    {
        public PrtTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
        }

        public PrtTuple(IReadOnlyPrtTuple<T1, T2, T3, T4, T5> other)
        {
            Item1 = (T1)other.Item1.Clone();
            Item2 = (T2)other.Item2.Clone();
            Item3 = (T3)other.Item3.Clone();
            Item4 = (T4)other.Item4.Clone();
            Item5 = (T5)other.Item5.Clone();
        }

        public IPrtValue Clone()
        {
            return new PrtTuple<T1, T2, T3, T4, T5>((T1)Item1.Clone(), (T2)Item2.Clone(), (T3)Item3.Clone(),
                (T4)Item4.Clone(), (T5)Item5.Clone());
        }

        public bool Equals(IPrtValue other)
        {
            return other is IReadOnlyPrtTuple<T1, T2, T3, T4, T5> tup && Equals(Item1, tup.Item1) &&
                   Equals(Item2, tup.Item2) && Equals(Item3, tup.Item3) && Equals(Item4, tup.Item4) && Equals(Item5, tup.Item5);
        }

        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
        public T4 Item4 { get; set; }
        public T5 Item5 { get; set; }
    }

    public interface IReadOnlyPrtTuple<out T1, out T2, out T3, out T4, out T5, out T6>
    {
        T1 Item1 { get; }
        T2 Item2 { get; }
        T3 Item3 { get; }
        T4 Item4 { get; }
        T5 Item5 { get; }
        T6 Item6 { get; }
    }

    public class PrtTuple<T1, T2, T3, T4, T5, T6> : IPrtValue, IReadOnlyPrtTuple<T1, T2, T3, T4, T5, T6>
        where T1 : IPrtValue
        where T2 : IPrtValue
        where T3 : IPrtValue
        where T4 : IPrtValue
        where T5 : IPrtValue
        where T6: IPrtValue
    {
        public PrtTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
        }

        public PrtTuple(IReadOnlyPrtTuple<T1, T2, T3, T4, T5, T6> other)
        {
            Item1 = (T1)other.Item1.Clone();
            Item2 = (T2)other.Item2.Clone();
            Item3 = (T3)other.Item3.Clone();
            Item4 = (T4)other.Item4.Clone();
            Item5 = (T5)other.Item5.Clone();
            Item6 = (T6)other.Item6.Clone();
        }

        public IPrtValue Clone()
        {
            return new PrtTuple<T1, T2, T3, T4, T5, T6>((T1)Item1.Clone(), (T2)Item2.Clone(), (T3)Item3.Clone(),
                (T4)Item4.Clone(), (T5)Item5.Clone(), (T6)Item6.Clone());
        }

        public bool Equals(IPrtValue other)
        {
            return other is IReadOnlyPrtTuple<T1, T2, T3, T4, T5, T6> tup && Equals(Item1, tup.Item1) &&
                   Equals(Item2, tup.Item2) && Equals(Item3, tup.Item3) && Equals(Item4, tup.Item4) && Equals(Item5, tup.Item5) && Equals(Item6, tup.Item6);
        }

        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
        public T4 Item4 { get; set; }
        public T5 Item5 { get; set; }
        public T6 Item6 { get; set; }
    }

    public interface IReadOnlyPrtTuple<out T1, out T2, out T3, out T4, out T5, out T6, out T7>
    {
        T1 Item1 { get; }
        T2 Item2 { get; }
        T3 Item3 { get; }
        T4 Item4 { get; }
        T5 Item5 { get; }
        T6 Item6 { get; }
        T7 Item7 { get; }
    }

    public class PrtTuple<T1, T2, T3, T4, T5, T6, T7> : IPrtValue, IReadOnlyPrtTuple<T1, T2, T3, T4, T5, T6, T7>
        where T1 : IPrtValue
        where T2 : IPrtValue
        where T3 : IPrtValue
        where T4 : IPrtValue
        where T5 : IPrtValue
        where T6 : IPrtValue
        where T7 : IPrtValue
    {
        public PrtTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
            Item7 = item7;
        }

        public PrtTuple(IReadOnlyPrtTuple<T1, T2, T3, T4, T5, T6, T7> other)
        {
            Item1 = (T1)other.Item1.Clone();
            Item2 = (T2)other.Item2.Clone();
            Item3 = (T3)other.Item3.Clone();
            Item4 = (T4)other.Item4.Clone();
            Item5 = (T5)other.Item5.Clone();
            Item6 = (T6)other.Item6.Clone();
            Item7 = (T7)other.Item7.Clone();
        }

        public IPrtValue Clone()
        {
            return new PrtTuple<T1, T2, T3, T4, T5, T6, T7>((T1)Item1.Clone(), (T2)Item2.Clone(), (T3)Item3.Clone(),
                (T4)Item4.Clone(), (T5)Item5.Clone(), (T6)Item6.Clone(), (T7)Item7.Clone());
        }

        public bool Equals(IPrtValue other)
        {
            return other is IReadOnlyPrtTuple<T1, T2, T3, T4, T5, T6, T7> tup && Equals(Item1, tup.Item1) &&
                   Equals(Item2, tup.Item2) && Equals(Item3, tup.Item3) && Equals(Item4, tup.Item4) && Equals(Item5, tup.Item5) && Equals(Item6, tup.Item6) && Equals(Item7, tup.Item7);
        }

        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
        public T4 Item4 { get; set; }
        public T5 Item5 { get; set; }
        public T6 Item6 { get; set; }
        public T7 Item7 { get; set; }
    }

    public interface IReadOnlyPrtTuple<out T1, out T2, out T3, out T4, out T5, out T6, out T7, out T8>
    {
        T1 Item1 { get; }
        T2 Item2 { get; }
        T3 Item3 { get; }
        T4 Item4 { get; }
        T5 Item5 { get; }
        T6 Item6 { get; }
        T7 Item7 { get; }
        T8 Item8 { get; }
    }
    
    public class PrtTuple<T1, T2, T3, T4, T5, T6, T7, T8> : IPrtValue, IReadOnlyPrtTuple<T1, T2, T3, T4, T5, T6, T7, T8>
        where T1 : IPrtValue
        where T2 : IPrtValue
        where T3 : IPrtValue
        where T4 : IPrtValue
        where T5 : IPrtValue
        where T6 : IPrtValue
        where T7 : IPrtValue
        where T8 : IPrtValue
    {
        public PrtTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
            Item7 = item7;
            Item8 = item8;
        }

        public PrtTuple(IReadOnlyPrtTuple<T1, T2, T3, T4, T5, T6, T7, T8> other)
        {
            Item1 = (T1)other.Item1.Clone();
            Item2 = (T2)other.Item2.Clone();
            Item3 = (T3)other.Item3.Clone();
            Item4 = (T4)other.Item4.Clone();
            Item5 = (T5)other.Item5.Clone();
            Item6 = (T6)other.Item6.Clone();
            Item7 = (T7)other.Item7.Clone();
            Item8 = (T8)other.Item8.Clone();
        }

        public IPrtValue Clone()
        {
            return new PrtTuple<T1, T2, T3, T4, T5, T6, T7, T8>((T1)Item1.Clone(), (T2)Item2.Clone(), (T3)Item3.Clone(),
                (T4)Item4.Clone(), (T5)Item5.Clone(), (T6)Item6.Clone(), (T7)Item7.Clone(), (T8)Item8.Clone());
        }

        public bool Equals(IPrtValue other)
        {
            return other is IReadOnlyPrtTuple<T1, T2, T3, T4, T5, T6, T7, T8> tup && Equals(Item1, tup.Item1) &&
                   Equals(Item2, tup.Item2) && Equals(Item3, tup.Item3) && Equals(Item4, tup.Item4) && Equals(Item5, tup.Item5) && Equals(Item6, tup.Item6) && Equals(Item7, tup.Item7) && Equals(Item8, tup.Item8);
        }

        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
        public T4 Item4 { get; set; }
        public T5 Item5 { get; set; }
        public T6 Item6 { get; set; }
        public T7 Item7 { get; set; }
        public T8 Item8 { get; set; }
    }
}