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

            // create the pom.xml file
            var pomTemplate = Constants.pomTemplate;
            pomTemplate = pomTemplate.Replace("-package-name-",job.PObservePackageName);
            
            string foreignInclude = "";
            var foreignFiles = job.InputForeignFiles.Where(x => x.EndsWith(".java"));
            if (foreignFiles.Any())
            {
                foreignInclude = Constants.pomForeignTemplate;
                string foreignSourceInclude = "";
                SortedSet<string> foreignFolders = new SortedSet<string>();

                foreach (var fileName in foreignFiles)
                {
                    var folderName = Path.GetDirectoryName(fileName);
                    if (folderName is not null)
                    {
                        foreignFolders.Add(folderName);
                    }
                }
                foreach (var folderName in foreignFolders)
                {
                    foreignSourceInclude += $"                                <source>{folderName}</source>\n";
                }
                foreignInclude = foreignInclude.Replace("-foreign-source-include-", foreignSourceInclude);
            }
            pomTemplate = pomTemplate.Replace("-foreign-include-", foreignInclude);
            
            File.WriteAllText(pomPath, pomTemplate);

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
                new TypesGenerator(job, Constants.TypesDefnFileName),
                new EventGenerator(job, Constants.EventDefnFileName),
                new MachineGenerator(job, Constants.MachineDefnFileName),
                new FFIStubGenerator(job, Constants.FFIStubFileName)
            };

            var ctx = new CompilationContext(job);
            return generators.SelectMany(g => g.GenerateCode(ctx, scope));
        }



        /// <summary>
        /// This compiler has a compilation stage.
        /// </summary>
        public bool HasCompilationStage => false;
    }
}
