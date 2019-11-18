using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static Plang.Compiler.CommandLineParseResult;

namespace Plang.Compiler
{
    public enum CommandLineParseResult
    {
        Success,
        Failure,
        HelpRequested
    }

    public static class CommandLineOptions
    {
        private static readonly Lazy<bool> isFileSystemCaseInsensitive = new Lazy<bool>(() =>
        {
            string file = Path.GetTempPath() + Guid.NewGuid().ToString().ToLower() + "-lower";
            File.CreateText(file).Close();
            bool isCaseInsensitive = File.Exists(file.ToUpper());
            File.Delete(file);
            return isCaseInsensitive;
        });

        private static readonly DefaultCompilerOutput CommandlineOutput =
            new DefaultCompilerOutput(new DirectoryInfo(Directory.GetCurrentDirectory()));

        private static bool IsFileSystemCaseInsensitive => isFileSystemCaseInsensitive.Value;

        public static CommandLineParseResult ParseArguments(IEnumerable<string> args, out CompilationJob job)
        {
            job = null;

            CompilerOutput outputLanguage = CompilerOutput.C;
            DirectoryInfo outputDirectory = null;

            List<string> commandLineFileNames = new List<string>();
            List<FileInfo> inputFiles = new List<FileInfo>();
            string targetName = null;
            bool generateSourceMaps = false;

            // enforce the argument prority
            // proj takes priority over everything else and no other arguments should be passed
            if (args.Where(a => a.ToLowerInvariant().Contains("-proj:")).Any() && args.Count() > 1)
            {
                CommandlineOutput.WriteMessage("-proj cannot be combined with other commandline options", SeverityKind.Error);
                return Failure;
            }

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
                                CommandlineOutput.WriteMessage("Missing target name", SeverityKind.Error);
                            }
                            else if (targetName == null)
                            {
                                targetName = colonArg;
                            }
                            else
                            {
                                CommandlineOutput.WriteMessage("Only one target must be specified", SeverityKind.Error);
                            }

                            break;

                        case "g":
                        case "generate":
                            switch (colonArg?.ToLowerInvariant())
                            {
                                case null:
                                    CommandlineOutput.WriteMessage(
                                        "Missing generation argument, expecting generate:[C,Coyote]", SeverityKind.Error);
                                    return Failure;

                                case "c":
                                    outputLanguage = CompilerOutput.C;
                                    break;

                                case "coyote":
                                    outputLanguage = CompilerOutput.Coyote;
                                    break;

                                default:
                                    CommandlineOutput.WriteMessage(
                                        $"Unrecognized generate option '{colonArg}', expecting C or Coyote",
                                        SeverityKind.Error);
                                    return Failure;
                            }

                            break;

                        case "o":
                        case "outputdir":
                            if (colonArg == null)
                            {
                                CommandlineOutput.WriteMessage("Must supply path for output directory",
                                    SeverityKind.Error);
                                return Failure;
                            }

                            outputDirectory = Directory.CreateDirectory(colonArg);
                            break;

                        case "proj":
                            if (colonArg == null)
                            {
                                CommandlineOutput.WriteMessage("Must supply project file for compilation",
                                    SeverityKind.Error);
                                return Failure;
                            }
                            else
                            {
                                // Parse the project file and generate the compilation job, ignore all other arguments passed
                                if (ParseProjectFile(colonArg, out job))
                                {
                                    return Success;
                                }
                                else
                                {
                                    return Failure;
                                }
                            }
                        case "s":
                        case "sourcemaps":
                            switch (colonArg?.ToLowerInvariant())
                            {
                                case null:
                                case "true":
                                    generateSourceMaps = true;
                                    break;

                                case "false":
                                    generateSourceMaps = false;
                                    break;

                                default:
                                    CommandlineOutput.WriteMessage(
                                        "sourcemaps argument must be either 'true' or 'false'", SeverityKind.Error);
                                    return Failure;
                            }

                            break;

                        case "h":
                        case "help":
                        case "-help":
                            return HelpRequested;

                        default:
                            commandLineFileNames.Add(arg);
                            CommandlineOutput.WriteMessage($"Unknown Command {arg.Substring(1)}", SeverityKind.Error);
                            return Failure;
                    }
                }
                else
                {
                    commandLineFileNames.Add(arg);
                }
            }

            // We are here so no project file supplied lets create a compilation job with other arguments

            // Each command line file name must be a legal P file name
            foreach (string inputFileName in commandLineFileNames)
            {
                if (IsLegalPFile(inputFileName, out FileInfo fullPathName))
                {
                    inputFiles.Add(fullPathName);
                }
                else
                {
                    CommandlineOutput.WriteMessage(
                        $"Illegal P file name {inputFileName} or file {fullPathName.FullName} not found", SeverityKind.Error);
                }
            }

            if (inputFiles.Count == 0)
            {
                CommandlineOutput.WriteMessage("At least one .p file must be provided", SeverityKind.Error);
                return Failure;
            }

            string projectName = targetName ?? Path.GetFileNameWithoutExtension(inputFiles[0].FullName);
            if (!IsLegalUnitName(projectName))
            {
                CommandlineOutput.WriteMessage($"{projectName} is not a legal project name", SeverityKind.Error);
                return Failure;
            }

            if (outputDirectory == null)
            {
                outputDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            }

            job = new CompilationJob(output: new DefaultCompilerOutput(outputDirectory), outputLanguage: outputLanguage, inputFiles: inputFiles, projectName: projectName, generateSourceMaps: generateSourceMaps);
            return Success;
        }

        private static bool ParseProjectFile(string projectFile, out CompilationJob job)
        {
            job = null;
            if (!IsLegalPProjFile(projectFile, out FileInfo fullPathName))
            {
                CommandlineOutput.WriteMessage(
                    $"Illegal P project file name {projectFile} or file {fullPathName?.FullName} not found", SeverityKind.Error);
                return false;
            }

            CommandlineOutput.WriteMessage($".... Parsing the project file: {projectFile}", SeverityKind.Info);

            CompilerOutput outputLanguage = CompilerOutput.C;
            DirectoryInfo outputDirectory = null;
            List<FileInfo> inputFiles = new List<FileInfo>();
            string targetName = null;
            bool generateSourceMaps = false;

            XElement projectXML = XElement.Load(fullPathName.FullName);

            // get all files to be compiled

            foreach (XElement inputs in projectXML.Elements("InputFiles"))
            {
                foreach (XElement inputFileName in inputs.Elements("PFile"))
                {
                    var pFiles = new List<string>();

                    if(Directory.Exists(inputFileName.Value))
                    {
                        foreach(var files in Directory.GetFiles(inputFileName.Value, "*.p"))
                        {
                            pFiles.Add(files);
                        }
                    }
                    else
                    {
                        pFiles.Add(inputFileName.Value);   
                    }

                    foreach(var pFile in pFiles)
                    {
                        if (IsLegalPFile(pFile, out FileInfo pFilePathName))
                        {
                            CommandlineOutput.WriteMessage($"....... project includes: {pFilePathName.FullName}", SeverityKind.Info);
                            inputFiles.Add(pFilePathName);
                        }
                        else
                        {
                            CommandlineOutput.WriteMessage(
                                $"Illegal P file name {pFile} or file {pFilePathName?.FullName} not found", SeverityKind.Error);
                        }
                    }
                }
            }

            if (inputFiles.Count == 0)
            {
                CommandlineOutput.WriteMessage("At least one .p file must be provided as input files", SeverityKind.Error);
                return false;
            }

            // get target file name
            if (projectXML.Elements("TargetFileName").Any())
            {
                targetName = projectXML.Element("TargetFileName").Value;
                if (!IsLegalUnitName(targetName))
                {
                    CommandlineOutput.WriteMessage($"{targetName} is not a legal target file name", SeverityKind.Error);
                    return false;
                }
            }

            string projectName = targetName ?? Path.GetFileNameWithoutExtension(inputFiles[0].FullName);

            // get output directory
            outputDirectory = projectXML.Elements("OutputDir").Any() ? Directory.CreateDirectory(projectXML.Element("OutputDir").Value) : new DirectoryInfo(Directory.GetCurrentDirectory());

            // get target language
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
                            CommandlineOutput.WriteMessage($"Expected true or false, received {projectXML.Element("Target").Attribute("sourcemaps").Value}", SeverityKind.Error);
                        }
                        break;

                    case "coyote":
                        outputLanguage = CompilerOutput.Coyote;
                        break;

                    default:
                        outputLanguage = CompilerOutput.C;
                        break;
                }
            }

            job = new CompilationJob(output: new DefaultCompilerOutput(outputDirectory), outputLanguage: outputLanguage, inputFiles: inputFiles, projectName: projectName, generateSourceMaps: generateSourceMaps);
            return true;
        }

        private static bool IsLegalUnitName(string unitFileName)
        {
            return Regex.IsMatch(unitFileName, "^[A-Za-z_][A-Za-z_0-9]*$");
        }

        private static bool IsLegalPFile(string fileName, out FileInfo file)
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

        private static bool IsLegalPProjFile(string fileName, out FileInfo file)
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

        public static void PrintUsage()
        {
            CommandlineOutput.WriteMessage("USAGE: Pc.exe file1.p [file2.p ...] [-t:tfile] [options]", SeverityKind.Info);
            CommandlineOutput.WriteMessage("USAGE: Pc.exe -proj:<.pproj file>", SeverityKind.Info);
            CommandlineOutput.WriteMessage("    -t:[tfileName]             -- name of output file produced for this compilation unit; if not supplied then file1", SeverityKind.Info);
            CommandlineOutput.WriteMessage("    -outputDir:[path]          -- where to write the generated files", SeverityKind.Info);
            CommandlineOutput.WriteMessage("    -generate:[C,Coyote]       -- select a target language to generate", SeverityKind.Info);
            CommandlineOutput.WriteMessage("        C   : generate C code using the Prt runtime", SeverityKind.Info);
            CommandlineOutput.WriteMessage("        Coyote  : generate C# code using the Coyote runtime", SeverityKind.Info);
            CommandlineOutput.WriteMessage("    -proj:[.pprojfile]         -- the p project to be compiled", SeverityKind.Info);
            CommandlineOutput.WriteMessage("    -sourcemaps[:(true|false)] -- enable or disable generating source maps", SeverityKind.Info);
            CommandlineOutput.WriteMessage("                                  in the compiled C output. may confuse some", SeverityKind.Info);
            CommandlineOutput.WriteMessage("                                  debuggers.", SeverityKind.Info);
            CommandlineOutput.WriteMessage("    -h, -help, --help          -- display this help message", SeverityKind.Info);
        }
    }
}