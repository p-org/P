using System;

namespace PSharpExtensions.Values
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