using Plang.Compiler.Backend.Java;

namespace Plang.Compiler.Backend.PInfer
{
    internal class PJavaRtDependenciesGenerator : MachineGenerator
    {

        private readonly string _myCode = "";
        public PJavaRtDependenciesGenerator(ICompilerConfiguration job, string filename, string code) : base(job, filename)
        {
            _myCode = code;
        }

        protected override void GenerateCodeImpl()
        {
            Write(_myCode.Replace("%PROJECT_NAME%", Job.ProjectName));
        }
    }
}