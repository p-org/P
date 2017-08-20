using System;

namespace Microsoft.Pc.TypeChecker {
    public class EnumMissingDefaultException : Exception
    {
        public PEnum Enum { get; }

        public EnumMissingDefaultException(PEnum pEnum)
            : base($"{pEnum.Name}: At least one enum element should be numbered 0")
        {
            Enum = pEnum;
        }
    }
}