using System;

namespace PrtSharp.PValues
{
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