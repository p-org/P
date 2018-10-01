using System.Collections.Generic;

namespace PrtSharp.PValues
{
    public interface IPrtValue
    {
        IPrtValue Clone();
    }

    internal static class HashHelper
    {
        private const uint FnvPrime = 0x01000193;
        private const uint FnvOffsetBasis = 0x811C9DC5;


        public static int ComputeHash(IEnumerable<object> values)
        {
            unchecked
            {
                uint hash = FnvOffsetBasis;
                foreach (var value in values)
                {
                    hash ^= (uint) value.GetHashCode();
                    hash *= FnvPrime;
                }

                return (int) hash;
            }
        }
    }

    

    public static class PValues
    {
        public static PrtBoolValue Box(bool value)
        {
            return value ? PrtBoolValue.PrtTrue : PrtBoolValue.PrtFalse;
        }

        public static PrtIntValue Box(long value)
        {
            return new PrtIntValue(value);
        }

        public static PrtIntValue Box(int value)
        {
            return new PrtIntValue(value);
        }

        public static PrtIntValue Box(short value)
        {
            return new PrtIntValue(value);
        }

        public static PrtIntValue Box(byte value)
        {
            return new PrtIntValue(value);
        }

        public static PrtFloatValue Box(double value)
        {
            return new PrtFloatValue(value);
        }

        public static PrtFloatValue Box(float value)
        {
            return new PrtFloatValue(value);
        }
    }

    public abstract class PPrimitiveValue<T> : IPrtValue
    {
        protected readonly T value;

        protected PPrimitiveValue(T value)
        {
            this.value = value;
        }

        public override bool Equals(object val)
        {
            return val is PPrimitiveValue<T> other && Equals(value, other.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public static implicit operator T(PPrimitiveValue<T> prtInt)
        {
            return prtInt.value;
        }

        public abstract IPrtValue Clone();
    }
}