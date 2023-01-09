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
        public CompilerConfiguration(ICompilerOutput output, DirectoryInfo outputDir, CompilerOutput outputLanguage, IReadOnlyList<string> inputFiles,
            string projectName, DirectoryInfo projectRoot = null, IReadOnlyList<string> projectDependencies = null)
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
        public IReadOnlyList<string> InputFiles { get; }
        public ILocationResolver LocationResolver { get; }
        public ITranslationErrorHandler Handler { get; }

        public IReadOnlyList<string> ProjectDependencies { get; }
    }
}