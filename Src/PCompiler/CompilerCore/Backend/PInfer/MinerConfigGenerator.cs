using System;
using Plang.Compiler.Backend.Java;

namespace Plang.Compiler.Backend.PInfer
{
    internal class MinerConfigGenerator : MachineGenerator
    {
        private readonly int NumQuantifiers;
        private readonly int TotalTerms;
        public MinerConfigGenerator(ICompilerConfiguration job, int numQuantifiers, int totalTerms) : base(job, PreambleConstants.MinerConfigFileName)
        {
            NumQuantifiers = numQuantifiers;
            TotalTerms = totalTerms;
        }

        protected override void GenerateCodeImpl()
        {
            Write(PreambleConstants.MinerConfigProg
                    .Replace("%NUM_TERMS_TO_CHOOSE%", $"{Math.Min(2, TotalTerms)}")
                    .Replace("%QUANTIFIED_EVENTS%", $"{NumQuantifiers}")
                    .Replace("%PROJECT_NAME%", Job.ProjectName));
        }
    }
}