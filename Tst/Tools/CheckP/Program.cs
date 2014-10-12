namespace CheckP
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class Program
    {
        private const int FailCode = 1;
        public static void Main(string[] args)
        {
            var checker = new CheckP.Checker(Environment.CurrentDirectory);
            if (!checker.Check())
            {
                Environment.ExitCode = FailCode;
            }
        }
    }
}

