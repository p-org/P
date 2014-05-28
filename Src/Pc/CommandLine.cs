namespace Microsoft.Pc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Domains;
    using Microsoft.Formula.API;
    using Microsoft.Formula.API.Generators;
    using Microsoft.Formula.API.Nodes;

    class CommandLine
    {
        private static string testProg1 = @"
        event e1;
        event e2 assert 4;
        event e3 assume 5;
        event e4 : (machine, foreign);
        event e5 assert 1 : (mach: machine, dat: seq [foreign]);
        main model M1 assume 10 { var x, y, z : int;  }
        machine M2 { var x : int; var y : seq[foreign]; var z : (m: machine, c: int); }
        monitor Mon { 
            var x : int; 
            action Foo {  monitor Mon1, foo, x; monitor Mon2, (x, y)[0];  }
        }
        ";

        static void Main(string[] args)
        {
            var parser = new Parser.Parser();
            List<Flag> flags;
            PProgram prog;
            var result = parser.ParseText(new ProgramName("test.p"), testProg1, out flags, out prog);
            foreach (var f in flags)
            {
                Console.WriteLine(f.Message);
            }

            if (!result)
            {
                return;
            }

            AST<Model> model;
            Microsoft.Formula.API.Factory.Instance.MkModel("foo", "P", prog.Terms, out model);
            model.Print(System.Console.Out);
        }
    }
}
