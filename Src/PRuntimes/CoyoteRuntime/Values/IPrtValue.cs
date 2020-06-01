using System;
using System.Collections.Generic;

namespace Plang.CoyoteRuntime.Values
{
    public interface IPrtValue : IEquatable<IPrtValue>
    {
        IPrtValue Clone();
    }
}