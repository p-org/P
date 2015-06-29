using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PrtDistDeployer
{
    public class CommandLineOptions
    {
        public static string pathToClusterConfig = ".\\ClusterConfiguration.xml";
        public static bool debugLocally = true;

        static void PrintArguments()
        {
            PrintHelper.Red("Please provide full path to the config file");
        }
        public static void ParseCommandline(string[] args)
        {
            if(args.Count() != 1)
            {
                PrintArguments();
                Environment.Exit(1);
            }
            else
            {
                pathToClusterConfig = args[0];
                if (!File.Exists(pathToClusterConfig))
                {
                    PrintArguments();
                    PrintHelper.Red("Failed to find the config file : " + pathToClusterConfig);
                    Environment.Exit(1);
                }
                            
            }

        }
    }
}
