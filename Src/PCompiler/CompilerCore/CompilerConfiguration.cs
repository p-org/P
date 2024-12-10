using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plang.Compiler.Backend;
using Plang.Compiler.TypeChecker;

namespace Plang.Compiler
{
    /// <summary>
    /// Represents the configuration settings for the P language compiler.
    /// This class contains all parameters and options that control the compilation process,
    /// including input/output paths, code generation options, and project settings.
    /// </summary>
    public class CompilerConfiguration : ICompilerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerConfiguration"/> class with default settings.
        /// </summary>
        /// <remarks>
        /// Default settings include:
        /// - No output directory
        /// - Empty input file lists
        /// - "generatedOutput" as project name
        /// - Current directory as project root
        /// - C# as the default output language
        /// - Default location resolver and error handler
        /// - Debug mode disabled
        /// </remarks>
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
            Debug = false;
            Timeout = 600;
            HandlesAll = true;
            CheckOnly = null;
            Parallelism = Math.Max(Environment.ProcessorCount / 2, 1);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerConfiguration"/> class with specific settings.
        /// </summary>
        /// <param name="output">The compiler output handler.</param>
        /// <param name="outputDir">The directory where compiler output will be generated.</param>
        /// <param name="outputLanguages">The list of target programming languages for code generation.</param>
        /// <param name="inputFiles">The list of input files to compile (P files and foreign files).</param>
        /// <param name="projectName">The name of the project being compiled. If null, derives from first input file.</param>
        /// <param name="projectRoot">The root directory of the project. If null, uses the current directory.</param>
        /// <param name="projectDependencies">The list of project dependencies. If null, initializes as empty list.</param>
        /// <param name="pObservePackageName">The name of the PObserve package. If null, defaults to "{ProjectName}.pobserve".</param>
        /// <param name="debug">Flag indicating whether to include debug information in output.</param>
        /// <exception cref="ArgumentException">Thrown when no input files are provided.</exception>
        public CompilerConfiguration(ICompilerOutput output, DirectoryInfo outputDir, IList<CompilerOutput> outputLanguages, IList<string> inputFiles,
            string projectName, DirectoryInfo projectRoot = null, IList<string> projectDependencies = null, string pObservePackageName = null, bool debug = false)
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
            Debug = debug;
        }

        /// <summary>
        /// Gets or sets the compiler output handler.
        /// </summary>
        public ICompilerOutput Output { get; set; }

        /// <summary>
        /// Gets or sets the directory where compiler output will be generated.
        /// </summary>
        public DirectoryInfo OutputDirectory { get; set; }

        /// <summary>
        /// Gets or sets the list of target programming languages for code generation.
        /// </summary>
        public IList<CompilerOutput> OutputLanguages { get; set; }

        /// <summary>
        /// Gets or sets the name of the project being compiled.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Gets or sets the name of the PObserve package.
        /// </summary>
        public string PObservePackageName { get; set; }

        /// <summary>
        /// Gets or sets the root directory of the project.
        /// </summary>
        public DirectoryInfo ProjectRootPath { get; set; }

        /// <summary>
        /// Gets or sets the backend code generator.
        /// </summary>
        public ICodeGenerator Backend { get; set; }

        /// <summary>
        /// Gets or sets the list of P language input files to compile.
        /// </summary>
        public IList<string> InputPFiles { get; set; }

        /// <summary>
        /// Gets or sets the list of foreign (non-P) input files to include.
        /// </summary>
        public IList<string> InputForeignFiles { get; set; }

        /// <summary>
        /// Gets or sets the location resolver for source code positions.
        /// </summary>
        public ILocationResolver LocationResolver { get; set;  }

        /// <summary>
        /// Gets or sets the handler for translation errors.
        /// </summary>
        public ITranslationErrorHandler Handler { get; set; }

        /// <summary>
        /// Gets or sets the list of project dependencies.
        /// </summary>
        public IList<string> ProjectDependencies { get; set;  }

        /// <summary>
        /// Gets or sets a value indicating whether debug information should be included in output.
        /// </summary>
        public bool Debug { get; set; }
        public int Timeout { get; set; }
        public bool HandlesAll { get; set; }
        public string CheckOnly { get; set; }
        public int Parallelism { get; set; }

        /// <summary>
        /// Copies all properties from another CompilerConfiguration instance to this instance.
        /// </summary>
        /// <param name="parsedConfig">The source configuration to copy from.</param>
        /// <remarks>
        /// This method performs a shallow copy of all properties from the specified configuration
        /// to the current instance, effectively replacing the current configuration.
        /// </remarks>
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
            Debug = parsedConfig.Debug;
        }
    }
}
