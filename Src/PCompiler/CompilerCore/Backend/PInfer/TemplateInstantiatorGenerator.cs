using System.IO;
using Plang.Compiler.Backend.Java;

namespace Plang.Compiler.Backend.PInfer
{
    internal class TemplateInstantiatorGenerator : MachineGenerator
    {
        public TemplateInstantiatorGenerator(ICompilerConfiguration job, string filename) : base(job, filename)
        {
        }

        protected override void GenerateCodeImpl()
        {
            Write(PreambleConstants.MainProg);
        }
    }
}