using System;
using System.Runtime.CompilerServices;

namespace PrtSharp.Values
{
    [Serializable]
    public sealed class PrtInt : PPrimitiveValue<long>
    {
        public PrtInt() : base(0)
        {
        }

        public PrtInt(long value) : base(value)
        {
        }

        public override IPrtValue Clone()
        {
            return new PrtInt(value);
        }

        public override bool Equals(object val)
        {
            return val is PrtInt other && Equals(value, other.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrtInt(byte val)
        {
            return new PrtInt(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrtInt(short val)
        {
            return new PrtInt(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrtInt(int val)
        {
            return new PrtInt(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrtInt(long val)
        {
            return new PrtInt(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(PrtInt val)
        {
            return val.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrtInt operator +(PrtInt prtInt1, PrtInt prtInt2)
        {
            return new PrtInt(prtInt1.value + prtInt2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrtInt operator -(PrtInt prtInt1, PrtInt prtInt2)
        {
            return new PrtInt(prtInt1.value - prtInt2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrtInt operator *(PrtInt prtInt1, PrtInt prtInt2)
        {
            return new PrtInt(prtInt1.value * prtInt2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrtInt operator /(PrtInt prtInt1, PrtInt prtInt2)
        {
            return new PrtInt(prtInt1.value / prtInt2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrtBool operator <(PrtInt prtInt1, PrtInt prtInt2)
        {
            return prtInt1.value < prtInt2.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrtBool operator >(PrtInt prtInt1, PrtInt prtInt2)
        {
            return prtInt1.value > prtInt2.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrtBool operator <=(PrtInt prtInt1, PrtInt prtInt2)
        {
            return prtInt1.value <= prtInt2.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrtBool operator >=(PrtInt prtInt1, PrtInt prtInt2)
        {
            return prtInt1.value >= prtInt2.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrtBool operator ==(PrtInt prtInt1, PrtInt prtInt2)
        {
            return Equals(prtInt1?.value, prtInt2?.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrtBool operator !=(PrtInt prtInt1, PrtInt prtInt2)
        {
            return Equals(prtInt1?.value, prtInt2?.value) == false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrtInt operator +(PrtInt prtInt)
        {
            return new PrtInt(+prtInt.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrtInt operator -(PrtInt prtInt)
        {
            return new PrtInt(-prtInt.value);
        }
    }
}