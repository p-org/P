using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plang.Compiler.Backend;
using Plang.Compiler.TypeChecker;

namespace Plang.Compiler
{
    public class CompilerConfiguration : ICompilerConfiguration
    {
        public CompilerConfiguration()
        {
            OutputDirectory = null;
            InputPFiles = new List<string>();
            InputForeignFiles = new List<string>();
            Output = null;
            ProjectName = "generatedOutput";
            PObservePackageName = $"{ProjectName}.pobserve";
            ProjectRootPath = new DirectoryInfo(Directory.GetCurrentDirectory());
            LocationResolver = new DefaultLocationResolver();
            Handler = new DefaultTranslationErrorHandler(LocationResolver);
            OutputLanguages = new List<CompilerOutput>{CompilerOutput.CSharp};
            Backend = null;
            ProjectDependencies = new List<string>();
        }
        public CompilerConfiguration(ICompilerOutput output, DirectoryInfo outputDir, IList<CompilerOutput> outputLanguages, IList<string> inputFiles,
            string projectName, DirectoryInfo projectRoot = null, IList<string> projectDependencies = null, string pObservePackageName = null)
        {
            if (!inputFiles.Any())
            {
                throw new ArgumentException("Must supply at least one input file", nameof(inputFiles));
            }

            Output = output;
            OutputDirectory = outputDir;
            InputPFiles = new List<string>();
            InputForeignFiles = new List<string>();
            foreach (var fileName in inputFiles)
            {
                if (fileName.EndsWith(".p"))
                {
                    InputPFiles.Add(fileName);
                }
                else
                {
                    InputForeignFiles.Add(fileName);
                }
            }
            ProjectName = projectName ?? Path.GetFileNameWithoutExtension(inputFiles[0]);
            PObservePackageName = pObservePackageName ?? $"{ProjectName}.pobserve";
            ProjectRootPath = projectRoot;
            LocationResolver = new DefaultLocationResolver();
            Handler = new DefaultTranslationErrorHandler(LocationResolver);
            OutputLanguages = outputLanguages;
            Backend = null;
            ProjectDependencies = projectDependencies ?? new List<string>();
        }
        
        public ICompilerOutput Output { get; set; }
        public DirectoryInfo OutputDirectory { get; set; }
        public IList<CompilerOutput> OutputLanguages { get; set; }
        public string ProjectName { get; set; }
        public string PObservePackageName { get; set; }
        public DirectoryInfo ProjectRootPath { get; set; }
        public ICodeGenerator Backend { get; set; }
        public IList<string> InputPFiles { get; set; }
        public IList<string> InputForeignFiles { get; set; }
        public ILocationResolver LocationResolver { get; set;  }
        public ITranslationErrorHandler Handler { get; set; }

        public IList<string> ProjectDependencies { get; set;  }

        public void Copy(CompilerConfiguration parsedConfig)
        {
            Backend = parsedConfig.Backend;
            InputPFiles = parsedConfig.InputPFiles;
            InputForeignFiles = parsedConfig.InputForeignFiles;
            OutputDirectory = parsedConfig.OutputDirectory;
            Handler = parsedConfig.Handler;
            Output = parsedConfig.Output;
            LocationResolver = parsedConfig.LocationResolver;
            ProjectDependencies = parsedConfig.ProjectDependencies;
            ProjectName = parsedConfig.ProjectName;
            PObservePackageName = parsedConfig.PObservePackageName;
            OutputLanguages = parsedConfig.OutputLanguages;
            ProjectRootPath = parsedConfig.ProjectRootPath;
        }
    }
}