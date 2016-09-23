using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Pc
{
    internal class Linker
    {
        static void Main(string[] args)
        {
            CommandLineOptions options;
            if (CommandLineOptions.ParseLinkString(args, out options))
            {
                Compiler compiler = new Compiler(true);
                compiler.Link(new StandardOutput(), options);
            }
            return;
        }
    }
}
