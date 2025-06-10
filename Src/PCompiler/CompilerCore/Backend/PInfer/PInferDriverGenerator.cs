using System.Collections.Generic;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.Backend.PInfer
{
    internal class DriverGenerator : MachineGenerator
    {
        private IEnumerable<string> TemplateNames;
        public DriverGenerator(ICompilerConfiguration job, IEnumerable<string> templateNames) : base(job, PreambleConstants.DriverCodeFileName)
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
                for (List<{Constants.EventsClass()}<?>> ts: eventsTrace) {{
                    Templates.{name}.execute(indices, ts, guards, filters, forallTerms, existsTerms);
                }}
                break;
                ";
            }
            WriteLine(template.Replace("%TEMPLATE%", insert).Replace("%EVENT_BASE%", $"{Constants.EventsClass()}<?>").Replace("%PROJECT_NAME%", Job.ProjectName));
        }
    }
}