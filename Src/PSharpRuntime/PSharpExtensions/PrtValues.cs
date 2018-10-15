using PrtSharp.Values;

namespace PrtSharp
{
    public static class PrtValues
    {
        public static PrtBool Box(bool value)
        {
            return value;
        }

        public static PrtInt Box(long value)
        {
            return new PrtInt(value);
        }

        public static PrtInt Box(int value)
        {
            return new PrtInt(value);
        }

        public static PrtInt Box(short value)
        {
            return new PrtInt(value);
        }

        public static PrtInt Box(byte value)
        {
            return new PrtInt(value);
        }

        public static PrtFloat Box(double value)
        {
            return new PrtFloat(value);
        }

        public static PrtFloat Box(float value)
        {
            return new PrtFloat(value);
        }
    }
}