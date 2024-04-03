using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using PChecker;
using PChecker.IO.Debugging;
using Plang.Compiler;

namespace Plang.Parser
{
    internal class ParsePProjectFile
    {

        /// <summary>
        /// Parse the P Project file for compiler
        /// </summary>
        /// <param name="projectFile">Path to the P project file</param>
        /// <param name="job">out parameter of P compilation job, after parsing the project file</param>
        /// <returns></returns>
        public void ParseProjectFileForCompiler(string projectFile, out CompilerConfiguration job)
        {
            job = null;
            try
            {
                if (!CheckFileValidity.IsLegalPProjFile(projectFile, out var projectFilePath))
                {
                    throw new CommandlineParsingError($"Illegal P project file name {projectFile} or file {projectFilePath?.FullName} not found");
                }
                CommandLineOutput.WriteInfo($"----------------------------------------");
                CommandLineOutput.WriteInfo($"==== Loading project file: {projectFile}");

                var inputFiles = new HashSet<string>();
                var projectDependencies = new HashSet<string>();

                // get all project dependencies and the input files
                var (fileInfos, list) = GetAllProjectDependencies(projectFilePath, inputFiles, projectDependencies);

                inputFiles.UnionWith(fileInfos);
                projectDependencies.UnionWith(list);

                if (inputFiles.Count == 0)
                {
                    Error.ReportAndExit("At least one .p file must be provided as input files, no input files found after parsing the project file");
                }

                // get project name
                var projectName = GetProjectName(projectFilePath);

                // get output directory
                var outputDirectory = GetOutputDirectory(projectFilePath);

                // get targets
                var outputLanguages = GetTargetLanguages(projectFilePath);

                // get pobserve package name
                var pObservePackageName = GetPObservePackage(projectFilePath);

                job = new CompilerConfiguration(output: new DefaultCompilerOutput(outputDirectory), outputDir: outputDirectory,
                    outputLanguages: outputLanguages, inputFiles: inputFiles.ToList(), projectName: projectName,
                    projectRoot: projectFilePath.Directory, projectDependencies: projectDependencies.ToList(),
                    pObservePackageName: pObservePackageName);

                CommandLineOutput.WriteInfo($"----------------------------------------");
            }
            catch (CommandlineParsingError ex)
            {
                Error.ReportAndExit($"<Error parsing project file>:\n {ex.Message}");
            }
            catch (Exception other)
            {
                Error.ReportAndExit($"<Internal Error>:\n {other.Message}\n <Please report to the P team or create a issue on GitHub, Thanks!>");
            }
        }

        /// <summary>
        /// Parse the P Project file for P checker
        /// </summary>
        /// <param name="projectFile">Path to the P project file</param>
        /// <param name="job">out parameter of P checker job, after parsing the project file</param>
        /// <returns></returns>
        public void ParseProjectFileForChecker(string projectFile, CheckerConfiguration job)
        {
            try
            {
                if (!CheckFileValidity.IsLegalPProjFile(projectFile, out var projectFilePath))
                {
                    throw new CommandlineParsingError($"Illegal P project file name {projectFile} or file {projectFilePath?.FullName} not found");
                }

                // set compiler output directory as p compiled path
                job.PCompiledPath = GetOutputDirectoryName(projectFilePath);
            }
            catch (CommandlineParsingError ex)
            {
                Error.ReportAndExit($"<Error parsing project file>:\n {ex.Message}");
            }
            catch (Exception other)
            {
                Error.ReportAndExit($"<Internal Error>:\n {other.Message}\n <Please report to the P team or create a issue on GitHub, Thanks!>");
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
            var projectXml = XElement.Load(projectFilePath.FullName);
            // add all input files from the current project
            inputFiles.UnionWith(ReadAllInputFiles(projectFilePath));

            // get recursive project dependencies
            foreach (var projectDepen in projectXml.Elements("IncludeProject"))
            {
                if (!CheckFileValidity.IsLegalPProjFile(projectDepen.Value, out var fullProjectDepenPathName))
                {
                    throw new CommandlineParsingError($"Illegal P project file name {projectDepen.Value} or file {fullProjectDepenPathName?.FullName} not found");
                }

                CommandLineOutput.WriteInfo($"==== Loading project file: {fullProjectDepenPathName.FullName}");

                if (projectDependencies.Contains(fullProjectDepenPathName.DirectoryName)) continue;
                // add path of imported project as project dependency
                projectDependencies.Add(fullProjectDepenPathName.DirectoryName);
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
            var projectXml = XElement.Load(projectFullPath.FullName);
            if (projectXml.Elements("ProjectName").Any())
            {
                projectName = projectXml.Element("ProjectName")?.Value;
                if (!CheckFileValidity.IsLegalProjectName(projectName))
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
        /// Parse the PObserve package name from the pproj file
        /// </summary>
        /// <param name="projectFullPath">Path to the pproj file</param>
        /// <returns>pobserve package name</returns>
        private string GetPObservePackage(FileInfo projectFullPath)
        {
            string pObservePackageName = null;
            var projectXml = XElement.Load(projectFullPath.FullName);
            if (projectXml.Elements("pobserve-package").Any())
            {
                pObservePackageName = projectXml.Element("pobserve-package")?.Value;
            }

            return pObservePackageName;
        }

        /// <summary>
        /// Parse the output directory information from the pproj file
        /// </summary>
        /// <param name="fullPathName"></param>
        /// <returns>If present returns the passed directory path, else the current directory</returns>
        private DirectoryInfo GetOutputDirectory(FileInfo fullPathName)
        {
            var projectXml = XElement.Load(fullPathName.FullName);
            if (projectXml.Elements("OutputDir").Any())
                return Directory.CreateDirectory(projectXml.Element("OutputDir")?.Value);
            else
                return new DirectoryInfo(Directory.GetCurrentDirectory());
        }

        /// <summary>
        /// Parse the output directory information from the pproj file
        /// </summary>
        /// <param name="fullPathName"></param>
        /// <returns>If present returns the output directory name, else the current directory name</returns>
        private string GetOutputDirectoryName(FileInfo fullPathName)
        {
            var projectXml = XElement.Load(fullPathName.FullName);
            if (projectXml.Elements("OutputDir").Any())
                return projectXml.Element("OutputDir")?.Value;
            else
                return Directory.GetCurrentDirectory();
        }

        private IList<CompilerOutput> GetTargetLanguages(FileInfo fullPathName)
        {
            var outputLanguages = new List<CompilerOutput>();
            var projectXml = XElement.Load(fullPathName.FullName);
            if (!projectXml.Elements("Target").Any())
            {
                outputLanguages.Add(CompilerOutput.CSharp);
            }
            else
            {
                string[] values = projectXml.Element("Target")?.Value.Split(new[] { ',', ' ' },
                    StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < values!.Length; i++)
                {
                    switch (values[i].ToLowerInvariant())
                    {
                        case "bugfinding":
                        case "csharp":
                            outputLanguages.Add(CompilerOutput.CSharp);
                            break;
                        case "verification":
                        case "coverage":
                        case "symbolic":
                        case "psym":
                        case "pcover":
                            outputLanguages.Add(CompilerOutput.Symbolic);
                            break;
                        case "pobserve":
                        case "java":
                            outputLanguages.Add(CompilerOutput.Java);
                            break;
                        case "stately":
                            outputLanguages.Add(CompilerOutput.Stately);
                            break;
                        default:
                            throw new CommandlineParsingError(
                                $"Expected CSharp, Java, Stately, or Symbolic as target, received {projectXml.Element("Target")?.Value}");
                    }
                }
            }

            return outputLanguages;
        }

        /// <summary>
        /// Read all the input P files included in the pproj
        /// </summary>
        /// <param name="fullPathName">Path to the pproj file</param>
        /// <returns>List of the all the P files included in the project</returns>
        private HashSet<string> ReadAllInputFiles(FileInfo fullPathName)
        {
            var inputFiles = new HashSet<string>();
            var projectXml = XElement.Load(fullPathName.FullName);

            // get all files to be compiled
            foreach (var inputs in projectXml.Elements("InputFiles"))
            {
                foreach (var inputFileName in inputs.Elements("PFile"))
                {
                    var pFiles = new List<string>();
                    var inputFileNameFull = Path.Combine(Path.GetDirectoryName(fullPathName.FullName) ?? throw new InvalidOperationException(), inputFileName.Value);

                    if (Directory.Exists(inputFileNameFull))
                    {
                        var enumerate = new EnumerationOptions();
                        enumerate.RecurseSubdirectories = true;
                        var getFiles =
                            from file in Directory.GetFiles(inputFileNameFull, "*.*", enumerate)
                            where ( CheckFileValidity.IsPFile(file) || CheckFileValidity.IsForeignFile(file))
                            let info = new FileInfo(file)
                            where (((info.Attributes & FileAttributes.Hidden) ==0)& ((info.Attributes & FileAttributes.System)==0))
                            select file;
                        foreach (var files in getFiles)
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
                        if (CheckFileValidity.IsLegalPFile(pFile, out var pFilePathName))
                        {
                            CommandLineOutput.WriteInfo($"....... includes p file: {pFilePathName.FullName}");
                            inputFiles.Add(pFilePathName.FullName);
                        }
                        else if (CheckFileValidity.IsLegalForeignFile(pFile, out var foreignFilePathName))
                        {
                            CommandLineOutput.WriteInfo($"....... includes foreign file: {foreignFilePathName.FullName}");
                            inputFiles.Add(foreignFilePathName.FullName);
                        }
                    }
                }
            }

            return inputFiles;
        }
    }
}