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

        public  static string pathToPBinaries = ".\\";

        public static string pathToNewServiceFolder = ".\\";

        public static string pathToPstools = ".\\";

        public static bool debugLocally = true;

        static void PrintArguments()
        {
            PrintHelper.Red("Please Provide all the required commandline arguments");
            PrintHelper.Red("--------------------------");
            PrintHelper.Red("-config:<path_to_xml_config_file>");
            PrintHelper.Red("-pbin:<path_to_p_binaries>");
            PrintHelper.Red("-clientbin:<path_to_the_client_binaries>");
            PrintHelper.Red("-pstools:<path_to_folder_containing_psexec_and_pskill");
            PrintHelper.Red("-debuglocally:<true or false>");
            PrintHelper.Red("--------------------------");


        }
        public static void ParseCommandline(string[] args)
        {
            if(args.Count() != 5)
            {
                PrintArguments();
                Environment.Exit(1);
            }
            else
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];

                    if (arg[0] == '-' || arg[0] == '/')
                    {
                        string option = arg.TrimStart('/', '-').ToLower();
                        string param = string.Empty;

                        int sepIndex = option.IndexOf(':');

                        if (sepIndex > 0)
                        {
                            param = option.Substring(sepIndex + 1);
                            option = option.Substring(0, sepIndex);
                        }
                        else if (sepIndex == 0)
                        {
                            PrintHelper.Red("Error: " + arg);
                            PrintArguments();
                            Environment.Exit(1);
                        }

                        switch (option)
                        {
                            case "?":
                            case "h":
                                PrintArguments();
                                Environment.Exit(0);
                            break;
                            case "config":
                                if(File.Exists(param))
                                    pathToClusterConfig = param;
                                else
                                {
                                    PrintHelper.Red("File : " + param + " not found");
                                    Environment.Exit(1);
                                }
                                
                            break;
                            case "pbin":
                                if (Directory.Exists(param))
                                    pathToPBinaries = param;
                                else
                                {
                                    PrintHelper.Red("Directory : " + param + " not found");
                                    Environment.Exit(1);
                                }
                            break;
                            case "clientbin":
                                if (Directory.Exists(param))
                                    pathToNewServiceFolder = param;
                                else
                                {
                                    PrintHelper.Red("Directory : " + param + " not found");
                                    Environment.Exit(1);
                                }
                               
                            break;
                            case "pstools":
                                if(Directory.Exists(param))
                                {
                                    if(File.Exists(param + "\\PsExec.exe") && File.Exists(param + "\\pskill.exe"))
                                    {
                                        pathToPstools = param;
                                    }
                                    else
                                    {
                                        PrintHelper.Red("File : PsExec or PsKill not found");
                                        Environment.Exit(1);
                                    }
                                }
                                else
                                {
                                    PrintHelper.Red("Directory : " + param + " not found");
                                    Environment.Exit(1);
                                }
                                break;
                            case "debuglocally":
                                bool input; 
                                if (!bool.TryParse(param, out input))
                                {
                                    PrintHelper.Red("Invalid Parameter to debugLocally option");
                                    Environment.Exit(1);
                                }
                                else
                                {
                                    debugLocally = input;
                                    if(!debugLocally)
                                    {
                                        Console.WriteLine("Please enter the username and Password for remote execution");
                                        PrintHelper.Red("Username:");
                                        ClusterConfiguration.username = Console.ReadLine();
                                        PrintHelper.Red("Password:");
                                        ClusterConfiguration.password = Console.ReadLine();
                                    }
                                }
                                break;
                            default:
                            {
                                PrintArguments();
                                Environment.Exit(1);
                            }
                            break;
                        }
                    }
                }
                            
            }

        }
    }
}
