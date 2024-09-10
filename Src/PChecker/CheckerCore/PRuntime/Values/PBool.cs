using System;
using System.Runtime.CompilerServices;

namespace PChecker.PRuntime.Values
{
    [Serializable]
    public readonly struct PBool : IPValue
    {
        public bool Equals(PBool other)
        {
            return value == other.value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is PBool other && Equals(other);
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public object ToDict()
        {
            return value;
        }

        private readonly bool value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PBool(bool value)
        {
            this.value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPValue Clone()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(in PBool val)
        {
            return val.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PBool(bool val)
        {
            return new PBool(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator true(in PBool pValue)
        {
            return pValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator false(in PBool pValue)
        {
            return !pValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator !(in PBool pValue)
        {
            return new PBool(!pValue.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator &(in PBool pValue1, in PBool pValue2)
        {
            return new PBool(pValue1.value && pValue2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator |(in PBool pValue1, in PBool pValue2)
        {
            return new PBool(pValue1.value || pValue2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in PBool pValue1, in PBool pValue2)
        {
            return Equals(pValue1, pValue2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in PBool pValue1, in PBool pValue2)
        {
            return !Equals(pValue1, pValue2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in PBool pValue1, in IPValue pValue2)
        {
            return pValue2 is PBool pBool && pValue1.value == pBool.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in PBool pValue1, in IPValue pValue2)
        {
            return pValue2 is PBool pBool && pValue1.value != pBool.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in IPValue pValue1, in PBool pValue2)
        {
            return pValue1 is PBool pBool && pValue2.value == pBool.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in IPValue pValue1, in PBool pValue2)
        {
            return pValue1 is PBool pBool && pValue2.value != pBool.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(IPValue obj)
        {
            return obj is PBool other && value == other.value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }
    }
}