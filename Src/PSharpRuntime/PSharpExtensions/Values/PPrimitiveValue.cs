using System.Collections.Generic;

namespace PrtSharp.Values
{
    public abstract class PPrimitiveValue<T> : IPrtValue
    {
        protected readonly T value;

        protected PPrimitiveValue(T value)
        {
            this.value = value;
        }

        public override bool Equals(object val)
        {
            if (ReferenceEquals(null, val))
            {
                return false;
            }

            if (ReferenceEquals(this, val))
            {
                return true;
            }

            if (val.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((PPrimitiveValue<T>) val);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(value);
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public static implicit operator T(PPrimitiveValue<T> prim)
        {
            return prim.value;
        }

        public abstract IPrtValue Clone();

        public bool Equals(IPrtValue other)
        {
            return other is PPrimitiveValue<T> otherV && EqualityComparer<T>.Default.Equals(value, otherV.value);
        }
    }
}