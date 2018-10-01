using System;

namespace PSharpExtensions
{
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
}