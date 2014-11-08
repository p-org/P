using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swordfish
{
    internal class Boolean
    {
        private bool? Value;

        public Boolean(bool? value)
        {
            this.Value = value;
        }

        public static implicit operator Boolean(bool value)
        {
            return new Boolean(value);
        }

        public static implicit operator bool (Boolean value)
        {
            return (bool)value.Value;
        }

        public bool BooleanValue()
        {
            return this.Value.Value;
        }
    }
}
