using System;
using System.Runtime.CompilerServices;

namespace PrtSharp.Values
{
    [Serializable]
    public sealed class PrtBool : PPrimitiveValue<bool>
    {
        public static readonly PrtBool PrtTrue = new PrtBool(true);
        public static readonly PrtBool PrtFalse = new PrtBool(false);

        private PrtBool() : base(false)
        {
        }

        private PrtBool(bool value) : base(value)
        {
        }

        public override IPrtValue Clone()
        {
            return new PrtBool(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(PrtBool val)
        {
            return val.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrtBool(bool val)
        {
            return val ? PrtTrue : PrtFalse;
        }

        public static bool operator true(PrtBool pValue)
        {
            return pValue;
        }

        public static bool operator false(PrtBool pValue)
        {
            return !pValue;
        }

        public static PrtBool operator !(PrtBool pValue)
        {
            return new PrtBool(!pValue.value);
        }

        public static PrtBool operator &(PrtBool pValue1, PrtBool pValue2)
        {
            return new PrtBool(pValue1.value && pValue2.value);
        }

        public static PrtBool operator |(PrtBool pValue1, PrtBool pValue2)
        {
            return new PrtBool(pValue1.value || pValue2.value);
        }
    }
}