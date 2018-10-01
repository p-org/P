using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PSharpExtensions
{
    public interface IPrtValue
    {
        IPrtValue Clone();
    }

    internal static class HashHelper
    {
        private const uint FnvPrime = 0x01000193;
        private const uint FnvOffsetBasis = 0x811C9DC5;


        public static int ComputeHash(IEnumerable<object> values)
        {
            unchecked
            {
                uint hash = FnvOffsetBasis;
                foreach (var value in values)
                {
                    hash ^= (uint) value.GetHashCode();
                    hash *= FnvPrime;
                }

                return (int) hash;
            }
        }
    }

    // TODO: generate up to T1, ..., T8
    public interface IReadOnlyPrtTuple<out T1>
    {
        T1 Item1 { get; }
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

    public sealed class PrtSequence<T> : IPrtValue, IReadOnlyList<T>
        where T : class, IPrtValue
    {
        private readonly List<T> values = new List<T>();
        private bool isDirty;
        private int hashCode;

        public PrtSequence()
        {
            this.hashCode = ComputeHashCode();
        }

        public PrtSequence(IEnumerable<T> values)
        {
            this.values = values.ToList();
            this.hashCode = ComputeHashCode();
        }

        private int ComputeHashCode()
        {
            return HashHelper.ComputeHash(values);
        }

        public IPrtValue Clone()
        {
            return new PrtSequence<T>(values.Select(item => item.Clone()).Cast<T>());
        }

        public void Add(T item)
        {
            values.Add(item);
            isDirty = true;
        }

        public void Insert(int index, T item)
        {
            values.Insert(index, item);
            isDirty = true;
        }

        public void RemoveAt(int index)
        {
            values.RemoveAt(index);
            isDirty = true;
        }

        private bool Equals(PrtSequence<T> other)
        {
            return values.SequenceEqual(other.values);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((PrtSequence<T>) obj);
        }

        public override int GetHashCode()
        {
            if (isDirty)
            {
                this.hashCode = ComputeHashCode();
            }

            return this.hashCode;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => values.Count;

        public T this[int index] => values[index];

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("(");
            string sep = "";
            foreach (T value in values)
            {
                sb.Append(sep);
                sb.Append(value);
                sep = ", ";
            }

            sb.Append(")");
            return sb.ToString();
        }
    }

    public static class PValues
    {
        public static PrtBoolValue Box(bool value)
        {
            return value ? PrtBoolValue.PrtTrue : PrtBoolValue.PrtFalse;
        }

        public static PrtIntValue Box(long value)
        {
            return new PrtIntValue(value);
        }

        public static PrtIntValue Box(int value)
        {
            return new PrtIntValue(value);
        }

        public static PrtIntValue Box(short value)
        {
            return new PrtIntValue(value);
        }

        public static PrtIntValue Box(byte value)
        {
            return new PrtIntValue(value);
        }

        public static PrtFloatValue Box(double value)
        {
            return new PrtFloatValue(value);
        }

        public static PrtFloatValue Box(float value)
        {
            return new PrtFloatValue(value);
        }
    }

    public abstract class PPrimitiveValue<T> : IPrtValue
    {
        protected readonly T value;

        protected PPrimitiveValue(T value)
        {
            this.value = value;
        }

        public override bool Equals(object val)
        {
            return val is PPrimitiveValue<T> other && Equals(value, other.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public static implicit operator T(PPrimitiveValue<T> prtInt)
        {
            return prtInt.value;
        }

        public abstract IPrtValue Clone();
    }

    [Serializable]
    public sealed class PrtFloatValue : PPrimitiveValue<double>
    {
        public PrtFloatValue() : base(0)
        {
        }

        public PrtFloatValue(double value) : base(value)
        {
        }

        public override IPrtValue Clone()
        {
            return new PrtFloatValue(value);
        }

        public override bool Equals(object val)
        {
            return val is PrtFloatValue other && Equals(value, other.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static PrtFloatValue operator +(PrtFloatValue prtFloat1, PrtFloatValue prtFloat2)
        {
            return new PrtFloatValue(prtFloat1.value + prtFloat2.value);
        }

        public static PrtFloatValue operator -(PrtFloatValue prtFloat1, PrtFloatValue prtFloat2)
        {
            return new PrtFloatValue(prtFloat1.value - prtFloat2.value);
        }

        public static PrtFloatValue operator *(PrtFloatValue prtFloat1, PrtFloatValue prtFloat2)
        {
            return new PrtFloatValue(prtFloat1.value * prtFloat2.value);
        }

        public static PrtFloatValue operator /(PrtFloatValue prtFloat1, PrtFloatValue prtFloat2)
        {
            return new PrtFloatValue(prtFloat1.value / prtFloat2.value);
        }

        public static PrtBoolValue operator <(PrtFloatValue prtFloat1, PrtFloatValue prtFloat2)
        {
            return PValues.Box(prtFloat1.value < prtFloat2.value);
        }

        public static PrtBoolValue operator >(PrtFloatValue prtFloat1, PrtFloatValue prtFloat2)
        {
            return PValues.Box(prtFloat1.value > prtFloat2.value);
        }

        public static PrtBoolValue operator <=(PrtFloatValue prtFloat1, PrtFloatValue prtFloat2)
        {
            return PValues.Box(prtFloat1.value <= prtFloat2.value);
        }

        public static PrtBoolValue operator >=(PrtFloatValue prtFloat1, PrtFloatValue prtFloat2)
        {
            return PValues.Box(prtFloat1.value >= prtFloat2.value);
        }

        public static PrtBoolValue operator ==(PrtFloatValue prtFloat1, PrtFloatValue prtFloat2)
        {
            return PValues.Box(Equals(prtFloat1?.value, prtFloat2?.value));
        }

        public static PrtBoolValue operator !=(PrtFloatValue prtFloat1, PrtFloatValue prtFloat2)
        {
            return PValues.Box(Equals(prtFloat1?.value, prtFloat2?.value) == false);
        }

        public static PrtFloatValue operator +(PrtFloatValue prtFloat)
        {
            return new PrtFloatValue(+prtFloat.value);
        }

        public static PrtFloatValue operator -(PrtFloatValue prtFloat)
        {
            return new PrtFloatValue(-prtFloat.value);
        }
    }


    [Serializable]
    public sealed class PrtIntValue : PPrimitiveValue<long>
    {
        public PrtIntValue() : base(0)
        {
        }

        public PrtIntValue(long value) : base(value)
        {
        }

        public override IPrtValue Clone()
        {
            return new PrtIntValue(value);
        }

        public override bool Equals(object val)
        {
            return val is PrtIntValue other && Equals(value, other.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static PrtIntValue operator +(PrtIntValue prtInt1, PrtIntValue prtInt2)
        {
            return new PrtIntValue(prtInt1.value + prtInt2.value);
        }

        public static PrtIntValue operator -(PrtIntValue prtInt1, PrtIntValue prtInt2)
        {
            return new PrtIntValue(prtInt1.value - prtInt2.value);
        }

        public static PrtIntValue operator *(PrtIntValue prtInt1, PrtIntValue prtInt2)
        {
            return new PrtIntValue(prtInt1.value * prtInt2.value);
        }

        public static PrtIntValue operator /(PrtIntValue prtInt1, PrtIntValue prtInt2)
        {
            return new PrtIntValue(prtInt1.value / prtInt2.value);
        }

        public static PrtBoolValue operator <(PrtIntValue prtInt1, PrtIntValue prtInt2)
        {
            return PValues.Box(prtInt1.value < prtInt2.value);
        }

        public static PrtBoolValue operator >(PrtIntValue prtInt1, PrtIntValue prtInt2)
        {
            return PValues.Box(prtInt1.value > prtInt2.value);
        }

        public static PrtBoolValue operator <=(PrtIntValue prtInt1, PrtIntValue prtInt2)
        {
            return PValues.Box(prtInt1.value <= prtInt2.value);
        }

        public static PrtBoolValue operator >=(PrtIntValue prtInt1, PrtIntValue prtInt2)
        {
            return PValues.Box(prtInt1.value >= prtInt2.value);
        }

        public static PrtBoolValue operator ==(PrtIntValue prtInt1, PrtIntValue prtInt2)
        {
            return PValues.Box(Equals(prtInt1?.value, prtInt2?.value));
        }

        public static PrtBoolValue operator !=(PrtIntValue prtInt1, PrtIntValue prtInt2)
        {
            return PValues.Box(Equals(prtInt1?.value, prtInt2?.value) == false);
        }

        public static PrtIntValue operator +(PrtIntValue prtInt)
        {
            return new PrtIntValue(+prtInt.value);
        }

        public static PrtIntValue operator -(PrtIntValue prtInt)
        {
            return new PrtIntValue(-prtInt.value);
        }
    }

    [Serializable]
    public sealed class PrtBoolValue : PPrimitiveValue<bool>
    {
        public static readonly PrtBoolValue PrtTrue = new PrtBoolValue(true);
        public static readonly PrtBoolValue PrtFalse = new PrtBoolValue(false);

        private PrtBoolValue() : base(false)
        {
        }

        private PrtBoolValue(bool value) : base(value)
        {
        }

        public override IPrtValue Clone()
        {
            return new PrtBoolValue(value);
        }

        public static bool operator true(PrtBoolValue pValue)
        {
            return pValue;
        }

        public static bool operator false(PrtBoolValue pValue)
        {
            return !pValue;
        }

        public static PrtBoolValue operator !(PrtBoolValue pValue)
        {
            return new PrtBoolValue(!pValue.value);
        }

        public static PrtBoolValue operator &(PrtBoolValue pValue1, PrtBoolValue pValue2)
        {
            return new PrtBoolValue(pValue1.value && pValue2.value);
        }

        public static PrtBoolValue operator |(PrtBoolValue pValue1, PrtBoolValue pValue2)
        {
            return new PrtBoolValue(pValue1.value || pValue2.value);
        }
    }
}