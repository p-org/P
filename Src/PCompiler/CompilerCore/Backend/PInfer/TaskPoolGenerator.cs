using Plang.Compiler.Backend.Java;

namespace Plang.Compiler.Backend.PInfer
{
    internal class TaskPookGenerator : MachineGenerator
    {
        public TaskPookGenerator(ICompilerConfiguration job) : base(job, PreambleConstants.TaskPoolFileName)
        {}

        protected override void GenerateCodeImpl()
        {
            Write(PreambleConstants.TaskPoolProg
                    .Replace("%PROJECT_NAME%", Job.ProjectName)
                    .Replace("%PARSEFILE%", PreambleConstants.ParseFileName));
        }
    }
}