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
        private static string testProg1 = 
        @"[ key1 = 1, key2 = null ]   event e1;
        event e2 assert 4 [ sentBy = M1  ];
        event e3 assume 5;
        event e4 : (machine, foreign);
        event e5 assert 1 : (mach: machine, dat: seq [foreign]);
        main model M1 assume 10 [ schedule=fair  ] { var x, y, z : int;  }
        machine M2 { var x : int; var y : seq[foreign]; var z : (m: machine, c: int); }
        monitor Mon { 
            var x, y, z : int [ size = int32 ]; 
            fun Foo (x: int, y : int) : map[int, int] [ isPassive = true ]
            {
               push S1;
               push Group1.Group2.S3;
            }

            state S [ failure = impossible ]
            {  defer E1, E2, E3; ignore E5; on F goto S;   entry { x = y; y = z; }  }
            state T
            {  on E1, E5 goto S { x = y + 1; }; on E push T; exit { push S; } entry { push T; }  }
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
                Console.WriteLine("{0}, {1}: {2}", f.Span.StartLine, f.Span.StartCol, f.Message);
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
