using System;

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

        public static PrtInt operator +(PrtInt prtInt1, PrtInt prtInt2)
        {
            return new PrtInt(prtInt1.value + prtInt2.value);
        }

        public static PrtInt operator -(PrtInt prtInt1, PrtInt prtInt2)
        {
            return new PrtInt(prtInt1.value - prtInt2.value);
        }

        public static PrtInt operator *(PrtInt prtInt1, PrtInt prtInt2)
        {
            return new PrtInt(prtInt1.value * prtInt2.value);
        }

        public static PrtInt operator /(PrtInt prtInt1, PrtInt prtInt2)
        {
            return new PrtInt(prtInt1.value / prtInt2.value);
        }

        public static PrtBool operator <(PrtInt prtInt1, PrtInt prtInt2)
        {
            return PrtValues.Box(prtInt1.value < prtInt2.value);
        }

        public static PrtBool operator >(PrtInt prtInt1, PrtInt prtInt2)
        {
            return PrtValues.Box(prtInt1.value > prtInt2.value);
        }

        public static PrtBool operator <=(PrtInt prtInt1, PrtInt prtInt2)
        {
            return PrtValues.Box(prtInt1.value <= prtInt2.value);
        }

        public static PrtBool operator >=(PrtInt prtInt1, PrtInt prtInt2)
        {
            return PrtValues.Box(prtInt1.value >= prtInt2.value);
        }

        public static PrtBool operator ==(PrtInt prtInt1, PrtInt prtInt2)
        {
            return PrtValues.Box(Equals(prtInt1?.value, prtInt2?.value));
        }

        public static PrtBool operator !=(PrtInt prtInt1, PrtInt prtInt2)
        {
            return PrtValues.Box(Equals(prtInt1?.value, prtInt2?.value) == false);
        }

        public static PrtInt operator +(PrtInt prtInt)
        {
            return new PrtInt(+prtInt.value);
        }

        public static PrtInt operator -(PrtInt prtInt)
        {
            return new PrtInt(-prtInt.value);
        }
    }
}