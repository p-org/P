using Plang.Compiler.Backend.Java;

namespace Plang.Compiler.Backend.PInfer
{
    internal class PInferTypesGenerator : TypesGenerator
    {
        internal PInferTypesGenerator(ICompilerConfiguration job, string filename) : base(job, filename)
        {
        }

        protected override void GenerateCodeImpl()
        {
            GenerateCodeImplWrapper(true);
        }
    }
}