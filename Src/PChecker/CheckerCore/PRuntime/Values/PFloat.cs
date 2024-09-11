using System;
using System.Runtime.CompilerServices;

namespace PChecker.PRuntime.Values
{
    [Serializable]
    public readonly struct PFloat : IPValue
    {
        private readonly double value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PFloat(double value)
        {
            this.value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPValue Clone()
        {
            return this;
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
        public bool Equals(IPValue other)
        {
            return other is PFloat f && value == f.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object val)
        {
            return val is PFloat other && Equals(value, other.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator double(in PFloat val)
        {
            return val.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PFloat(float val)
        {
            return new PFloat(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PFloat(double val)
        {
            return new PFloat(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PFloat(PInt val)
        {
            return new PFloat(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PFloat operator +(in PFloat pFloat1, in PFloat pFloat2)
        {
            return new PFloat(pFloat1.value + pFloat2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PFloat operator -(in PFloat pFloat1, in PFloat pFloat2)
        {
            return new PFloat(pFloat1.value - pFloat2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PFloat operator *(in PFloat pFloat1, in PFloat pFloat2)
        {
            return new PFloat(pFloat1.value * pFloat2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PFloat operator /(in PFloat pFloat1, in PFloat pFloat2)
        {
            return new PFloat(pFloat1.value / pFloat2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator <(in PFloat pFloat1, in PFloat pFloat2)
        {
            return pFloat1.value < pFloat2.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator >(in PFloat pFloat1, in PFloat pFloat2)
        {
            return pFloat1.value > pFloat2.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator <=(in PFloat pFloat1, in PFloat pFloat2)
        {
            return pFloat1.value <= pFloat2.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator >=(in PFloat pFloat1, in PFloat pFloat2)
        {
            return pFloat1.value >= pFloat2.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator ==(in PFloat pFloat1, in PFloat pFloat2)
        {
            return Equals(pFloat1.value, pFloat2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator !=(in PFloat pFloat1, in PFloat pFloat2)
        {
            return Equals(pFloat1.value, pFloat2.value) == false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PFloat operator +(in PFloat pFloat)
        {
            return new PFloat(+pFloat.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PFloat operator -(in PFloat pFloat)
        {
            return new PFloat(-pFloat.value);
        }
    }
}