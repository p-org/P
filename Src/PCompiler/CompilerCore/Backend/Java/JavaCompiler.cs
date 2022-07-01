using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plang.Compiler.TypeChecker;

namespace Plang.Compiler.Backend.Java
{

    public class JavaCompiler : ICodeGenerator
    {
        public void GenerateBuildScript(ICompilationJob job)
        {
            var pomPath = Path.Combine(job.ProjectRootPath.FullName, Constants.BuildFileName);
            if (File.Exists(pomPath))
            {
                job.Output.WriteInfo("Reusing existing " + Constants.BuildFileName);
                return;
            }

            File.WriteAllText(pomPath, Constants.BuildFileTemplate(job.ProjectName));
            job.Output.WriteInfo("Generated " + Constants.BuildFileName);
        }

        /// <summary>
        /// Generates all extracted Java code.  Later, this will also generate FFI stubs if they are
        /// absent.
        /// </summary>
        public IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope scope)
        {
            GenerateBuildScript(job);

            List<ICodeGenerator> generators = new List<ICodeGenerator>()
            {
                new MachineGenerator(Constants.MachineDefnFileName),
                new EventGenerator(Constants.EventDefnFileName),
                new TypesGenerator(Constants.TypesDefnFileName),
                new FFIStubGenerator(Constants.FFIStubFileName)
            };

            return generators.SelectMany(g => g.GenerateCode(job, scope));
        }



        /// <summary>
        /// This compiler has a compilation stage.
        /// </summary>
        public bool HasCompilationStage => true;

        /// <summary>
        /// Collates the previously-generated Java sources into a final JAR.
        /// </summary>
        public void Compile(ICompilationJob job)
        {
            string stdout = "";
            string stderr = "";

            string[] args = { "clean", "package"};
            if (Compiler.RunWithOutput(
                job.ProjectRootPath.FullName, out stdout, out stderr, "mvn", args) != 0)
            {
                throw new TranslationException($"Java project compilation failed.\n" + $"{stdout}\n" + $"{stderr}\n");
            }
        }
    }
}
