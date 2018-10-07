using System;

namespace PrtSharp.Values
{
    [Serializable]
    public sealed class PrtFloat : PPrimitiveValue<double>
    {
        public PrtFloat() : base(0)
        {
        }

        public PrtFloat(double value) : base(value)
        {
        }

        public override IPrtValue Clone()
        {
            return new PrtFloat(value);
        }

        public override bool Equals(object val)
        {
            return val is PrtFloat other && Equals(value, other.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static implicit operator double(PrtFloat val)
        {
            return val.value;
        }

        public static implicit operator PrtFloat(float val)
        {
            return new PrtFloat(val);
        }

        public static implicit operator PrtFloat(double val)
        {
            return new PrtFloat(val);
        }

        public static PrtFloat operator +(PrtFloat prtFloat1, PrtFloat prtFloat2)
        {
            return new PrtFloat(prtFloat1.value + prtFloat2.value);
        }

        public static PrtFloat operator -(PrtFloat prtFloat1, PrtFloat prtFloat2)
        {
            return new PrtFloat(prtFloat1.value - prtFloat2.value);
        }

        public static PrtFloat operator *(PrtFloat prtFloat1, PrtFloat prtFloat2)
        {
            return new PrtFloat(prtFloat1.value * prtFloat2.value);
        }

        public static PrtFloat operator /(PrtFloat prtFloat1, PrtFloat prtFloat2)
        {
            return new PrtFloat(prtFloat1.value / prtFloat2.value);
        }

        public static PrtBool operator <(PrtFloat prtFloat1, PrtFloat prtFloat2)
        {
            return PrtValues.Box(prtFloat1.value < prtFloat2.value);
        }

        public static PrtBool operator >(PrtFloat prtFloat1, PrtFloat prtFloat2)
        {
            return PrtValues.Box(prtFloat1.value > prtFloat2.value);
        }

        public static PrtBool operator <=(PrtFloat prtFloat1, PrtFloat prtFloat2)
        {
            return PrtValues.Box(prtFloat1.value <= prtFloat2.value);
        }

        public static PrtBool operator >=(PrtFloat prtFloat1, PrtFloat prtFloat2)
        {
            return PrtValues.Box(prtFloat1.value >= prtFloat2.value);
        }

        public static PrtBool operator ==(PrtFloat prtFloat1, PrtFloat prtFloat2)
        {
            return PrtValues.Box(Equals(prtFloat1?.value, prtFloat2?.value));
        }

        public static PrtBool operator !=(PrtFloat prtFloat1, PrtFloat prtFloat2)
        {
            return PrtValues.Box(Equals(prtFloat1?.value, prtFloat2?.value) == false);
        }

        public static PrtFloat operator +(PrtFloat prtFloat)
        {
            return new PrtFloat(+prtFloat.value);
        }

        public static PrtFloat operator -(PrtFloat prtFloat)
        {
            return new PrtFloat(-prtFloat.value);
        }
    }
}