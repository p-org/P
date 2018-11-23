using System;

namespace Plang.PrtSharp.Values
{
    public interface IPrtValue : IEquatable<IPrtValue>
    {
        IPrtValue Clone();
    }
}