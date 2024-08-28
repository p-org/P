using Plang.Compiler.Backend.Java;

namespace Plang.Compiler.Backend.PInfer
{
    internal class TraceIndexGenerator : MachineGenerator
    {
        public TraceIndexGenerator(ICompilerConfiguration job) : base(job, PreambleConstants.TraceIndexFileName)
        {}

        protected override void GenerateCodeImpl()
        {
            Write(PreambleConstants.TraceIndexProg);
        }
    }
}