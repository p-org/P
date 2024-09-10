using System;
using System.Runtime.CompilerServices;

namespace PChecker.PRuntime.Values
{
    [Serializable]
    public readonly struct PString : IPValue
    {
        public bool Equals(PString other)
        {
            return string.Equals(value, other.value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is PString other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPValue Clone()
        {
            return this;
        }

        private readonly string value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PString(string val)
        {
            return new PString(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator string(PString val)
        {
            return val.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PString(string value)
        {
            this.value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PString operator +(in PString pString1, in PString pString2)
        {
            return new PString(pString1.value + pString2.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in PString pValue1, in PString pValue2)
        {
            return Equals(pValue1, pValue2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in PString pValue1, in PString pValue2)
        {
            return !Equals(pValue1, pValue2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in PString pValue1, in IPValue pValue2)
        {
            return pValue2 is PString pString && string.Equals(pValue1.value, pString.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in PString pValue1, in IPValue pValue2)
        {
            return pValue2 is PString pString && !string.Equals(pValue1.value, pString.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in IPValue pValue1, in PString pValue2)
        {
            return pValue1 is PString pString && string.Equals(pValue2.value, pString.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in IPValue pValue1, in PString pValue2)
        {
            return pValue1 is PString pString && !string.Equals(pValue2.value, pString.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator <(in PString pString1, in PString pString2)
        {
            return string.Compare(pString1.value, pString2.value) == -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator <(in IPValue pValue1, in PString pValue2)
        {
            return pValue1 is PString pString && string.Compare(pString.value, pValue2.value) == -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator <(in PString pValue1, in IPValue pValue2)
        {
            return pValue2 is PString pString && string.Compare(pString.value, pValue1.value) == -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator >(in PString pString1, in PString pString2)
        {
            return string.Compare(pString1.value, pString2.value) == 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator >(in IPValue pValue1, in PString pValue2)
        {
            return pValue1 is PString pString && string.Compare(pString.value, pValue2.value) == 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator >(in PString pValue1, in IPValue pValue2)
        {
            return pValue2 is PString pString && string.Compare(pString.value, pValue1.value) == 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator <=(in PString pString1, in PString pString2)
        {
            return string.Compare(pString1.value, pString2.value) != 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator <=(in IPValue pValue1, in PString pValue2)
        {
            return pValue1 is PString pString && string.Compare(pString.value, pValue2.value) != 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator <=(in PString pValue1, in IPValue pValue2)
        {
            return pValue2 is PString pString && string.Compare(pString.value, pValue1.value) != 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator >=(in PString pString1, in PString pString2)
        {
            return string.Compare(pString1.value, pString2.value) != -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator >=(in IPValue pValue1, in PString pValue2)
        {
            return pValue1 is PString pString && string.Compare(pString.value, pValue2.value) != -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBool operator >=(in PString pValue1, in IPValue pValue2)
        {
            return pValue2 is PString pString && string.Compare(pString.value, pValue1.value) != -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(IPValue obj)
        {
            return obj is PString other && string.Equals(value, other.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value;
        }

        /// <summary>
        /// Like ToString, but emits a representation of a string literal, surrounded by double-quotes,
        /// and where all interior double-quotes are escaped.
        /// </summary>
        /// <returns></returns>
        public string ToEscapedString()
        {
            var v = value ?? "";
            return $"\"{v.Replace("\"", "\\\"")}\"";
        }


        public object ToDict()
        {
            return ToString();
        }
    }
}