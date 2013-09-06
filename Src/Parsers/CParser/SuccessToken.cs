namespace CParser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text;
    using System.Threading;

    internal class SuccessToken
    {
        public bool Succeeded
        {
            get;
            private set;
        }

        public SuccessToken()
        {
            Succeeded = true;
        }

        public void Failed()
        {
            Succeeded = false;
        }
    }
}
