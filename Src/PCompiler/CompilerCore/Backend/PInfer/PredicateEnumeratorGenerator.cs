using Plang.Compiler.Backend.Java;

namespace Plang.Compiler.Backend.PInfer
{
    internal class PredicateEnumeratorGenerator : MachineGenerator
    {
        public PredicateEnumeratorGenerator(ICompilerConfiguration job) : base(job, PreambleConstants.PredicateEnumeratorFileName)
        {
        }

        protected override void GenerateCodeImpl()
        {
            Write(PreambleConstants.PredicateEnumeratorProg);
        }
    }
}