using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.Backend.PInfer
{
    public class PInferTemplateGenerator : JavaSourceGenerator
    {
        private readonly HashSet<IPExpr> Predicates;
        private readonly IDictionary<IPExpr, HashSet<Variable>> FreeEvents;
        private readonly List<IPExpr> Terms;
        public PInferTemplateGenerator(ICompilerConfiguration job, string filename,
                HashSet<IPExpr> predicates, IEnumerable<IPExpr> terms, IDictionary<IPExpr, HashSet<Variable>> freeEvents, int n, int m, int t) : base(job, filename)
        {
            Predicates = predicates;
            Terms = [.. terms];
            FreeEvents = freeEvents;
        }

        protected override void GenerateCodeImpl()
        {
            WriteLine("public class Templates {");
            // Forall-only template
            // Two Quantifier Two Fields
            WriteLine("}");
        }

        private string GenerateForallTemplate(int numQuantifier, string[] fieldTypes)
        {
            string templateName = $"Forall{numQuantifier}Events{string.Join("", fieldTypes)}";
            WriteLine($"public static class {templateName} {{");
            foreach (var (ty, i) in fieldTypes.Select((val, index) => (val, index)))
            {
                WriteLine($"private {ty} f{i};");
            }
            WriteLine($"public {templateName} ({string.Join(", ", fieldTypes.Select((val, index) => $"{val} f{index}"))}) {{");
            foreach (var (ty, i) in fieldTypes.Select((val, index) => (val, index)))
            {
                WriteLine($"this.f{i} = f{i};");
            }
            WriteLine("}");
            WriteLine($"public void execute(List<PEvents.EventBase> trace, List<{Job.ProjectName}.PredicateWrapper> predicates, String[])");
            WriteLine("}");
            return templateName;
        }
    }
}