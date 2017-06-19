using System;

namespace UnitTests
{
    public class BuildSettings
    {
        public static string Platform { get; } = Environment.Is64BitProcess ? "x64" : "x86";

#if DEBUG
        public const string Configuration = "Debug";
#else
        public const string Configuration = "Release";
#endif
    }
}