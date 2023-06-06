using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plang.Compiler.TypeChecker;

namespace Plang.Compiler.Backend.Java
{

    public class JavaCompiler : ICodeGenerator
    {
        public void GenerateBuildScript(ICompilerConfiguration job)
        {
            var pomPath = Path.Combine(job.OutputDirectory.FullName, Constants.BuildFileName);

            File.WriteAllText(pomPath, Constants.BuildFileTemplate(job.ProjectName));
            job.Output.WriteInfo("Generated " + Constants.BuildFileName);
        }

        /// <summary>
        /// Generates all extracted Java code.  Later, this will also generate FFI stubs if they are
        /// absent.
        /// </summary>
        public IEnumerable<CompiledFile> GenerateCode(ICompilerConfiguration job, Scope scope)
        {
            GenerateBuildScript(job);

            var generators = new List<JavaSourceGenerator>()
            {
                new TypesGenerator(Constants.TypesDefnFileName),
                new EventGenerator(Constants.EventDefnFileName),
                new MachineGenerator(Constants.MachineDefnFileName),
                new FFIStubGenerator(Constants.FFIStubFileName)
            };

            var ctx = new CompilationContext(job);
            return generators.SelectMany(g => g.GenerateCode(ctx, scope));
        }



        /// <summary>
        /// This compiler has a compilation stage.
        /// </summary>
        public bool HasCompilationStage => true;

        /// <summary>
        /// Collates the previously-generated Java sources into a final JAR.
        /// </summary>
        public void Compile(ICompilerConfiguration job)
        {
            var stdout = "";
            var stderr = "";

            string[] args = { "clean", "package"};
            if (Compiler.RunWithOutput(
                job.OutputDirectory.FullName, out stdout, out stderr, "mvn", args) != 0)
            {
                throw new TranslationException($"Java project compilation failed.\n" + $"{stdout}\n" + $"{stderr}\n");
            }
        }
    }
}
