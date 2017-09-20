using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Formula.API;
using Microsoft.Pc;

namespace PBuild
{
    internal class PBuildTool
    {
        public PSolutionInfo CurrentSolution;
        public InputOptions Options;

        public PBuildTool()
        {
            Options = new InputOptions();
            CurrentSolution = new PSolutionInfo();
        }

        private bool ParseCommandLine(string[] args)
        {
            foreach (var t in args)
            {
                var arg = t;
                if (arg[0] == '/' || arg[0] == '-')
                {
                    arg = arg.Substring(1);
                    string option = null;
                    var sep = arg.IndexOfAny(new[] {'=', ':'});
                    if (sep > 0)
                    {
                        option = arg.Substring(sep + 1).Trim();
                        arg = arg.Substring(0, sep).Trim();
                    }
                    switch (arg)
                    {
                        case "sln":
                            Options.SolutionXml = option;
                            break;
                        case "rebuild":
                            Options.Rebuild = true;
                            break;
                        case "relink":
                            Options.Relink = true;
                            break;
                        case "generate":
                            switch (option)
                            {
                                case "c":
                                    Options.Output = CompilerOutput.C;
                                    break;
                                case "c#":
                                    Options.Output = CompilerOutput.CSharp;
                                    break;
                                case "zing":
                                    Options.Output = CompilerOutput.Zing;
                                    break;
                                default:
                                    WriteError("### Unrecognized option with generate: " + option);
                                    return false;
                            }
                            break;
                        case "project":
                            Options.ProjectName = option;
                            break;
                        default:
                            WriteError("### Unrecognized option: " + arg);
                            return false;
                    }
                }
                else
                {
                    WriteError("### Unrecognized option");
                    return false;
                }
            }
            return true;
        }

        private static void WriteError(string format, params object[] args)
        {
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, args);
            Console.ForegroundColor = saved;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("USAGE: PBuild.exe  [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("    /rebuild - force rebuild");
            Console.WriteLine("    /sln:<path> - path to the solution xml file");
            Console.WriteLine("    /generate:<C/C#/Zing> - specify the type of output to generate");
            Console.WriteLine("    /project:<name> - compile a particular project");
            Console.WriteLine("    /relink - force re-link");
        }

        public void ParseSolution()
        {
            //read the config xml
            var psolutionXml = new XmlDocument();
            psolutionXml.Load(Options.SolutionXml);

            var fileName = Path.GetFileName(Options.SolutionXml);
            Console.WriteLine("Loaded the solution file : {0}", fileName);
            try
            {
                var solName = psolutionXml.GetElementsByTagName("PSolution")[0].Attributes.GetNamedItem("name").Value;
                var pprojects = psolutionXml.GetElementsByTagName("PProject");
                foreach (XmlNode project in pprojects)
                {
                    var projectInfo = new PProjectInfo
                    {
                        Name = project.Attributes.GetNamedItem("name").Value,
                        outputDir = Path.GetFullPath(project.Attributes.GetNamedItem("outputdir").Value)
                    };
                    var psources = project["Source"].ChildNodes;
                    foreach (XmlNode pfile in psources)
                        projectInfo.psources.Add(pfile.InnerText);
                    var depends = project.SelectNodes("Depends");
                    foreach (XmlNode dproject in depends)
                        projectInfo.depends.Add(dproject.InnerText);

                    var plinkfiles = project["Link"];
                    if (plinkfiles != null)
                        foreach (XmlNode pfile in plinkfiles.ChildNodes)
                            projectInfo.testscripts.Add(pfile.InnerText);

                    CurrentSolution.projects.Add(projectInfo);
                }
                CurrentSolution.name = solName;
                CurrentSolution.solutionDir = Path.GetDirectoryName(Path.GetFullPath(Options.SolutionXml));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to parse the XML");
                Console.WriteLine(ex.ToString());
            }
        }

        private void CompileProject(PProjectInfo project)
        {
            var compileArgs = new CommandLineOptions
            {
                inputFileNames = new List<string>(project.psources.Select(Path.GetFullPath).ToList())
            };
            //populate all dependencies
            var depFiles = (from depFile in project.depends let outDir = Path.GetFullPath(CurrentSolution.projects.First(x => x.Name == depFile).outputDir) select Path.Combine(outDir, depFile + ".4ml")).ToList();
            compileArgs.dependencies = new List<string>(depFiles);
            compileArgs.shortFileNames = true;
            compileArgs.outputDir = project.outputDir;
            compileArgs.unitName = project.Name + ".4ml";
            compileArgs.liveness = LivenessOption.None;
            compileArgs.compilerOutput = Options.Output;
            compileArgs.profile = true;

            var compileResult = false;
            var svc = new Compiler(true);
            if (Options.Relink && !Options.Rebuild)
            {
                compileResult = true;
            }
            else
            {
                // use separate process that contains pre-compiled P compiler.
                Console.WriteLine("==============================================================");
                Console.WriteLine("=== Compiling project {0} ===", project.Name);

                compileResult = svc.Compile(new StandardOutput(), compileArgs);
            }

            if (!compileResult)
            {
                Environment.Exit(-1);
            }

            if (project.testscripts.Count > 0)
            {
                Console.WriteLine("=== Linking project {0} ===", project.Name);
                //start linking the project
                compileArgs.inputFileNames =
                    new List<string>(project.testscripts.Select(Path.GetFullPath).ToList());
                //populate all summary files
                compileArgs.dependencies.Add(Path.Combine(project.outputDir, project.Name + ".4ml"));
                svc.Link(new StandardOutput(), compileArgs);
            }
            Console.WriteLine("==============================================================");
        }

        public bool CheckIfCompileProject(PProjectInfo project)
        {
            var returnVal = false;
            var summaryFile = Path.Combine(project.outputDir, project.Name + ".4ml");
            var allPSources = new List<string>(project.psources.Select(Path.GetFullPath).ToList());
            var summaryFileWriteTime = File.GetLastWriteTime(summaryFile);
            foreach (var pfile in allPSources)
                if (DateTime.Compare(summaryFileWriteTime, File.GetLastWriteTime(pfile)) <= 0)
                    returnVal = true;

            return returnVal;
        }

        #region Topological Sorting Dependencies

        /// <summary>
        ///     Topological Sorting (Kahn's algorithm)
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Topological_sorting</remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="nodes">All nodes of directed acyclic graph.</param>
        /// <param name="edges">All edges of directed acyclic graph.</param>
        /// <returns>Sorted node in topological order.</returns>
        private static List<T> TopologicalSortFiles<T>(List<T> nodes, List<Tuple<T, T>> edges) where T : IEquatable<T>
        {
            // Empty list that will contain the sorted elements
            var l = new List<T>();

            // Set of all nodes with no incoming edges
            var S = new HashSet<T>(nodes.Where(n => edges.All(e => e.Item2.Equals(n) == false)));

            // while S is non-empty do
            while (S.Any())
            {
                //  remove a node n from S
                var n = S.First();
                S.Remove(n);

                // add n to tail of L
                l.Add(n);

                // for each node m with an edge e from n to m do
                foreach (var e in edges.Where(e => e.Item1.Equals(n)).ToList())
                {
                    var m = e.Item2;

                    // remove edge e from the graph
                    edges.Remove(e);

                    // if m has no other incoming edges then
                    if (edges.All(me => me.Item2.Equals(m) == false))
                        S.Add(m);
                }
            }

            // if graph has edges then
            if (edges.Any())
                return null;
            return l;
        }

        #endregion


        private static void Main(string[] args)
        {
            var p = new PBuildTool();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return;
            }
            //parse the solution file
            if (!File.Exists(p.Options.SolutionXml))
            {
                WriteError("Please provide a solution file: {0}", p.Options.SolutionXml);
                PrintUsage();
                return;
            }
            p.ParseSolution();

            //check if all is correct
            p.CheckXml();
            //print information
            p.CurrentSolution.PrintInfo();

            //setting current directory
            Directory.SetCurrentDirectory(p.CurrentSolution.solutionDir);
            Console.WriteLine("Set current Directory: {0}", Directory.GetCurrentDirectory());

            if (p.Options.ProjectName != "")
            {
                //compile only one project
                //compile the entire solution
                var compileProject = p.CurrentSolution.projects.First(x => x.Name == p.Options.ProjectName);
                var nodes = compileProject.depends.ToList();
                nodes.Add(compileProject.Name);
                var edges = (from dep in compileProject.depends where compileProject.Name != dep select new Tuple<string, string>(dep, compileProject.Name)).ToList();

                var orderedProjects = TopologicalSortFiles(nodes, edges);
                var rebuild = false;
                foreach (var project in orderedProjects)
                {
                    //compile each project and then link it
                    var projectInfo = p.CurrentSolution.projects.First(x => x.Name == project);
                    rebuild = p.CheckIfCompileProject(projectInfo) || rebuild;
                    if (rebuild || p.Options.Rebuild || p.Options.Relink)
                    {
                        p.CompileProject(projectInfo);
                    }
                    else
                    {
                        Console.WriteLine("==============================================================");
                        Console.WriteLine("Ignoring compilation of project {0}, to recompile use option /rebuild",
                            projectInfo.Name);
                        Console.WriteLine("==============================================================");
                    }
                }
            }
            else
            {
                //compile the entire solution
                var nodes = p.CurrentSolution.projects.Select(x => x.Name).ToList();
                var edges = (from project in p.CurrentSolution.projects from dep in project.depends where project.Name != dep select new Tuple<string, string>(dep, project.Name)).ToList();

                var orderedProjects = TopologicalSortFiles(nodes, edges);
                var rebuild = false;
                foreach (var project in orderedProjects)
                {
                    //compile each project and then link it
                    var projectInfo = p.CurrentSolution.projects.First(x => x.Name == project);
                    rebuild = p.CheckIfCompileProject(projectInfo) || rebuild;
                    if (rebuild || p.Options.Rebuild || p.Options.Relink)
                    {
                        p.CompileProject(projectInfo);
                    }
                    else
                    {
                        Console.WriteLine("==============================================================");
                        Console.WriteLine("Ignoring compilation of project {0}, to recompile use option /rebuild",
                            projectInfo.Name);
                        Console.WriteLine("==============================================================");
                    }
                }
            }
        }

        private void CheckXml()
        {
            foreach (var project in CurrentSolution.projects)
            {
                foreach (var psource in project.psources)
                {
                    if (!File.Exists(psource))
                    {
                        WriteError("File {0} not found", psource);
                    }
                }

                foreach (var depProject in project.depends)
                {
                    if (!CurrentSolution.projects.Select(x => x.Name == depProject).Any())
                    {
                        WriteError("Project {0} not in solution", depProject);
                    }
                }

                foreach (var psource in project.testscripts)
                {
                    if (!File.Exists(psource))
                    {
                        WriteError("File {0} not found", psource);
                    }
                }
            }

            if (!CurrentSolution.projects.Select(x => x.Name == Options.ProjectName).Any())
            {
                WriteError("Project {0} not in solution", Options.ProjectName);
            }
        }

        public class InputOptions
        {
            public CompilerOutput Output;
            public string ProjectName;
            public bool Rebuild;
            public bool Relink;
            public string SolutionXml;

            public InputOptions()
            {
                SolutionXml = "";
                Rebuild = false;
                Relink = false;
                Output = CompilerOutput.CSharp;
                ProjectName = "";
            }
        }

        public class PSolutionInfo
        {
            public string name;
            public List<PProjectInfo> projects;
            public string solutionDir;

            public PSolutionInfo()
            {
                name = "";
                projects = new List<PProjectInfo>();
            }

            public void PrintInfo()
            {
                Console.WriteLine("==============================================================");
                Console.WriteLine("Solution Information:");
                Console.WriteLine("Name : {0}", name);
                foreach (var project in projects)
                {
                    Console.WriteLine("- Projects: {0}", project.Name);
                    foreach (var psource in project.psources)
                        Console.WriteLine("---> includes p file: {0}", psource);
                    foreach (var dep in project.depends)
                        Console.WriteLine("--> depends on: {0}", dep);
                }
                Console.WriteLine("==============================================================");
            }
        }

        public class PProjectInfo
        {
            public List<string> depends;
            public string Name;
            public string outputDir;
            public List<string> psources;
            public List<string> testscripts;

            public PProjectInfo()
            {
                Name = "";
                depends = new List<string>();
                psources = new List<string>();
                testscripts = new List<string>();
                outputDir = null;
            }
        }

        public class ConsoleOutputStream : ICompilerOutput
        {
            public void WriteMessage(string msg, SeverityKind severity)
            {
                WriteError(msg);
            }
        }
    }
}