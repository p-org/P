using System;

namespace PrtSharp.PValues
{
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
}