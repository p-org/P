using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plang.Compiler.Backend;
using Plang.Compiler.TypeChecker;

namespace Plang.Compiler
{
    public class CompilationJob : ICompilationJob
    {
        public CompilationJob(ICompilerOutput output, DirectoryInfo outputDir, CompilerOutput outputLanguage, IReadOnlyList<string> inputFiles,
            string projectName, DirectoryInfo projectRoot = null, bool generateSourceMaps = false, IReadOnlyList<string> projectDependencies = null)
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
            GenerateSourceMaps = generateSourceMaps;
            ProjectDependencies = projectDependencies ?? new List<string>();
        }

        public bool GenerateSourceMaps { get; }
        public ICompilerOutput Output { get; }
        public DirectoryInfo OutputDirectory { get; }
        public CompilerOutput OutputLanguage { get; }
        public string ProjectName { get; }
        public DirectoryInfo ProjectRootPath { get; }
        public ICodeGenerator Backend { get; }
        public IReadOnlyList<string> InputFiles { get; }
        public ILocationResolver LocationResolver { get; }
        public ITranslationErrorHandler Handler { get; }

        public IReadOnlyList<string> ProjectDependencies { get; }
    }
}