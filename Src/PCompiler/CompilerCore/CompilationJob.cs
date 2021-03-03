using Plang.Compiler.Backend;
using Plang.Compiler.TypeChecker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Plang.Compiler
{
    public class CompilationJob : ICompilationJob
    {
        public CompilationJob(ICompilerOutput output, DirectoryInfo outputDir, CompilerOutput outputLanguage, IReadOnlyList<FileInfo> inputFiles,
            string projectName, DirectoryInfo projectRoot = null, bool generateSourceMaps = false, IReadOnlyList<string> projectDependencies = null,
            DirectoryInfo aspectjOutputDir = null)
        {
            if (!inputFiles.Any())
            {
                throw new ArgumentException("Must supply at least one input file", nameof(inputFiles));
            }

            Output = output;
            OutputDirectory = outputDir;
            AspectjOutputDirectory = aspectjOutputDir;
            InputFiles = inputFiles;
            ProjectName = projectName ?? Path.GetFileNameWithoutExtension(inputFiles[0].FullName);
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
        public DirectoryInfo AspectjOutputDirectory { get; }
        public CompilerOutput OutputLanguage { get; }
        public string ProjectName { get; }
        public DirectoryInfo ProjectRootPath { get; }
        public ICodeGenerator Backend { get; }
        public IReadOnlyList<FileInfo> InputFiles { get; }
        public ILocationResolver LocationResolver { get; }
        public ITranslationErrorHandler Handler { get; }

        public IReadOnlyList<string> ProjectDependencies { get; }
    }
}