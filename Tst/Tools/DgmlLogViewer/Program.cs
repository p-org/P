using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DgmlLogViewer
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                p.PrintUsage();
            }
            else
            {
                p.Run();
            }
        }
        void PrintUsage()
        {
            Console.WriteLine("Usage: {0} <logfile>", Path.GetFileName(typeof(Program).Assembly.Location));
            Console.WriteLine("This tool parses the P log output containing stuff like <EnqueueLog> and produces a DGML diagram of what happened in the state machine");
        }

        bool ParseCommandLine(string[] args)
        {
            for (int i = 0, n = args.Length; i< n; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    // parse switch
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "h":
                        case "help":
                        case "?":
                            return false;
                        default:
                            Console.WriteLine("Error: Unexpected argument '{0}'", arg);
                            return false;
                    }
                }
                else
                {
                    // positional argument.
                    if (!AddPositionalArgument(arg))
                    {
                        return false;
                    }
                }
            }
            if (!CheckPositionalArguments())
            {
                return false;
            }
            return true;
        }

        private bool AddPositionalArgument(string arg)
        {
            if (logFile == null)
            {
                logFile = Path.GetFullPath(arg);
                if (!File.Exists(logFile))
                {
                    Console.WriteLine("Error: logfile '{0}' not found", logFile);
                    return false;
                }
                return true;
            }
            else
            {
                Console.WriteLine("Error: Too many arguments");
            }
            return false;
        }

        private bool CheckPositionalArguments()
        {
            if (logFile == null)
            {
                Console.WriteLine("Error: Missing logfile argument");
                return false;
            }
            return true;
        }

        string logFile;
        DgmlLite dgml;

        void Run()
        {
            dgml = new DgmlLite();
            using (var reader = new StreamReader(logFile))
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    ParseLog(line);
                    line = reader.ReadLine();
                }
            }

            string baseName = Path.GetFileNameWithoutExtension(logFile);
            string outPath = Path.Combine(Path.GetDirectoryName(logFile), baseName + ".dgml");
            dgml.Save(outPath);
            Console.WriteLine("Saved {0}", outPath);
        }

        Regex enqueueLog = new Regex(@"\<EnqueueLog\> Enqueued event (.*) with payload (.*) on Machine (.*)");
        Regex actionLog = new Regex(@"\<ActionLog\> Machine (.*) executed action in state (.*)");
        Regex createLog = new Regex(@"\<CreateLog\> Machine (.*) is created");
        Regex stateLog = new Regex(@"\<StateLog\> Machine (.*) entered state (.*)");
        Regex exitLog = new Regex(@"\<ExitLog\> Machine (.*) exiting state (.*)");
        Regex raiseLog = new Regex(@"\<RaiseLog\> Machine (.*) raised event (.*) with payload (.*)");
        Regex popLog = new Regex(@"<PopLog> Machine (.*) popped and reentered state (.*)");

        Dictionary<string, string> currentStates = new Dictionary<string, string>();
        Dictionary<string, string> eventQueue = new Dictionary<string, string>();

        private void ParseLog(string line)
        {
            if (line.StartsWith(" < CreateLog>"))
            {
                var match = createLog.Match(line);

                if (match.Success && match.Groups.Count > 1)
                {
                    string machineName = match.Groups[1].Value;
                    dgml.GetOrCreateGroup(machineName);
                }
            }

            if (line.StartsWith("<StateLog>"))
            {
                var match = stateLog.Match(line);
                if (match.Success && match.Groups.Count > 2)
                {
                    string machineName = match.Groups[1].Value;
                    string stateName = match.Groups[2].Value;
                    dgml.GetOrCreateNodeInGroup(machineName, stateName);

                    string currentState;
                    if (currentStates.TryGetValue(machineName, out currentState) && currentState != stateName)
                    {
                        string eventName = null;
                        eventQueue.TryGetValue(machineName, out eventName);
                        dgml.GetOrCreateLink(machineName + "." + currentState, machineName + "." + stateName, eventName);
                        eventQueue.Clear();
                    }

                    currentStates[machineName] = stateName;
                }

            }

            if (line.StartsWith("<PopLog>"))
            {
                var match = popLog.Match(line);
                if (match.Success && match.Groups.Count > 2)
                {
                    string machineName = match.Groups[1].Value;
                    string stateName = match.Groups[2].Value;
                    dgml.GetOrCreateNodeInGroup(machineName, stateName);

                    string currentState;
                    if (currentStates.TryGetValue(machineName, out currentState) && currentState != stateName)
                    {
                        string eventName = null;
                        eventQueue.TryGetValue(machineName, out eventName);
                        dgml.GetOrCreateLink(machineName + "." + currentState, machineName + "." + stateName, eventName);
                    }

                    currentStates[machineName] = stateName;
                }

            }

            if (line.StartsWith("<EnqueueLog>"))
            {
                var match = enqueueLog.Match(line);
                if (match.Success)
                {
                    if (match.Groups.Count > 3)
                    {
                        string eventName = match.Groups[1].Value;
                        string machineName = match.Groups[3].Value;
                        eventQueue[machineName] = eventName;
                    }
                }
            }

            if (line.StartsWith("<RaiseLog>"))
            {
                // sometimes this happens without recording an <EnqueueLog>, when event is raised and handled in the same machine.

                var match = raiseLog.Match(line);
                if (match.Success)
                {
                    if (match.Groups.Count > 3)
                    {
                        string eventName = match.Groups[2].Value;
                        string machineName = match.Groups[1].Value;
                        eventQueue[machineName] = eventName;
                    }
                }
            }
        }

    }
}
