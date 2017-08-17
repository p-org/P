using System;

namespace Microsoft.Pc.TypeChecker {
    public class EnumMissingDefaultException : Exception
    {
        public PEnum Enum { get; }

        public EnumMissingDefaultException(PEnum pEnum)
        {
            Enum = pEnum;
        }
    }
}