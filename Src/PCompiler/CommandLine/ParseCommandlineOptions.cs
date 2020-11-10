using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace Plang.Compiler
{
    class ParseCommandlineOptions
    {
        private static readonly Lazy<bool> isFileSystemCaseInsensitive = new Lazy<bool>(() =>
        {
            string file = Path.GetTempPath() + Guid.NewGuid().ToString().ToLower() + "-lower";
            File.CreateText(file).Close();
            bool isCaseInsensitive = File.Exists(file.ToUpper());
            File.Delete(file);
            return isCaseInsensitive;
        });
        private static bool IsFileSystemCaseInsensitive => isFileSystemCaseInsensitive.Value;

        private readonly DefaultCompilerOutput CommandlineOutput;

        public ParseCommandlineOptions(DefaultCompilerOutput output)
        {
            CommandlineOutput = output;
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

                CommandlineOutput.WriteInfo($"==== Parsing project file: {projectFile}");

                CompilerOutput outputLanguage = CompilerOutput.C;
                List<FileInfo> inputFiles = new List<FileInfo>();
                bool generateSourceMaps = false;
                List<string> projectDependencies = new List<string>();

                // get all project dependencies and the input files
                var dependencies = GetAllProjectDependencies(projectFilePath);

                inputFiles.AddRange(dependencies.inputFiles);
                projectDependencies.AddRange(dependencies.projectDependencies);

                if (inputFiles.Count == 0)
                {
                    throw new CommandlineParsingError("At least one .p file must be provided as input files, no input files found after parsing the project file");
                }

                // get project name
                string projectName = GetProjectName(projectFilePath);

                // get output directory
                DirectoryInfo outputDirectory = GetOutputDirectory(projectFilePath);

                // get target language
                GetTargetLanguage(projectFilePath, ref outputLanguage, ref generateSourceMaps);

                job = new CompilationJob(output: new DefaultCompilerOutput(outputDirectory), outputLanguage: outputLanguage, inputFiles: inputFiles, projectName: projectName, generateSourceMaps: generateSourceMaps, projectDependencies);

                return true;
            }
            catch(CommandlineParsingError ex)
            {
                CommandlineOutput.WriteError($"<Error parsing project file>:\n {ex.Message}");
                return false;
            }
            catch(Exception other)
            {
                CommandlineOutput.WriteError($"<Internal Error>:\n {other.Message}\n <Please report to the P team (p-devs@amazon.com) or create a issue on GitHub, Thanks!>");
                return false;
            }
        }

        internal bool ParseCommandLineOptions(IEnumerable<string> args, out CompilationJob job)
        {
            string targetName = null;
            CompilerOutput outputLanguage = CompilerOutput.CSharp;
            DirectoryInfo outputDirectory = null;

            List<string> commandLineFileNames = new List<string>();
            List<FileInfo> inputFiles = new List<FileInfo>();

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
                                        throw new CommandlineParsingError("Missing generation argument, expecting generate:[C,CSharp]");
                                    case "c":
                                        outputLanguage = CompilerOutput.C;
                                        break;
                                    case "csharp":
                                        outputLanguage = CompilerOutput.CSharp;
                                        break;
                                    default:
                                        throw new CommandlineParsingError($"Unrecognized generate option '{colonArg}', expecting C or CSharp");
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

                            default:
                                CommandLineOptions.PrintUsage();
                                throw new CommandlineParsingError($"Illegal Command {arg.Substring(1)}");
                        }
                    }
                    else
                    {
                        if (IsLegalPFile(arg, out FileInfo fullPathName))
                        {
                            inputFiles.Add(fullPathName);
                        }
                        else
                        {
                            throw new CommandlineParsingError($"Illegal P file name {arg} or file {fullPathName.FullName} not found");
                        }
                    }
                }

                if (inputFiles.Count == 0)
                {
                    CommandlineOutput.WriteError("At least one .p file must be provided");
                    return false;
                }

                string projectName = targetName ?? Path.GetFileNameWithoutExtension(inputFiles[0].FullName);
                if (!IsLegalProjectName(projectName))
                {
                    CommandlineOutput.WriteError($"{projectName} is not a legal project name");
                    return false;
                }

                if (outputDirectory == null)
                {
                    outputDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
                }

                job = new CompilationJob(output: new DefaultCompilerOutput(outputDirectory), outputLanguage: outputLanguage, inputFiles: inputFiles, projectName: projectName);
                return true;
            }
            catch(CommandlineParsingError ex)
            {
                CommandlineOutput.WriteError($"<Error parsing commandline>:\n {ex.Message}");
                return false;
            }
            catch(Exception other)
            {
                CommandlineOutput.WriteError($"<Internal Error>:\n {other.Message}\n <Please report to the P team (p-devs@amazon.com) or create an issue on GitHub, Thanks!>");
                return false;
            }

            
        }

        /// <summary>
        /// Parse the P Project file and return all the input P files and project dependencies (includes transitive dependencies)
        /// </summary>
        /// <param name="projectFilePath">Path to the P Project file</param>
        /// <returns></returns>
        private (List<FileInfo> inputFiles, List<string> projectDependencies) GetAllProjectDependencies(FileInfo projectFilePath)
        {
            var projectDependencies = new List<string>();
            var inputFiles = new List<FileInfo>();
            XElement projectXML = XElement.Load(projectFilePath.FullName);
            projectDependencies.Add(GetProjectName(projectFilePath));
            // add all input files from the current project
            inputFiles.AddRange(ReadAllInputFiles(projectFilePath));

            // get recursive project dependencies
            foreach (XElement projectDepen in projectXML.Elements("IncludeProject"))
            {

                if (!IsLegalPProjFile(projectDepen.Value, out FileInfo fullProjectDepenPathName))
                {
                    throw new CommandlineParsingError($"Illegal P project file name {projectDepen.Value} or file {fullProjectDepenPathName?.FullName} not found");
                }

                CommandlineOutput.WriteInfo($"==== Parsing project file: {fullProjectDepenPathName.FullName}");

                var inputsAndDependencies = GetAllProjectDependencies(fullProjectDepenPathName);
                projectDependencies.AddRange(inputsAndDependencies.projectDependencies);
                inputFiles.AddRange(inputsAndDependencies.inputFiles);
                if (projectDependencies.Count != projectDependencies.Distinct().Count())
                {
                    throw new CommandlineParsingError($"Cyclic project dependencies: {projectDependencies}");
                }
            }

            return (inputFiles, projectDependencies);
        }

        private string GetProjectName(FileInfo projectFullPath)
        {
            string projectName = null;
            XElement projectXML = XElement.Load(projectFullPath.FullName);
            if (projectXML.Elements("ProjectName").Any())
            {
                projectName = projectXML.Element("ProjectName").Value;
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

        private DirectoryInfo GetOutputDirectory(FileInfo fullPathName)
        {
            XElement projectXML = XElement.Load(fullPathName.FullName);
            DirectoryInfo outputDirectory = projectXML.Elements("OutputDir").Any() ? Directory.CreateDirectory(projectXML.Element("OutputDir").Value) : new DirectoryInfo(Directory.GetCurrentDirectory());
            return outputDirectory;
        }

        private void GetTargetLanguage(FileInfo fullPathName, ref CompilerOutput outputLanguage, ref bool generateSourceMaps)
        {
            XElement projectXML = XElement.Load(fullPathName.FullName);
            if (projectXML.Elements("Target").Any())
            {
                switch (projectXML.Element("Target").Value.ToLowerInvariant())
                {
                    case "c":
                        outputLanguage = CompilerOutput.C;
                        // check for generate source maps attribute
                        try
                        {
                            if (projectXML.Element("Target").Attributes("sourcemaps").Any())
                            {
                                generateSourceMaps = bool.Parse(projectXML.Element("Target").Attribute("sourcemaps").Value);
                            }
                        }
                        catch (Exception)
                        {
                            throw new CommandlineParsingError($"Expected true or false, received {projectXML.Element("Target").Attribute("sourcemaps").Value}");
                        }
                        break;

                    case "csharp":
                        outputLanguage = CompilerOutput.CSharp;
                        break;

                    default:
                        throw new CommandlineParsingError($"Expected C or CSharp as target, received {projectXML.Element("Target").Value}");
                }
            }
        }

        private List<FileInfo> ReadAllInputFiles(FileInfo fullPathName)
        {
            List<FileInfo> inputFiles = new List<FileInfo>();
            XElement projectXML = XElement.Load(fullPathName.FullName);

            // get all files to be compiled
            foreach (XElement inputs in projectXML.Elements("InputFiles"))
            {
                foreach (XElement inputFileName in inputs.Elements("PFile"))
                {
                    var pFiles = new List<string>();
                    var inputFileNameFull = Path.Combine(Path.GetDirectoryName(fullPathName.FullName), inputFileName.Value);

                    if (Directory.Exists(inputFileNameFull))
                    {
                        foreach (var files in Directory.GetFiles(inputFileNameFull, "*.p"))
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
                            CommandlineOutput.WriteInfo($"....... includes p file: {pFilePathName.FullName}");
                            inputFiles.Add(pFilePathName);
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

            string path = Path.GetFullPath(fileName);
            if (IsFileSystemCaseInsensitive)
            {
                path = path.ToLowerInvariant();
            }

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

            string path = Path.GetFullPath(fileName);
            if (IsFileSystemCaseInsensitive)
            {
                path = path.ToLowerInvariant();
            }

            file = new FileInfo(path);

            return true;
        }
        #endregion

        /// <summary>
        /// Exception to capture errors when parsing commandline arguments
        /// </summary>
        private class CommandlineParsingError : Exception
        {
            public CommandlineParsingError()
            {
            }

            public CommandlineParsingError(string message) : base(message)
            {
            }

            public CommandlineParsingError(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected CommandlineParsingError(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }

    }
}
