using Plang.Compiler.Backend.Java;

namespace Plang.Compiler.Backend.PInfer
{
    internal class TemplateInstantiatorGenerator : MachineGenerator
    {
        private readonly int NumQuantifiers;
        public TemplateInstantiatorGenerator(ICompilerConfiguration job, int numQuantifiers) : base(job, PreambleConstants.MainFileName)
        {
            NumQuantifiers = numQuantifiers;
        }

        protected override void GenerateCodeImpl()
        {
            Write(PreambleConstants.MainProg
                    .Replace("%QUANTIFIED_EVENTS%", $"{NumQuantifiers}")
                    .Replace("%PROJECT_NAME%", Job.ProjectName));
        }
    }
}