using System;
using System.Runtime.CompilerServices;

namespace PChecker.PRuntime.Values
{
    [Serializable]
    public readonly struct PInt : IPValue
    {
        private readonly long value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PInt(long value)
        {
            this.value = value;
        }

        public bool Equals(IPValue other)
        {
            return other is PInt i && value == i.value;
        }

        public IPValue Clone()
        {
            return this;
        }

        public override bool Equals(object val)
        {
            return val is PInt other && Equals(value, other.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public object ToDict()
        {
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PInt(byte val)
        {
            return new PInt(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PInt(short val)
        {
            return new PInt(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PInt(int val)
        {
            return new PInt(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PInt(long val)
        {
            return new PInt(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PInt(in PFloat val)
        {
            return new PInt((long)val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(in PInt val)
        {
            return (int)val.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(in PInt val)
        {
            return val.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PInt operator +(in PInt pInt1, in PInt pInt2)
        {
            return new PInt(pInt1.value + pInt2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PInt operator -(in PInt pInt1, in PInt pInt2)
        {
            return new PInt(pInt1.value - pInt2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PInt operator *(in PInt pInt1, in PInt pInt2)
        {
            return new PInt(pInt1.value * pInt2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PInt operator /(in PInt pInt1, in PInt pInt2)
        {
            return new PInt(pInt1.value / pInt2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator <(in PInt pInt1, in PInt pInt2)
        {
            return pInt1.value < pInt2.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator >(in PInt pInt1, in PInt pInt2)
        {
            return pInt1.value > pInt2.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator <=(in PInt pInt1, in PInt pInt2)
        {
            return pInt1.value <= pInt2.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator >=(in PInt pInt1, in PInt pInt2)
        {
            return pInt1.value >= pInt2.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator ==(in PInt pInt1, in PInt pInt2)
        {
            return Equals(pInt1.value, pInt2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator ==(in IPValue pInt1, in PInt pInt2)
        {
            return pInt1 is PInt int1 && Equals(int1.value, pInt2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator !=(in IPValue pInt1, in PInt pInt2)
        {
            return pInt1 is PInt int1 && !Equals(int1.value, pInt2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator ==(in PInt pInt1, in IPValue pInt2)
        {
            return pInt2 is PInt int2 && Equals(pInt1.value, int2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator !=(in PInt pInt1, in IPValue pInt2)
        {
            return pInt2 is PInt int2 && !Equals(pInt1.value, int2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator !=(in PInt pInt1, in PInt pInt2)
        {
            return Equals(pInt1.value, pInt2.value) == false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PInt operator +(in PInt pInt)
        {
            return new PInt(+pInt.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PInt operator -(in PInt pInt)
        {
            return new PInt(-pInt.value);
        }
    }
}