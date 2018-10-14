using System;

namespace PrtSharp
{
    public interface IPrtValue : IEquatable<IPrtValue>
    {
        IPrtValue Clone();
    }
}