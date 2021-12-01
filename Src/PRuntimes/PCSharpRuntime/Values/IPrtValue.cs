using System;

namespace Plang.CSharpRuntime.Values
{
    public interface IPrtValue : IEquatable<IPrtValue>
    {
        IPrtValue Clone();
    }
}