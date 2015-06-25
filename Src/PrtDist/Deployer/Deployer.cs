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
        public static string MainExe;
        public static string NodeManagerPort;
        public static string ContainerPortStart;
        public static string NetworkShare;
        public static string LocalFolder;
        public static string MainMachineNode;
        public static string CentralServerNode;
        public static int TotalNodes;
        public static Dictionary<int, string> AllNodes = new Dictionary<int, string>();

        public static void Initialize()
        {
            //read the config xml
            XmlDocument config = new XmlDocument();
            XmlNodeList nodes;
            config.Load(CommandLineOptions.pathToClusterConfig);

            MainExe = config.GetElementsByTagName("MainExe")[0].InnerText;
            NodeManagerPort = config.GetElementsByTagName("NodeManagerPort")[0].InnerText;
            ContainerPortStart = config.GetElementsByTagName("ContainerPortStart")[0].InnerText;
            NetworkShare = config.GetElementsByTagName("NetworkShare")[0].InnerText;
            LocalFolder = config.GetElementsByTagName("LocalFolder")[0].InnerText;
            MainMachineNode = config.GetElementsByTagName("MainMachineNode")[0].InnerText;
            CentralServerNode = config.GetElementsByTagName("CentralServer")[0].InnerText;
            TotalNodes = int.Parse(config.GetElementsByTagName("TotalNodes")[0].InnerText);
            nodes = config.GetElementsByTagName("Node");
            int index = 0;
            foreach(XmlNode n in nodes)
            {
                AllNodes.Add(index, n.InnerText);
                index++;
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
            Console.WriteLine("1: Deploy the service");
            Console.WriteLine("2 : Kill Deployed Service and NodeManagers on all machines");
            Console.WriteLine("3 : Exit");
            Console.WriteLine();
            Console.Write("Enter the Option: ");
            Console.ForegroundColor = oldColor;
        }

        public static void KillService()
        {
            foreach (var node in ClusterConfiguration.AllNodes)
            {
                //kill all the nodemanagers

                //kill all the MainExe
            }
        }

        public static void DeployService()
        {

            #region Copy all the files
            //copy all the binaries on the network share
            //delete all the existing files.
            if(!Directory.Exists(ClusterConfiguration.NetworkShare))
            {
                PrintHelper.Red("Network share in config file doesnot exist");
                Environment.Exit(1);
            }
            DirectoryInfo nS = new DirectoryInfo(ClusterConfiguration.NetworkShare);
            foreach(var file in nS.GetFiles())
            {
                file.Delete();
            }

            PrintHelper.Green("Deleted all the old files in network share : " + ClusterConfiguration.NetworkShare);

            //copy the xml file
            File.Copy(CommandLineOptions.pathToClusterConfig, ClusterConfiguration.NetworkShare + "\\" + Path.GetFileName(CommandLineOptions.pathToClusterConfig));
            //copy all the pbinaries
            nS = new DirectoryInfo(CommandLineOptions.pathToPBinaries);
            foreach (var file in nS.GetFiles())
            {
                file.CopyTo(ClusterConfiguration.NetworkShare + "\\" + file.Name, true);
            }
            //copy all the service binaries
            nS = new DirectoryInfo(CommandLineOptions.pathToNewServiceFolder);
            foreach (var file in nS.GetFiles())
            {
                file.CopyTo(ClusterConfiguration.NetworkShare + "\\" + file.Name, true);
            }

            #endregion

            #region Start NodeManager 
            //start node manager at all the nodes except the main

            foreach(var node in ClusterConfiguration.AllNodes)
            {
                if(node.Value.Equals(ClusterConfiguration.MainMachineNode))
                {
                    continue;
                }
                else
                {
                    PrintHelper.Green("Started NodeManager on " + node.Value);
                    //start the NodeManager in listening mode.
                    //Nodemanager.exe nodeId 0
                    string psExec = CommandLineOptions.pathToPstools + "\\psexec.exe";
                    string quotedArgs = "";
                    ProcessStartInfo startInfo = new ProcessStartInfo(psExec, quotedArgs);
                    Process proc = new Process();
                    proc.StartInfo = startInfo;

                    proc.Start();
                    int timeoutInMilliSecs = 5 * 60 * 1000; // 5 minutes
                    bool didExitbeforeTimeout = proc.WaitForExit(timeoutInMilliSecs);
                    int errorCode = proc.ExitCode;
                    proc.Close();
                }
            }

            //Start the main machine
            PrintHelper.Green("Started NodeManager and Main machine on " + ClusterConfiguration.MainMachineNode);
            //start the NodeManager with main
            //NodeManager.exe nodeId 1

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
