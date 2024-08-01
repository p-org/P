using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Plang.PInfer
{
    public class PInferInvoke
    {
        public static void invokeMain(PInferConfiguration configuration)
        {
            ProcessStartInfo startInfo;
            Process process;
            var depsOpt = Path.Combine(GetPInferDependencies(), "*");
            var classpath = Path.Combine(configuration.OutputDirectory, "PInfer", "target", "classes");
            List<string> args = ["-cp",
                    string.Join(":", [depsOpt, classpath]),
                    $"{configuration.ProjectName}.pinfer.Main"];
            
            ShowConfig(configuration);
            startInfo = new ProcessStartInfo("java", args.Concat(GetMinerConfigArgs(configuration)))
            {
                UseShellExecute = true
            };
            process = Process.Start(startInfo);
            process.WaitForExit();
        }

        private static void ShowConfig(PInferConfiguration config)
        {
            Console.WriteLine("Mining specifications captured by the following search space:");
            Console.WriteLine($"\t#Forall preceding quantifiers:\t\t{config.NumForallQuantifiers}");
            Console.WriteLine($"\t#Predicates in Guard:\t\t\t{config.NumGuardPredicates}");
            Console.WriteLine($"\t#Predicates in Filter:\t\t\t{config.NumFilterPredicates}");
            Console.WriteLine($"\tArity of target specifications:\t\t{config.InvArity}");
            if (config.MustIncludeGuard.Count() > 0)
            {
                Console.WriteLine($"\tMust include guards:\t\t{string.Join(", ", config.MustIncludeGuard)}");
            }
            if (config.MustIncludeFilter.Count() > 0)
            {
                Console.WriteLine($"\tMust include filters(Id):\t\t{string.Join(", ", config.MustIncludeFilter)}");
            }
            Console.WriteLine($"\tSkip trivial combinations:\t\t{config.SkipTrivialCombinations}");
        }

        private static List<string> GetMinerConfigArgs(PInferConfiguration configuration)
        {
            var args = new List<string>();
            if (configuration.NumForallQuantifiers != -1)
            {
                args.Add("-nforall");
                args.Add($"{configuration.NumForallQuantifiers}");
                args.Add("-fd");
                args.Add($"{configuration.NumFilterPredicates}");
            }
            args.Add("-gd");
            args.Add($"{configuration.NumGuardPredicates}");
            if (configuration.SkipTrivialCombinations)
            {
                args.Add("-st");
            }
            args.Add("-p");
            args.Add(Path.Combine(configuration.OutputDirectory, "PInfer", $"{configuration.ProjectName}.predicates.json"));
            args.Add("-t");
            args.Add(Path.Combine(configuration.OutputDirectory, "PInfer", $"{configuration.ProjectName}.terms.json"));
            if (configuration.MustIncludeGuard.Count() > 0)
            {
                args.Add("-g");
                foreach (var g in configuration.MustIncludeGuard)
                {
                    args.Add($"{g}");
                }
            }
            if (configuration.MustIncludeGuard.Count() > 0)
            {
                args.Add("-f");
                foreach (var f in configuration.MustIncludeFilter)
                {
                    args.Add($"{f}");
                }
            }
            args.Add("-nt");
            args.Add($"{configuration.InvArity}");
            if (configuration.Verbose)
            {
                args.Add("-v");
            }
            args.Add("-l");
            foreach (var t in configuration.TracePaths)
            {
                args.Add($"{t}");
            }
            return args;
        }

        private static string GetPInferDependencies()
        {
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo info = new(currentDir);
            return Path.Combine(info.Parent.Parent.Parent.Parent.Parent.ToString(), "pinfer-dependencies");
        }
    }
}