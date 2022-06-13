using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Plang.Compiler
{
    internal class ParseCommandlineOptions
    {
        /// <summary>
        /// Check if the underlying file system is case insensitive
        /// </summary>

        private readonly DefaultCompilerOutput commandlineOutput;

        public ParseCommandlineOptions(DefaultCompilerOutput output)
        {
            commandlineOutput = output;
        }

        /// <summary>
        /// Parse the P Project file
        /// </summary>
        /// <param name="projectFile">Path to the P project file</param>
        /// <param name="job">out parameter of P compilation job, after parsing the project file</param>
        /// <returns></returns>
        public bool ParseProjectFile(string projectFile, out CompilationJob job)
        {
            job = null;
            try
            {
                if (!IsLegalPProjFile(projectFile, out FileInfo projectFilePath))
                {
                    throw new CommandlineParsingError($"Illegal P project file name {projectFile} or file {projectFilePath?.FullName} not found");
                }
                commandlineOutput.WriteInfo($"----------------------------------------");
                commandlineOutput.WriteInfo($"==== Loading project file: {projectFile}");

                var outputLanguage = CompilerOutput.CSharp;
                HashSet<string> inputFiles = new HashSet<string>();
                bool generateSourceMaps = false;
                HashSet<string> projectDependencies = new HashSet<string>();

                // get all project dependencies and the input files
                var (fileInfos, list) = GetAllProjectDependencies(projectFilePath, inputFiles, projectDependencies);

                inputFiles.UnionWith(fileInfos);
                projectDependencies.UnionWith(list);

                if (inputFiles.Count == 0)
                {
                    throw new CommandlineParsingError("At least one .p file must be provided as input files, no input files found after parsing the project file");
                }

                // get project name
                var projectName = GetProjectName(projectFilePath);

                // get output directory
                var outputDirectory = GetOutputDirectory(projectFilePath);
                var aspectjOutputDirectory = GetAspectjOutputDirectory(projectFilePath, outputDirectory);

                // get target language
                GetTargetLanguage(projectFilePath, ref outputLanguage, ref generateSourceMaps);

                job = new CompilationJob(output: new DefaultCompilerOutput(outputDirectory, aspectjOutputDirectory), outputDirectory,
                    outputLanguage: outputLanguage, inputFiles: inputFiles.ToList(), projectName: projectName, projectFilePath.Directory,
                    generateSourceMaps: generateSourceMaps, projectDependencies: projectDependencies.ToList(), aspectjOutputDir: aspectjOutputDirectory);

                commandlineOutput.WriteInfo($"----------------------------------------");
                return true;
            }
            catch (CommandlineParsingError ex)
            {
                commandlineOutput.WriteError($"<Error parsing project file>:\n {ex.Message}");
                return false;
            }
            catch (Exception other)
            {
                commandlineOutput.WriteError($"<Internal Error>:\n {other.Message}\n <Please report to the P team (p-devs@amazon.com) or create a issue on GitHub, Thanks!>");
                commandlineOutput.WriteError($"{other.StackTrace}\n");
                return false;
            }
        }

        /// <summary>
        /// Parse the commandline arguments to construct the compilation job
        /// </summary>
        /// <param name="args">Commandline arguments</param>
        /// <param name="job">Generated Compilation job</param>
        /// <returns></returns>
        internal bool ParseCommandLineOptions(IEnumerable<string> args, out CompilationJob job)
        {
            string targetName = null;
            CompilerOutput outputLanguage = CompilerOutput.CSharp;
            DirectoryInfo outputDirectory = null;
            DirectoryInfo aspectjOutputDirectory = null;
            HashSet<string> inputFiles = new HashSet<string>();
            commandlineOutput.WriteInfo($"----------------------------------------");
            job = null;
            try
            {
                foreach (string x in args)
                {
                    string arg = x;
                    string colonArg = null;
                    if (arg[0] == '-')
                    {
                        int colonIndex = arg.IndexOf(':');
                        if (colonIndex >= 0)
                        {
                            arg = x.Substring(0, colonIndex);
                            colonArg = x.Substring(colonIndex + 1);
                        }

                        switch (arg.Substring(1).ToLowerInvariant())
                        {
                            case "t":
                            case "target":
                                if (colonArg == null)
                                {
                                    throw new CommandlineParsingError("Missing target project name (-t:<project name>)");
                                }
                                else if (targetName == null)
                                {
                                    targetName = colonArg;
                                }
                                else
                                {
                                    throw new CommandlineParsingError("Only one target must be specified with (-t)");
                                }
                                break;

                            case "g":
                            case "generate":
                                switch (colonArg?.ToLowerInvariant())
                                {
                                    case null:
                                        throw new CommandlineParsingError("Missing generation argument, expecting generate:[C,CSharp,Java,RVM,Symbolic]");
                                    case "c":
                                        outputLanguage = CompilerOutput.C;
                                        break;
                                    case "csharp":
                                        outputLanguage = CompilerOutput.CSharp;
                                        break;
                                    case "java":
                                        outputLanguage = CompilerOutput.Java;
                                        break;
                                    case "rvm":
                                        outputLanguage = CompilerOutput.Rvm;
                                        break;
                                    case "symbolic":
                                        outputLanguage = CompilerOutput.Symbolic;
                                        break;
                                    default:
                                        throw new CommandlineParsingError($"Unrecognized generate option '{colonArg}', expecting one of C, CSharp, Java, RVM, Symbolic.");
                                }
                                break;

                            case "o":
                            case "outputdir":
                                if (colonArg == null)
                                {
                                    throw new CommandlineParsingError("Must supply path for output directory (-o:<output directory>)");
                                }
                                outputDirectory = Directory.CreateDirectory(colonArg);
                                break;

                            case "a":
                            case "aspectoutputdir":
                                if (colonArg == null)
                                {
                                    throw new CommandlineParsingError("Must supply path for aspectj output directory (-a:<aspectj output directory>)");
                                }
                                aspectjOutputDirectory = Directory.CreateDirectory(colonArg);
                                break;

                            default:
                                CommandLineOptions.PrintUsage();
                                throw new CommandlineParsingError($"Illegal Command {arg.Substring(1)}");
                        }
                    }
                    else
                    {
                        if (IsLegalPFile(arg, out FileInfo fullPathName))
                        {
                            inputFiles.Add(fullPathName.FullName);
                            commandlineOutput.WriteInfo($"....... includes p file: {fullPathName.FullName}");
                        }
                        else
                        {
                            throw new CommandlineParsingError($"Illegal P file name {arg} (file name cannot have special characters) or file not found.");
                        }
                    }
                }

                if (inputFiles.Count == 0)
                {
                    commandlineOutput.WriteError("At least one .p file must be provided");
                    return false;
                }

                string projectName = targetName ?? Path.GetFileNameWithoutExtension(inputFiles.FirstOrDefault());
                if (!IsLegalProjectName(projectName))
                {
                    commandlineOutput.WriteError($"{projectName} is not a legal project name");
                    return false;
                }

                if (outputDirectory == null)
                {
                    outputDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
                }

                if (aspectjOutputDirectory == null)
                {
                    aspectjOutputDirectory = outputDirectory;
                }

                job = new CompilationJob(output: new DefaultCompilerOutput(outputDirectory, aspectjOutputDirectory), outputDirectory,
                    outputLanguage: outputLanguage, inputFiles: inputFiles.ToList(), projectName: projectName, projectRoot: outputDirectory,
                    aspectjOutputDir: aspectjOutputDirectory);
                commandlineOutput.WriteInfo($"----------------------------------------");
                return true;
            }
            catch (CommandlineParsingError ex)
            {
                commandlineOutput.WriteError($"<Error parsing commandline>:\n {ex.Message}");
                return false;
            }
            catch (Exception other)
            {
                commandlineOutput.WriteError($"<Internal Error>:\n {other.Message}\n <Please report to the P team (p-devs@amazon.com) or create an issue on GitHub, Thanks!>");
                commandlineOutput.WriteError($"{other.StackTrace}\n");
                return false;
            }
        }

        /// <summary>
        /// Parse the P Project file and return all the input P files and project dependencies (includes transitive dependencies)
        /// </summary>
        /// <param name="projectFilePath">Path to the P Project file</param>
        /// <param name="preInputFiles"></param>
        /// <param name="preProjectDependencies"></param>
        /// <returns></returns>
        private (HashSet<string> inputFiles, HashSet<string> projectDependencies) GetAllProjectDependencies(FileInfo projectFilePath, HashSet<string> preInputFiles, HashSet<string> preProjectDependencies)
        {
            var projectDependencies = new HashSet<string>(preProjectDependencies);
            var inputFiles = new HashSet<string>(preInputFiles);
            XElement projectXml = XElement.Load(projectFilePath.FullName);
            projectDependencies.Add(GetProjectName(projectFilePath));
            // add all input files from the current project
            inputFiles.UnionWith(ReadAllInputFiles(projectFilePath));

            // get recursive project dependencies
            foreach (XElement projectDepen in projectXml.Elements("IncludeProject"))
            {
                if (!IsLegalPProjFile(projectDepen.Value, out FileInfo fullProjectDepenPathName))
                {
                    throw new CommandlineParsingError($"Illegal P project file name {projectDepen.Value} or file {fullProjectDepenPathName?.FullName} not found");
                }

                commandlineOutput.WriteInfo($"==== Loading project file: {fullProjectDepenPathName.FullName}");

                if (projectDependencies.Contains(GetProjectName(fullProjectDepenPathName))) continue;
                var inputsAndDependencies = GetAllProjectDependencies(fullProjectDepenPathName, inputFiles, projectDependencies);
                projectDependencies.UnionWith(inputsAndDependencies.projectDependencies);
                inputFiles.UnionWith(inputsAndDependencies.inputFiles);
            }

            return (inputFiles, projectDependencies);
        }

        /// <summary>
        /// Parse the Project Name from the pproj file
        /// </summary>
        /// <param name="projectFullPath">Path to the pproj file</param>
        /// <returns>project name</returns>
        private string GetProjectName(FileInfo projectFullPath)
        {
            string projectName;
            XElement projectXml = XElement.Load(projectFullPath.FullName);
            if (projectXml.Elements("ProjectName").Any())
            {
                projectName = projectXml.Element("ProjectName")?.Value;
                if (!IsLegalProjectName(projectName))
                {
                    throw new CommandlineParsingError($"{projectName} is not a legal project name");
                }
            }
            else
            {
                throw new CommandlineParsingError($"Missing project name in {projectFullPath.FullName}");
            }

            return projectName;
        }

        /// <summary>
        /// Parse the output directory information from the pproj file
        /// </summary>
        /// <param name="fullPathName"></param>
        /// <returns>If present returns the passed directory path, else the current directory</returns>
        private DirectoryInfo GetOutputDirectory(FileInfo fullPathName)
        {
            XElement projectXml = XElement.Load(fullPathName.FullName);
            if (projectXml.Elements("OutputDir").Any())
                return Directory.CreateDirectory(projectXml.Element("OutputDir")?.Value);
            else
                return new DirectoryInfo(Directory.GetCurrentDirectory());
        }

        private DirectoryInfo GetAspectjOutputDirectory(FileInfo fullPathName, DirectoryInfo outputDir)
        {
            XElement projectXml = XElement.Load(fullPathName.FullName);
            if (projectXml.Elements("AspectjOutputDir").Any())
                return Directory.CreateDirectory(projectXml.Element("AspectjOutputDir").Value);
            else
                return outputDir;
        }

        private void GetTargetLanguage(FileInfo fullPathName, ref CompilerOutput outputLanguage, ref bool generateSourceMaps)
        {
            XElement projectXml = XElement.Load(fullPathName.FullName);
            if (!projectXml.Elements("Target").Any()) return;
            switch (projectXml.Element("Target")?.Value.ToLowerInvariant())
            {
                case "c":
                    outputLanguage = CompilerOutput.C;
                    // check for generate source maps attribute
                    try
                    {
                        if (projectXml.Element("Target").Attributes("sourcemaps").Any())
                        {
                            generateSourceMaps = bool.Parse(projectXml.Element("Target")?.Attribute("sourcemaps")?.Value ?? string.Empty);
                        }
                    }
                    catch (Exception)
                    {
                        throw new CommandlineParsingError($"Expected true or false, received {projectXml.Element("Target")?.Attribute("sourcemaps")?.Value}");
                    }
                    break;

                case "csharp":
                    outputLanguage = CompilerOutput.CSharp;
                    break;

                case "java":
                    outputLanguage = CompilerOutput.Java;
                    break;
                
                case "rvm":
                    outputLanguage = CompilerOutput.Rvm;
                    break;
                
                case "symbolic":
                    outputLanguage = CompilerOutput.Symbolic;
                    break;

                default:
                    throw new CommandlineParsingError($"Expected C, CSharp, Java, RVM, or Symbolic as target, received {projectXml.Element("Target")?.Value}");
            }
        }

        /// <summary>
        /// Read all the input P files included in the pproj 
        /// </summary>
        /// <param name="fullPathName">Path to the pproj file</param>
        /// <returns>List of the all the P files included in the project</returns>
        private HashSet<string> ReadAllInputFiles(FileInfo fullPathName)
        {
            HashSet<string> inputFiles = new HashSet<string>();
            XElement projectXml = XElement.Load(fullPathName.FullName);

            // get all files to be compiled
            foreach (XElement inputs in projectXml.Elements("InputFiles"))
            {
                foreach (XElement inputFileName in inputs.Elements("PFile"))
                {
                    var pFiles = new List<string>();
                    var inputFileNameFull = Path.Combine(Path.GetDirectoryName(fullPathName.FullName) ?? throw new InvalidOperationException(), inputFileName.Value);

                    if (Directory.Exists(inputFileNameFull))
                    {
                        var enumerate = new EnumerationOptions();
                        enumerate.RecurseSubdirectories = true;
                        foreach (var files in Directory.GetFiles(inputFileNameFull, "*.p", enumerate))
                        {
                            pFiles.Add(files);
                        }
                    }
                    else
                    {
                        pFiles.Add(inputFileNameFull);
                    }

                    foreach (var pFile in pFiles)
                    {
                        if (IsLegalPFile(pFile, out FileInfo pFilePathName))
                        {
                            commandlineOutput.WriteInfo($"....... includes p file: {pFilePathName.FullName}");
                            inputFiles.Add(pFilePathName.FullName);
                        }
                        else
                        {
                            throw new CommandlineParsingError($"Illegal P file name {pFile} or file {pFilePathName?.FullName} not found");
                        }
                    }
                }
            }

            return inputFiles;
        }

        #region Functions to check if the commandline inputs are legal

        private bool IsLegalProjectName(string projectName)
        {
            return Regex.IsMatch(projectName, "^[A-Za-z_][A-Za-z_0-9]*$");
        }

        private bool IsLegalPFile(string fileName, out FileInfo file)
        {
            file = null;
            if (fileName.Length <= 2 || !fileName.EndsWith(".p") || !File.Exists(Path.GetFullPath(fileName)))
            {
                return false;
            }

            var path = Path.GetFullPath(fileName);
            file = new FileInfo(path);

            return true;
        }

        private bool IsLegalPProjFile(string fileName, out FileInfo file)
        {
            file = null;
            if (fileName.Length <= 2 || !fileName.EndsWith(".pproj") || !File.Exists(Path.GetFullPath(fileName)))
            {
                return false;
            }

            var path = Path.GetFullPath(fileName);
            file = new FileInfo(path);

            return true;
        }

        #endregion Functions to check if the commandline inputs are legal

        /// <summary>
        /// Exception to capture errors when parsing commandline arguments
        /// </summary>
        private class CommandlineParsingError : Exception
        {
            public CommandlineParsingError(string message) : base(message)
            {
            }
        }
    }
}
