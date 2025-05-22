using Plang.Compiler.Backend.Java;

namespace Plang.Compiler.Backend.PInfer
{
    internal class TermEnumeratorGenerator : MachineGenerator
    {
        public TermEnumeratorGenerator(ICompilerConfiguration job) : base(job, PreambleConstants.TermEnumeratorFileName)
        {
        }

        protected override void GenerateCodeImpl()
        {
            Write(PreambleConstants.TermEnumeratorProg);
        }
    }
}