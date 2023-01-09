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
            OutputDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            Output = new DefaultCompilerOutput(OutputDirectory);
            ProjectName = "generatedOutput";
            ProjectRootPath = null;
            LocationResolver = new DefaultLocationResolver();
            Handler = new DefaultTranslationErrorHandler(LocationResolver);
            OutputLanguage = CompilerOutput.CSharp;
            Backend = TargetLanguage.GetCodeGenerator(OutputLanguage);
            ProjectDependencies = new List<string>();
        }
        public CompilerConfiguration(ICompilerOutput output, DirectoryInfo outputDir, CompilerOutput outputLanguage, IList<string> inputFiles,
            string projectName, DirectoryInfo projectRoot = null, IList<string> projectDependencies = null)
        {
            if (!inputFiles.Any())
            {
                throw new ArgumentException("Must supply at least one input file", nameof(inputFiles));
            }

            Output = output;
            OutputDirectory = outputDir;
            InputFiles = inputFiles;
            ProjectName = projectName ?? Path.GetFileNameWithoutExtension(inputFiles[0]);
            ProjectRootPath = projectRoot;
            LocationResolver = new DefaultLocationResolver();
            Handler = new DefaultTranslationErrorHandler(LocationResolver);
            OutputLanguage = outputLanguage;
            Backend = TargetLanguage.GetCodeGenerator(outputLanguage);
            ProjectDependencies = projectDependencies ?? new List<string>();
        }
        
        public ICompilerOutput Output { get; set; }
        public DirectoryInfo OutputDirectory { get; set; }
        public CompilerOutput OutputLanguage { get; set; }
        public string ProjectName { get; set; }
        public DirectoryInfo ProjectRootPath { get; set; }
        public ICodeGenerator Backend { get; set; }
        public IList<string> InputFiles { get; set; }
        public ILocationResolver LocationResolver { get; set;  }
        public ITranslationErrorHandler Handler { get; set; }

        public IList<string> ProjectDependencies { get; set;  }

        public void Copy(CompilerConfiguration parsedConfig)
        {
            this.Backend = parsedConfig.Backend;
            this.InputFiles = parsedConfig.InputFiles;
            this.OutputDirectory = parsedConfig.OutputDirectory;
            this.Handler = parsedConfig.Handler;
            this.Output = parsedConfig.Output;
            this.LocationResolver = parsedConfig.LocationResolver;
            this.ProjectDependencies = parsedConfig.ProjectDependencies;
            this.ProjectName = parsedConfig.ProjectName;
            this.OutputLanguage = parsedConfig.OutputLanguage;
            this.ProjectRootPath = parsedConfig.ProjectRootPath;
        }
    }
}