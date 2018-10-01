using System;

namespace PSharpExtensions
{
    public interface IPrtValue : IEquatable<IPrtValue>
    {
        IPrtValue Clone();
    }
}