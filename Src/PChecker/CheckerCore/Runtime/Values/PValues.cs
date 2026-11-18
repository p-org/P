namespace PChecker.Runtime.Values
{
    public static class PValues
    {
        public static PBool Box(bool value)
        {
            return value;
        }

        public static PInt Box(long value)
        {
            return new PInt(value);
        }

        public static PInt Box(int value)
        {
            return new PInt(value);
        }

        public static PInt Box(short value)
        {
            return new PInt(value);
        }

        public static PInt Box(byte value)
        {
            return new PInt(value);
        }

        public static PFloat Box(double value)
        {
            return new PFloat(value);
        }

        public static PFloat Box(float value)
        {
            return new PFloat(value);
        }

        public static PBool SafeEquals(IPValue val1, IPValue val2)
        {
            return ReferenceEquals(val1, val2) || val1 != null && val1.Equals(val2);
        }

        /// <summary>
        /// Deep-copies a value. Primitives and machine references are immutable, so the copy
        /// shares them instead of boxing a new instance.
        /// </summary>
        public static IPValue Clone(IPValue value) =>
            value is PInt or PBool or PFloat or PString or PMachineValue ? value : value?.Clone();

        public static IPValue PCastValue(IPValue value, PType type)
        {
            //todo: Needs to be fixed for better error message
            /*if (!PInhabitsType(value, type))
                throw new PInhabitsTypeException(
                    $"value {value.ToString()} is not a member of type {type.ToString()}");*/
            return value.Clone();
        }
    }
}