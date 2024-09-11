using PChecker.PRuntime.Values;

namespace PChecker.PRuntime
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