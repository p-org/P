using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.PSharp;

namespace PrtSharp.Values
{
    [Serializable]
    public struct PrtNull : IPrtValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PMachineValue(PrtNull val)
        {
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Event(PrtNull val)
        {
            return new Default();
        }

        public bool Equals(IPrtValue other)
        {
            return other is PrtNull;
        }

        public IPrtValue Clone()
        {
            return new PrtNull();
        }
    }
}
