using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace P.Tester
{
    public class PTesterCommandLine
    {
        /*
        public static bool ParseCommandLine(string[] args)
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
                        PrintHelp(arg, "Malformed option");
                        return false;
                    }

                    switch (option)
                    {
                        case "?":
                        case "h":
                            {
                                PrintHelp(null, null);
                                Environment.Exit((int)ZingerResult.Success);
                                break;
                            }
                        case "fbound":
                            ZingerConfiguration.zBoundedSearch.FinalExecutionCutOff = int.Parse(param);
                            break;

                        case "ibound":
                            ZingerConfiguration.zBoundedSearch.IterativeIncrement = int.Parse(param);
                            break;

                        case "p":
                            if (param.Length == 0)
                            {
                                ZingerConfiguration.DegreeOfParallelism = Environment.ProcessorCount;
                            }
                            else
                            {
                                ZingerConfiguration.DegreeOfParallelism = int.Parse(param);
                            }
                            break;

                        case "m":
                        case "multiple":
                            ZingerConfiguration.StopOnError = false;
                            break;

                        case "s":
                        case "stats":
                            ZingerConfiguration.PrintStats = true;
                            break;

                        case "et":
                            ZingerConfiguration.EnableTrace = true;
                            ZingerConfiguration.traceLogFile = param;
                            break;

                        case "entirezingtrace":
                            ZingerConfiguration.DetailedZingTrace = true;
                            break;

                        case "ct":
                            ZingerConfiguration.CompactTraces = true;
                            break;

                        case "frontiertodisk":
                            ZingerConfiguration.FrontierToDisk = true;
                            break;

                        case "co":
                            ZingerConfiguration.NonChooseProbability = double.Parse(param);
                            break;

                        case "maxmemory":
                            ZingerConfiguration.MaxMemoryConsumption = double.Parse(param);
                            break;

                        case "maxdfsstack":
                            ZingerConfiguration.BoundDFSStackLength = int.Parse(param);
                            break;

                        case "pb":
                            ZingerConfiguration.DoPreemptionBounding = true;
                            break;

                        case "randomsample":
                            {
                                if (param.Length != 0)
                                {
                                    string pattern = @"(\d+,\d+)";
                                    Match result = Regex.Match(param, pattern);
                                    if (result.Success)
                                    {
                                        ZingerConfiguration.MaxSchedulesPerIteration = int.Parse(result.Value.Split(',').ElementAt(0));
                                        ZingerConfiguration.MaxDepthPerSchedule = int.Parse(result.Value.Split(',').ElementAt(1));
                                    }
                                    else
                                    {
                                        PrintHelp(option, String.Format("Invalid parameter passed with randomsample, expecting (int,int)"));
                                        return false;
                                    }
                                }
                                ZingerConfiguration.DoRandomSampling = true;
                            }
                            break;

                        case "depthb":
                            {
                                ZingerConfiguration.DoDelayBounding = false;
                                break;
                            }
                        case "sched":
                            {
                                try
                                {
                                    ZingerConfiguration.DoDelayBounding = true;
                                    if (!File.Exists(param))
                                    {
                                        PrintHelp(option, string.Format("File {0} not found", param));
                                        return false;
                                    }

                                    var schedAssembly = Assembly.LoadFrom(param);
                                    if (schedAssembly.GetTypes().Where(t => (t.BaseType.Name == "ZingerDelayingScheduler")).Count() != 1)
                                    {
                                        PTesterUtil.PrintErrorMessage(String.Format("Zing Scheduler {0}: Should have (only one) class inheriting the base class ZingerDelayingScheduler", Path.GetFileName(param)));
                                        return false;
                                    }

                                    if (schedAssembly.GetTypes().Where(t => (t.BaseType.Name == "ZingerSchedulerState")).Count() != 1)
                                    {
                                        PTesterUtil.PrintErrorMessage(String.Format("Zing Scheduler {0}: Should have (only one) class inheriting the base class IZingerSchedulerState", Path.GetFileName(param)));
                                        return false;
                                    }
                                    // get class name
                                    string schedClassName = schedAssembly.GetTypes().Where(t => (t.BaseType.Name == "ZingerDelayingScheduler")).First().FullName;
                                    var schedStateClassName = schedAssembly.GetTypes().Where(t => (t.BaseType.Name == "ZingerSchedulerState")).First().FullName;
                                    var schedClassType = schedAssembly.GetType(schedClassName);
                                    var schedStateClassType = schedAssembly.GetType(schedStateClassName);
                                    ZingerConfiguration.ZExternalScheduler.zDelaySched = Activator.CreateInstance(schedClassType) as ZingerDelayingScheduler;
                                    ZingerConfiguration.ZExternalScheduler.zSchedState = Activator.CreateInstance(schedStateClassType) as ZingerSchedulerState;
                                }
                                catch (Exception e)
                                {
                                    PTesterUtil.PrintErrorMessage(String.Format("Passed scheduler dll {0} implementing delaying scheduler is Invalid", Path.GetFileName(param)));
                                    PTesterUtil.PrintErrorMessage(e.Message);
                                    return false;
                                }
                            }
                            break;

                        case "timeout":
                            if (param.Length != 0)
                            {
                                ZingerConfiguration.Timeout = int.Parse(param);
                            }
                            break;

                        case "bc":
                            ZingerConfiguration.BoundChoices = true;
                            ZingerConfiguration.zBoundedSearch.FinalChoiceCutOff = int.Parse(param);
                            break;

                        case "ndfsliveness":
                            ZingerConfiguration.DoNDFSLiveness = true;
                            break;

                        case "maceliveness":
                            ZingerConfiguration.DoMaceliveness = true;
                            if (param.Length == 0)
                            {
                                //Use the default parameters
                                ZingerConfiguration.MaceLivenessConfiguration = new ZingerMaceLiveness();
                            }
                            else
                            {
                                var parameters = Regex.Match(param, "([0-9]*,[0-9]*,[0-9]*)").Groups[0].ToString();
                                var bounds = parameters.Split(',');
                                if (bounds.Count() != 3)
                                {
                                    PrintHelp(arg, "Invalid parameters passed to maceliveness");
                                    return false;
                                }
                                else
                                {
                                    ZingerConfiguration.MaceLivenessConfiguration = new ZingerMaceLiveness(int.Parse(bounds[0]), int.Parse(bounds[1]), int.Parse(bounds[2]));
                                }
                            }
                            break;

                        case "randomliveness":
                            ZingerConfiguration.DoMAPLiveness = true;
                            break;

                        case "plugin":
                            {
                                if (ZingerConfiguration.DronacharyaEnabled)
                                {
                                    PrintHelp(option, String.Format("Dronacharya and plugin cannot be enabled together"));
                                }
                                try
                                {
                                    //check if the file exists
                                    if (!File.Exists(param))
                                    {
                                        PrintHelp(option, String.Format("File {0} not found", param));
                                    }

                                    var pluginAssembly = Assembly.LoadFrom(param);
                                    if (pluginAssembly.GetTypes().Where(t => (t.BaseType.Name == "ZingerPluginInterface")).Count() != 1)
                                    {
                                        PTesterUtil.PrintErrorMessage(String.Format("Zing plugin {0}: Should have (only one) class inheriting the base class ZingerPluginInterface", param));
                                        return false;
                                    }

                                    if (pluginAssembly.GetTypes().Where(t => (t.BaseType.Name == "ZingerPluginState")).Count() != 1)
                                    {
                                        PTesterUtil.PrintErrorMessage(String.Format("Zing plugin {0}: Should have (only one) class inheriting the base class ZingerPluginState", param));
                                        return false;
                                    }
                                    // get class name
                                    string pluginClassName = pluginAssembly.GetTypes().Where(t => (t.BaseType.Name == "ZingerPluginInterface")).First().FullName;
                                    var pluginStateClassName = pluginAssembly.GetTypes().Where(t => (t.BaseType.Name == "ZingerPluginState")).First().FullName;
                                    var pluginClassType = pluginAssembly.GetType(pluginClassName);
                                    var pluginStateClassType = pluginAssembly.GetType(pluginStateClassName);
                                    ZingerConfiguration.ZPlugin = new ZingerExternalPlugin();
                                    ZingerConfiguration.ZPlugin.zPlugin = Activator.CreateInstance(pluginClassType) as ZingerPluginInterface;
                                    ZingerConfiguration.ZPlugin.zPluginState = Activator.CreateInstance(pluginStateClassType) as ZingerPluginState;

                                    ZingerConfiguration.IsPluginEnabled = true;
                                }
                                catch (Exception e)
                                {
                                    PTesterUtil.PrintErrorMessage(String.Format("Passed dll {0} implementing plugin is Invalid", Path.GetFileName(param)));
                                    PTesterUtil.PrintErrorMessage(e.Message);
                                    return false;
                                }
                            }
                            break;

                        case "dronacharya":
                            {
                                if (ZingerConfiguration.IsPluginEnabled)
                                {
                                    PrintHelp(option, String.Format("Dronacharya and plugin cannot be enabled together"));
                                }

                                ZingerConfiguration.DronacharyaEnabled = true;

                                ZingerConfiguration.ZDronacharya = new ZingDronacharya(param);

                                var pluginDll = ZingerConfiguration.ZDronacharya.DronaConfiguration.motionPlannerPluginPath;
                                try
                                {
                                    //check if the file exists
                                    if (!File.Exists(pluginDll))
                                    {
                                        PrintHelp(option, String.Format("File {0} not found", pluginDll));
                                    }

                                    var pluginAssembly = Assembly.LoadFrom(pluginDll);
                                    if (pluginAssembly.GetTypes().Where(t => (t.BaseType.Name == "ZingerPluginInterface")).Count() != 1)
                                    {
                                        PTesterUtil.PrintErrorMessage(String.Format("Zing plugin {0}: Should have (only one) class inheriting the base class ZingerPluginInterface", pluginDll));
                                        return false;
                                    }

                                    if (pluginAssembly.GetTypes().Where(t => (t.BaseType.Name == "ZingerPluginState")).Count() != 1)
                                    {
                                        PTesterUtil.PrintErrorMessage(String.Format("Zing plugin {0}: Should have (only one) class inheriting the base class ZingerPluginState", pluginDll));
                                        return false;
                                    }
                                    // get class name
                                    string pluginClassName = pluginAssembly.GetTypes().Where(t => (t.BaseType.Name == "ZingerPluginInterface")).First().FullName;
                                    var pluginStateClassName = pluginAssembly.GetTypes().Where(t => (t.BaseType.Name == "ZingerPluginState")).First().FullName;
                                    var pluginClassType = pluginAssembly.GetType(pluginClassName);
                                    var pluginStateClassType = pluginAssembly.GetType(pluginStateClassName);
                                    ZingerConfiguration.ZPlugin = new ZingerExternalPlugin();
                                    ZingerConfiguration.ZPlugin.zPlugin = Activator.CreateInstance(pluginClassType) as ZingerPluginInterface;
                                    ZingerConfiguration.ZPlugin.zPluginState = Activator.CreateInstance(pluginStateClassType) as ZingerPluginState;
                                }
                                catch (Exception e)
                                {
                                    PTesterUtil.PrintErrorMessage(String.Format("Passed dll {0} implementing plugin is Invalid", Path.GetFileName(param)));
                                    PTesterUtil.PrintErrorMessage(e.Message);
                                    return false;
                                }
                            }
                            break;

                        case "dronaworker":
                            ZingerConfiguration.IsDronaMain = false;
                            break;

                        default:
                            PrintHelp(arg, "Invalid Option");
                            return false;
                    }
                }
                else
                {
                    if (ZingerConfiguration.ZingModelFile != "")
                    {
                        PrintHelp(arg, "Only one Zing model may be referenced");
                        return false;
                    }

                    if (!File.Exists(arg))
                    {
                        PrintHelp(arg, "Can't find Zing Assembly");
                        return false;
                    }

                    ZingerConfiguration.ZingModelFile = arg;
                }
            }

            if (ZingerConfiguration.ZingModelFile == "")
            {
                PrintHelp(null, "No Zing Model Specified");
                return false;
            }
            return true;
        }
        */
        public static void PrintHelp(string arg, string errorMessage)
        {
            if (errorMessage != null)
            {
                if (arg != null)
                    PTesterUtil.PrintErrorMessage(String.Format("Error: \"{0}\" - {1}", arg, errorMessage));
                else
                    PTesterUtil.PrintErrorMessage(String.Format("Error: {0}", errorMessage));
            }

            Console.Write("HELP ME");
        }

        public static void Main()
        {

        }
    }
}
