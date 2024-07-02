using System.Collections.Generic;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.Backend.PInfer
{
    internal class DriverGenerator : MachineGenerator
    {
        private IEnumerable<string> TemplateNames;
        public DriverGenerator(ICompilerConfiguration job, string filename, IEnumerable<string> templateNames) : base(job, filename)
        {
            TemplateNames = templateNames;
        }

        protected override void GenerateCodeImpl()
        {
            string template = PreambleConstants.DriverCodeTemplate;
            string insert = "";
            foreach (string name in TemplateNames)
            {
                insert += @$"case ""{name}"":
                for (List<PEvents.EventBase> trace: traces) {{
                    Templates.{name}.execute(trace, predicates, terms);
                }}
                break;
                ";
            }
            WriteLine(template.Replace("%TEMPLATE%", insert));
        }
    }
}