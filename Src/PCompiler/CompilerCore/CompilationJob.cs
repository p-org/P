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
        public CompilationJob(ICompilerOutput output, CompilerOutput outputLanguage, IReadOnlyList<FileInfo> inputFiles,
            string projectName = null, bool generateSourceMaps = false)
        {
            if (!inputFiles.Any())
            {
                throw new ArgumentException("Must supply at least one input file", nameof(inputFiles));
            }

            Output = output;
            InputFiles = inputFiles;
            ProjectName = projectName ?? Path.GetFileNameWithoutExtension(inputFiles[0].FullName);
            LocationResolver = new DefaultLocationResolver();
            Handler = new DefaultTranslationErrorHandler(LocationResolver);
            Backend = TargetLanguage.GetCodeGenerator(outputLanguage);
            GenerateSourceMaps = generateSourceMaps;
        }

        public bool GenerateSourceMaps { get; }
        public ICompilerOutput Output { get; }

        public string ProjectName { get; }
        public ICodeGenerator Backend { get; }
        public IReadOnlyList<FileInfo> InputFiles { get; }
        public ILocationResolver LocationResolver { get; }
        public ITranslationErrorHandler Handler { get; }
    }
}