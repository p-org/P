using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Threading;
using System.Diagnostics;


namespace PrtDistDeployer
{

    class PrintHelper
    {
        public static void Red(string log)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(log);
            Console.ForegroundColor = oldColor;
        }

        public static void Green(string log)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(log);
            Console.ForegroundColor = oldColor;
        }

        public static void Normal(string log)
        {
            Console.WriteLine(log);
        }
    }

    class ClusterConfiguration
    {
        //This can be made more secured in the future
        public static string username = "planguser";
        public static string password = "Pldi2015";
        public static string MainExe;
        public static string NodeManagerPort;
        public static string ContainerPortStart;
        public static string NetworkShare;
        public static string LocalFolder;
        public static string MainMachineNode;
        public static string CentralServerNode;
        public static int TotalNodes;
        public static string psToolsPath;
        public static string pBinPath;
        public static string clientBinPath;
        public static string configFileName;
        public static Dictionary<int, string> AllNodes = new Dictionary<int, string>();

        public static void Initialize()
        {
            //read the config xml
            XmlDocument config = new XmlDocument();
            XmlNodeList nodes;
            config.Load(CommandLineOptions.pathToClusterConfig);

            configFileName = Path.GetFileName(CommandLineOptions.pathToClusterConfig);
            Console.WriteLine("Loaded the config file : {0}", configFileName);
            try
            {
                psToolsPath = config.GetElementsByTagName("PSTools")[0].InnerText;
                pBinPath = config.GetElementsByTagName("PBinaries")[0].InnerText;
                MainExe = config.GetElementsByTagName("MainExe")[0].InnerText;
                clientBinPath = System.IO.Path.GetDirectoryName(MainExe);
                NodeManagerPort = config.GetElementsByTagName("NodeManagerPort")[0].InnerText;
                ContainerPortStart = config.GetElementsByTagName("ContainerPortStart")[0].InnerText;

                NetworkShare = config.GetElementsByTagName("NetworkShare")[0].InnerText;
                LocalFolder = config.GetElementsByTagName("LocalFolder")[0].InnerText;
                MainMachineNode = config.GetElementsByTagName("MainMachineNode")[0].InnerText;
                CentralServerNode = config.GetElementsByTagName("CentralServer")[0].InnerText;
                TotalNodes = int.Parse(config.GetElementsByTagName("TotalNodes")[0].InnerText);
                nodes = config.GetElementsByTagName("Node");
                int index = 0;
                foreach (XmlNode n in nodes)
                {
                    AllNodes.Add(index, n.InnerText);
                    index++;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to parse the XML");
                Console.WriteLine(ex.ToString());
            }

            //check if the config file is correct.
            if (!Directory.Exists(psToolsPath))
            {
                PrintHelper.Red("Directory : " + psToolsPath + " not found");
                Environment.Exit(1);
            }
            if (!Directory.Exists(clientBinPath))
            {
                PrintHelper.Red("Directory : " + clientBinPath + " not found");
                Environment.Exit(1);
            }
            else
            {
                if(!File.Exists(MainExe))
                {
                    PrintHelper.Red(" MainExe not found in " + clientBinPath);
                    Environment.Exit(1);
                }
            }
            if (Directory.Exists(psToolsPath))
            {
                if (!(File.Exists(psToolsPath + "\\PsExec.exe") && File.Exists(psToolsPath + "\\pskill.exe")))
                {
                    PrintHelper.Red("File : PsExec or PsKill not found");
                    Environment.Exit(1);
                }
            }
            else
            {
                PrintHelper.Red("Directory : " + psToolsPath + " not found");
                Environment.Exit(1);
            }



            //depending on config file infer whether we are performing debug-only
            string lh = "localhost";
            if (CentralServerNode == lh && MainMachineNode == lh && TotalNodes == 1 && AllNodes.Where(n => n.Value != lh).Count() == 0)
            {
                CommandLineOptions.debugLocally = true;
                PrintHelper.Normal("||Debugging Locally||");
            }
            else
            {
                CommandLineOptions.debugLocally = false;
                PrintHelper.Normal("||Deploying on cluster, username: " + ClusterConfiguration.username + "||");
            }

        }
    }
    class PrtDistDeployer
    {
        public static void PrintOptions()
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("1 : Deploy the service");
            Console.WriteLine("2 : Kill deployed service and NodeManagers on all machines");
            Console.WriteLine("3 : Exit");
            Console.WriteLine();
            Console.Write("Option: ");
            Console.ForegroundColor = oldColor;
        }

        public static void KillService()
        {
            //$psKill + " -t -u planguser -p Pldi2015 \\$nn PrtDistService.exe"
            foreach (var node in ClusterConfiguration.AllNodes)
            {
                PrintHelper.Green("Killing " + ClusterConfiguration.MainExe + " on " + node.Value);
                string kill_MainExe;
                if (CommandLineOptions.debugLocally)
                {
                    kill_MainExe = String.Format(@" -t {0}", ClusterConfiguration.MainExe);
                }
                else
                {
                    kill_MainExe = String.Format(@" -t -u {0} -p {1} \\{2} {3}", ClusterConfiguration.username, ClusterConfiguration.password, node.Value, ClusterConfiguration.MainExe);
                }

                {
                    string psExec = ClusterConfiguration.psToolsPath + "\\pskill.exe";
                    ProcessStartInfo startInfo = new ProcessStartInfo(psExec, kill_MainExe);
                    Process proc = new Process();
                    proc.StartInfo = startInfo;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.Start();
                    int timeoutInMilliSecs = 5 * 1000; // 5 seconds
                    string error = proc.StandardError.ReadToEnd();
                    string output = proc.StandardOutput.ReadToEnd();
                    bool didExitbeforeTimeout = proc.WaitForExit(timeoutInMilliSecs);
                    int errorCode = proc.ExitCode;
                    proc.Close();
                    //Console.WriteLine("PsKill return value :{0}", errorCode);
                    //Console.WriteLine("{0}", error);
                }


                PrintHelper.Green("Killing NodeManager on " + node.Value);
                string kill_nodemanager;
                if (CommandLineOptions.debugLocally)
                {
                    kill_nodemanager = String.Format(@" -t NodeManager.exe");
                }
                else
                {
                    kill_nodemanager = String.Format(@" -t -u {0} -p {1} \\{2} NodeManager.exe", ClusterConfiguration.username, ClusterConfiguration.password, node.Value);
                }
                {
                    string psExec = ClusterConfiguration.psToolsPath + "\\pskill.exe";
                    ProcessStartInfo startInfo = new ProcessStartInfo(psExec, kill_nodemanager);
                    Process proc = new Process();
                    proc.StartInfo = startInfo;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.Start();
                    int timeoutInMilliSecs = 5 * 1000; // 5 seconds
                    string error = proc.StandardError.ReadToEnd();
                    string output = proc.StandardOutput.ReadToEnd();
                    bool didExitbeforeTimeout = proc.WaitForExit(timeoutInMilliSecs);
                    int errorCode = proc.ExitCode;
                    proc.Close();
                    //Console.WriteLine("PsKill return value :{0}", errorCode);
                    //Console.WriteLine("{0}", error);

                }

                

            }
        }

        public static void DeployService()
        {

            #region Copy all the files
            //copy all the binaries on the network share
            //delete all the existing files.
            if (!Directory.Exists(ClusterConfiguration.NetworkShare))
            {
                PrintHelper.Red("Network share doesnot exist : " + ClusterConfiguration.NetworkShare);
                Environment.Exit(1);
            }
            DirectoryInfo nS = new DirectoryInfo(ClusterConfiguration.NetworkShare);
            foreach (var file in nS.GetFiles())
            {
                file.Delete();
            }

            PrintHelper.Green("Deleted all the old files in network share : " + ClusterConfiguration.NetworkShare);

            //copy the xml file
            File.Copy(CommandLineOptions.pathToClusterConfig, ClusterConfiguration.NetworkShare + "\\" + Path.GetFileName(CommandLineOptions.pathToClusterConfig));
            //copy all the pbinaries
            nS = new DirectoryInfo(ClusterConfiguration.pBinPath);
            foreach (var file in nS.GetFiles())
            {
                file.CopyTo(ClusterConfiguration.NetworkShare + "\\" + file.Name, true);
            }
            //copy all the service binaries
            nS = new DirectoryInfo(ClusterConfiguration.clientBinPath);
            foreach (var file in nS.GetFiles())
            {
                file.CopyTo(ClusterConfiguration.NetworkShare + "\\" + file.Name, true);
            }

            #endregion

            #region Copy all binaries in local folder

            //$psExec + " -d -u planguser -p Pldi2015 \\$nn Robocopy $deploymentFolder $localFolder /E /PURGE"
            foreach (var node in ClusterConfiguration.AllNodes)
            {
                string robocopy_command;
                if(CommandLineOptions.debugLocally)
                {
                    robocopy_command = String.Format(@" -d Robocopy {0} {1} /E /PURGE", ClusterConfiguration.NetworkShare, ClusterConfiguration.LocalFolder);
                }
                else
                {
                    robocopy_command = String.Format(@" -d -u {0} -p {1} \\{2} Robocopy {3} {4} /E /PURGE", ClusterConfiguration.username, ClusterConfiguration.password, node.Value, ClusterConfiguration.NetworkShare, ClusterConfiguration.LocalFolder);
                }
                
                string psExec = ClusterConfiguration.psToolsPath + "\\psexec.exe";
                ProcessStartInfo startInfo = new ProcessStartInfo(psExec, robocopy_command);
                Process proc = new Process();
                proc.StartInfo = startInfo;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.Start();
                int timeoutInMilliSecs = 5 * 1000; // 5 seconds
                string error = proc.StandardError.ReadToEnd();
                string output = proc.StandardOutput.ReadToEnd();
                bool didExitbeforeTimeout = proc.WaitForExit(timeoutInMilliSecs);
                int errorCode = proc.ExitCode;
                proc.Close();
                Console.WriteLine("PsExec return value :{0}", errorCode);
                Console.WriteLine("{0}", error);
            }

            #endregion

            #region Start NodeManager
            //start node manager at all the nodes except the main

            foreach (var node in ClusterConfiguration.AllNodes)
            {
                if (node.Value.Equals(ClusterConfiguration.MainMachineNode))
                {
                    continue;
                }
                else
                {
                    PrintHelper.Green("Started NodeManager on " + node.Value);
                    //start the NodeManager in listening mode.
                    //Nodemanager.exe nodeId 0
                    //$psExec + " -d -u planguser -p Pldi2015 \\$nn $localFolder\PrtDistService.exe"
                    string start_nodemanager;
                    if(CommandLineOptions.debugLocally)
                    {
                        start_nodemanager = String.Format(@" -d {0}\NodeManager.exe {2} {1} 0", node.Value, ClusterConfiguration.LocalFolder, node.Key, ClusterConfiguration.configFileName);
                    }
                    else
                    {
                        start_nodemanager = String.Format(@" -d -u {0} -p {1} \\{2} {3}\NodeManager.exe {5} {4} 0", ClusterConfiguration.username, ClusterConfiguration.password, node.Value, ClusterConfiguration.LocalFolder, node.Key, ClusterConfiguration.configFileName);
                    }
                    
                    string psExec = ClusterConfiguration.psToolsPath + "\\psexec.exe";
                    ProcessStartInfo startInfo = new ProcessStartInfo(psExec, start_nodemanager);
                    Process proc = new Process();
                    proc.StartInfo = startInfo;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.Start();
                    int timeoutInMilliSecs = 5 * 1000; // 5 seconds
                    string error = proc.StandardError.ReadToEnd();
                    string output = proc.StandardOutput.ReadToEnd();
                    bool didExitbeforeTimeout = proc.WaitForExit(timeoutInMilliSecs);
                    int errorCode = proc.ExitCode;
                    proc.Close();
                    Console.WriteLine("PsExec return value :{0}", errorCode);
                    Console.WriteLine("{0}", error);
                }
            }

            //Start the main machine
            PrintHelper.Green("Started NodeManager and Main machine on " + ClusterConfiguration.MainMachineNode);
            //start the NodeManager with main
            //NodeManager.exe nodeId 1
            string start_main;
            if(CommandLineOptions.debugLocally)
            {
                start_main = String.Format(@" -d {0}\NodeManager.exe {2} {1} 1", ClusterConfiguration.LocalFolder, ClusterConfiguration.AllNodes.Where(n => n.Value == ClusterConfiguration.MainMachineNode).First().Key, ClusterConfiguration.configFileName);
            }
            else
            {
                start_main = String.Format(@" -d -u {0} -p {1} \\{2} {3}\NodeManager.exe {5} {4} 1", ClusterConfiguration.username, ClusterConfiguration.password, ClusterConfiguration.MainMachineNode,ClusterConfiguration.LocalFolder, ClusterConfiguration.AllNodes.Where(n => n.Value == ClusterConfiguration.MainMachineNode).First().Key, ClusterConfiguration.configFileName);
            }
            
            {
                string psExec = ClusterConfiguration.psToolsPath + "\\psexec.exe";
                ProcessStartInfo startInfo = new ProcessStartInfo(psExec, start_main);
                Process proc = new Process();
                proc.StartInfo = startInfo;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.Start();
                int timeoutInMilliSecs = 5 * 1000; // 5 seconds
                string error = proc.StandardError.ReadToEnd();
                string output = proc.StandardOutput.ReadToEnd();
                bool didExitbeforeTimeout = proc.WaitForExit(timeoutInMilliSecs);
                int errorCode = proc.ExitCode;
                proc.Close();
                Console.WriteLine("PsExec return value :{0}", errorCode);
                Console.WriteLine("{0}", error);
            }
            #endregion

        }
        public static void Main(string[] args)
        {
            
            //Parse the command line arguments
            CommandLineOptions.ParseCommandline(args);

            //Initialize the Cluster Configuration
            ClusterConfiguration.Initialize();


            while (true)
            {
                PrintHelper.Red("Pick the operation to be Performed");
                PrintOptions();
                var pressedOption = Console.ReadLine();
                int option;
                var success = int.TryParse(pressedOption, out option);
                if(!success)
                {
                    PrintHelper.Red("Invalid Option");
                    continue;
                }
                switch(option)
                {
                    case 1:
                        DeployService();
                        break;
                    case 2:
                        KillService();
                        break;
                    case 3:
                        Environment.Exit(0);
                        break;
                    default:
                        PrintHelper.Red("Invalid Option");
                        break;
                }
            }
        }

    }
}
