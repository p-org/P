using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Pc;
using Microsoft.Formula.API;
using System.IO;
using System.Xml;
namespace PBuild
{
    

    class PBuildTool
    {
        public class InputOptions
        {
            public string solutionXML;
            public bool rebuild;
            public CompilerOutput output;
            public string projectName;

            public InputOptions()
            {
                solutionXML = "";
                rebuild = false;
                output = CompilerOutput.CSharp;
                projectName = "";
            }
        }

        public PBuildTool()
        {
            Options = new InputOptions();
            currentSolution = new PSolutionInfo();
        }
        public InputOptions Options;

        bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == '/' || arg[0] == '-')
                {
                    arg = (arg.Substring(1).ToLowerInvariant());
                    string option = null;
                    int sep = arg.IndexOfAny(new char[] { '=', ':' });
                    if (sep > 0)
                    {
                        option = (arg.Substring(sep + 1).Trim()).ToLower();
                        arg = (arg.Substring(0, sep).Trim()).ToLower();
                    }
                    switch (arg)
                    {
                        case "sln":
                            Options.solutionXML = option;
                            break;
                        case "rebuild":
                            Options.rebuild = true;
                            break;
                        case "generate":
                            switch (option)
                            {
                                case "c":
                                    Options.output = CompilerOutput.C;
                                    break;
                                case "c#":
                                    Options.output = CompilerOutput.CSharp;
                                    break;
                                case "zing":
                                    Options.output = CompilerOutput.Zing;
                                    break;
                                default:
                                    WriteError("### Unrecognized option with generate: " + option);
                                    return false;
                            }
                            break;
                        case "project":
                            Options.projectName = option;
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
        static void PrintUsage()
        {
            Console.WriteLine("USAGE: PBuild.exe  [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("    /rebuild - force rebuild of the entire solution");
            Console.WriteLine("    /sln:<path> - path to the solution xml file");
            Console.WriteLine("    /generate:<C/C#/Zing> - specify the type of output to generate");
            Console.WriteLine("    /project:<name> - compile a particular project");
        }

        public class PSolutionInfo {
            public string name;
            public List<PProjectInfo> projects;
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
                foreach(var project in projects)
                {
                    Console.WriteLine("- Projects: {0}", project.name);
                    foreach(var psource in project.psources)
                    {
                        Console.WriteLine("---> includes p file: {0}", psource);
                    }
                    foreach (var dep in project.depends)
                    {
                        Console.WriteLine("--> depends on: {0}", dep);
                    }
                }
                Console.WriteLine("==============================================================");

            }
        }

        public class PProjectInfo
        {
            public string name;
            public List<string> depends;
            public List<string> psources;
            public List<string> testscripts;
            public string outputDir;
            public PProjectInfo()
            {
                name = "";
                depends = new List<string>();
                psources = new List<string>();
                testscripts = new List<string>();
                outputDir = null;
            }
        }

        public PSolutionInfo currentSolution;
        public void ParseSolution()
        {
            //read the config xml
            XmlDocument psolutionXML = new XmlDocument();
            psolutionXML.Load(Options.solutionXML);

            var fileName = Path.GetFileName(Options.solutionXML);
            Console.WriteLine("Loaded the solution file : {0}", fileName);
            try
            {

                var solName = psolutionXML.GetElementsByTagName("PSolution")[0].Attributes.GetNamedItem("name").Value.ToLower();
                var pprojects = psolutionXML.GetElementsByTagName("PProject");
                foreach(XmlNode project in pprojects)
                {
                    PProjectInfo projectInfo = new PProjectInfo();
                    projectInfo.name = project.Attributes.GetNamedItem("name").Value.ToLower();
                    projectInfo.outputDir = project.Attributes.GetNamedItem("outputdir").Value;
                    var psources = project["Source"].ChildNodes;
                    foreach(XmlNode pfile in psources)
                    {
                        projectInfo.psources.Add(pfile.InnerText);
                    }
                    var depends = project.SelectNodes("Depends");
                    foreach (XmlNode dproject in depends)
                    {
                        projectInfo.depends.Add(dproject.InnerText.ToLower());
                    }
                    
                    var plinkfiles = project["Link"];
                    if(plinkfiles != null)
                    {
                        foreach (XmlNode pfile in plinkfiles.ChildNodes)
                        {
                            projectInfo.testscripts.Add(pfile.InnerText);
                        }
                    }
                    

                    currentSolution.name = solName;
                    currentSolution.projects.Add(projectInfo);
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to parse the XML");
                Console.WriteLine(ex.ToString());
            }
        }

        public class ConsoleOutputStream : ICompilerOutput
        {
            public void WriteMessage(string msg, SeverityKind severity)
            {
                WriteError(msg);
            }
        }

        void CompileProject(PProjectInfo project)
        {
            
            var compileArgs = new CommandLineOptions();
            compileArgs.inputFileNames = new List<string>(project.psources);
            //populate all dependencies
            var depFiles = new List<string>();
            foreach(var depFile in project.depends)
            {
                var outDir = currentSolution.projects.Where(x => x.name == depFile).First().outputDir;
                depFiles.Add(Path.Combine(outDir, depFile + ".4ml"));
            }
            compileArgs.dependencies = new List<string>(depFiles);
            compileArgs.shortFileNames = true;
            compileArgs.outputDir = project.outputDir;
            compileArgs.shortFileNames = true;
            compileArgs.unitName = project.name + ".4ml";
            compileArgs.liveness = LivenessOption.None;
            compileArgs.compilerOutput = Options.output;
            compileArgs.profile = true;

            bool compileResult = false;

            // use separate process that contains pre-compiled P compiler.
            Console.WriteLine("==============================================================");
            Console.WriteLine("=== Compiling project {0} ===", project.name);
            CompilerServiceClient svc = new CompilerServiceClient();
            compileResult = svc.Compile(compileArgs, Console.Out);
            if (compileResult && project.testscripts.Count > 0)
            {
                Console.WriteLine("=== Linking project {0} ===", project.name);
                //start linking the project
                compileArgs.inputFileNames =  new List<string>(project.testscripts);
                //populate all summary files
                compileArgs.dependencies.Add(Path.Combine(project.outputDir, project.name + ".4ml"));
                compileResult = svc.Link(compileArgs, Console.Out);
            }
            Console.WriteLine("==============================================================");
        }
        #region Topological Sorting Dependencies
        /// <summary>
        /// Topological Sorting (Kahn's algorithm) 
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Topological_sorting</remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="nodes">All nodes of directed acyclic graph.</param>
        /// <param name="edges">All edges of directed acyclic graph.</param>
        /// <returns>Sorted node in topological order.</returns>
        static List<T> TopologicalSortFiles<T>(List<T> nodes, List<Tuple<T, T>> edges) where T : IEquatable<T>
        {
            // Empty list that will contain the sorted elements
            var L = new List<T>();

            // Set of all nodes with no incoming edges
            var S = new HashSet<T>(nodes.Where(n => edges.All(e => e.Item2.Equals(n) == false)));

            // while S is non-empty do
            while (S.Any())
            {

                //  remove a node n from S
                var n = S.First();
                S.Remove(n);

                // add n to tail of L
                L.Add(n);

                // for each node m with an edge e from n to m do
                foreach (var e in edges.Where(e => e.Item1.Equals(n)).ToList())
                {
                    var m = e.Item2;

                    // remove edge e from the graph
                    edges.Remove(e);

                    // if m has no other incoming edges then
                    if (edges.All(me => me.Item2.Equals(m) == false))
                    {
                        // insert m into S
                        S.Add(m);
                    }
                }
            }

            // if graph has edges then
            if (edges.Any())
            {
                // return error (graph has at least one cycle)
                return null;
            }
            else
            {
                // return L (a topologically sorted order)
                return L;
            }
        }
        #endregion


        static void Main(string[] args)
        {

            PBuildTool p = new PBuildTool();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return;
            }
            //parse the solution file
            if (!File.Exists(p.Options.solutionXML))
            {
                WriteError("The solution file does not exist: {0}", p.Options.solutionXML);
                return;
            }
            else
            {
                p.ParseSolution();
            }

            //check if the parsed solution is correct
            //p.currentSolution.Check(p.Options.projectName);

            //print information
            p.currentSolution.PrintInfo();

            if(p.Options.projectName != "")
            {
                //compile only one project

            }
            else
            {
                //compile the entire solution
                var nodes = p.currentSolution.projects.Select(x => x.name).ToList();
                var edges = new List<Tuple<string, string>>();
                foreach (var project in p.currentSolution.projects)
                {
                    foreach (var dep in project.depends)
                    {
                        if (project.name != dep)
                        {
                            edges.Add(new Tuple<string, string>(dep.ToLower(), project.name));
                        }
                    }
                }

                var orderedProjects = TopologicalSortFiles<string>(nodes, edges);
                foreach(var project in orderedProjects)
                {
                    //compile each project and then link it
                    var projectInfo = p.currentSolution.projects.Where(x => x.name == project).First();
                    //if(p.CheckIfCompileProject(projectInfo))
                    p.CompileProject(projectInfo);
                }
            }

        }
    }
}
