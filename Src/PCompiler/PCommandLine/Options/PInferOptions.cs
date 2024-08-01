using System;
using System.Linq;
using System.Collections.Generic;
using PChecker.IO.Debugging;
using Plang.Parser;
using Plang.PInfer;
using System.IO;
using System.Text.Json;

namespace Plang.Options
{
    internal sealed class PInferOptions
    {
        private readonly CommandLineArgumentParser Parser;
        
        internal PInferOptions()
        {
            Parser = new("p infer", "The P Specification Miner");
            var minerConfigGroup = Parser.GetOrCreateGroup("miner-config", "SpecMiner Configuration");
            var stOpt = minerConfigGroup.AddArgument("skip-trivial", "st", "Skip trivial guard-filter-term combinations.", typeof(bool));
            var ngOpt = minerConfigGroup.AddArgument("num-guard-predicates", "ng", "Number of atomic predicates to conjoin in the guard (default: 0)", typeof(int));
            var nfOpt = minerConfigGroup.AddArgument("num-filter-predicates", "nf", "Number of atomic predicates to conjoin in the filter (default: 0)", typeof(int));
            var arityOpt = minerConfigGroup.AddArgument("inv-arity", "ia", "Arity (# terms involved) of invariants to mine (default: 2)", typeof(int));
            var quantifiersOpt = minerConfigGroup.AddArgument("forall-quantifiers", "nforall", "Number of preceding `forall` quantifiers. Default: # of events quantified (i.e. all events are forall-quantified)", typeof(int));
            var mustIncludeGuardsOpt = minerConfigGroup.AddArgument("hint-guards", "hg", "Hint guards to include in the specification (default: none)");
            var mustIncludeFiltersOpt = minerConfigGroup.AddArgument("hint-filters", "hf", "Hint filters to include in the specification (default: none)");
            var verboseMode = minerConfigGroup.AddArgument("verbose", "verbose", "Verbose mode, print stderr of Daikon (default: false)");
            var tracesOpt = minerConfigGroup.AddArgument("traces", "t", "Path to the trace files");
            stOpt.IsRequired = false;
            ngOpt.IsRequired = false;
            nfOpt.IsRequired = false;
            arityOpt.IsRequired = false;
            verboseMode.IsRequired = false;
            quantifiersOpt.IsRequired = false;
            mustIncludeGuardsOpt.IsMultiValue = true;
            mustIncludeFiltersOpt.IsMultiValue = true;
            mustIncludeFiltersOpt.IsRequired = false;
            mustIncludeGuardsOpt.IsRequired = false;
            tracesOpt.IsRequired = true;
            tracesOpt.IsMultiValue = true;
            
            var minerActionsGroup = Parser.GetOrCreateGroup("miner-actions", "Commands to list predicates and terms");
            minerActionsGroup.AddArgument("list-predicates", "lp", "List all atomic predicates that can be included in the mined specification");
            minerActionsGroup.AddArgument("list-terms", "lt", "List all terms that can be included in the mined specification");
        }
        
        internal PInferConfiguration Parse(string[] args)
        {
            PInferConfiguration pinferConfig = new();
            List<CommandLineArgument> fetch = [];
            PCompilerOptions.FindLocalPProject(fetch);
            var projectFile = (string)fetch[0].Value;
            if (!CheckFileValidity.IsLegalPProjFile(projectFile, out var projectFilePath))
            {
                throw new CommandlineParsingError($"Illegal P project file name {projectFile} or file {projectFilePath?.FullName} not found");
            }

            pinferConfig.OutputDirectory = ParsePProjectFile.GetOutputDirectory(projectFilePath).ToString();
            pinferConfig.ProjectName = ParsePProjectFile.GetProjectName(projectFilePath);
            try
            {
                var result = Parser.ParseArguments(args);
                foreach (var arg in result)
                {
                    UpdateConfiguration(arg, pinferConfig);
                }
            }
            catch (CommandLineException ex)
            {
                if ((from arg in ex.Result where arg.LongName == "list-predicates" select arg).Any())
                {
                    WritePredicates(pinferConfig);
                    Environment.Exit(1);
                }
                else if ((from arg in ex.Result where arg.LongName == "list-terms" select arg).Any())
                {
                    WriteTerms(pinferConfig);
                    Environment.Exit(1);
                }
                else
                {
                    Parser.PrintHelp(Console.Out);
                    Error.ReportAndExit(ex.Message);
                }
            }
            catch (Exception ex)
            {
                Parser.PrintHelp(Console.Out);
                Error.ReportAndExit(ex.Message);
            }
            return pinferConfig;
        }

        private static void UpdateConfiguration(CommandLineArgument arg, PInferConfiguration config)
        {
            switch (arg.LongName)
            {
                case "skip-trivial":
                    config.SkipTrivialCombinations = (bool) arg.Value;
                    break;
                case "num-guard-predicates":
                    config.NumGuardPredicates = (int) arg.Value;
                    break;
                case "num-filter-predicates":
                    config.NumFilterPredicates = (int) arg.Value;
                    break;
                case "inv-arity":
                    config.InvArity = (int) arg.Value;
                    break;
                case "forall-quantifiers":
                    config.NumForallQuantifiers =  (int) arg.Value;
                    break;
                case "hint-guards":
                    config.MustIncludeGuard = [.. ((string[])arg.Value).Select(int.Parse)];
                    break;
                case "hint-filters":
                    config.MustIncludeFilter = [.. ((string[])arg.Value).Select(int.Parse)];
                    break;
                case "traces":
                    config.TracePaths = (string[]) arg.Value;
                    break;
                case "verbose":
                    config.Verbose = true;
                    break;
            }
        }

        private static void WritePredicates(PInferConfiguration config)
        {
            var filePath = Path.Combine(config.OutputDirectory, "PInfer", config.ProjectName + ".predicates.json");
            using StreamReader r = new(filePath);
            string json = r.ReadToEnd();
            var predicates = JsonSerializer.Deserialize<List<AtomicPredicates>>(json);
            List<(int, string, string)> extractInfo = predicates.Select(x => (x.Order, x.Repr.Split("=>")[0].Trim(), x.Repr.Split("where")[1].Trim())).ToList();
            int reprLength = 0;
            int eventLength = 0;
            foreach (var pi in extractInfo)
            {
                reprLength = Math.Max(reprLength, pi.Item2.Length);
                eventLength = Math.Max(eventLength, pi.Item3.Length);
            }
            var formatStr = $"| {{0, -5}} | {{1, -{reprLength + 1}}} | {{2, -{eventLength + 1}}} |";
            Console.WriteLine("Available Atomic Predicates:");
            Console.WriteLine(string.Format(formatStr, "Id", "Repr", "Bounded Events"));
            foreach (var (order, repr, events) in extractInfo)
            {
                Console.WriteLine(string.Format(formatStr, order, repr, events));
            }
        }

        private static void WriteTerms(PInferConfiguration config)
        {
            var filePath = Path.Combine(config.OutputDirectory, "PInfer", config.ProjectName + ".terms.json");
            using StreamReader r = new(filePath);
            string json = r.ReadToEnd();
            var terms = JsonSerializer.Deserialize<List<Terms>>(json);
            List<(string, string, string)> extractInfo = terms.Select(x => (x.Repr.Split("=>")[0].Trim(), x.TypeStr, x.Repr.Split("where")[1].Trim())).ToList();
            int reprLength = 0;
            int typeLength = 0;
            int eventLength = 0;
            foreach (var pi in extractInfo)
            {
                reprLength = Math.Max(reprLength, pi.Item1.Length);
                typeLength = Math.Max(typeLength, pi.Item2.Length);
                eventLength = Math.Max(eventLength, pi.Item3.Length);
            }
            Console.WriteLine("Available Terms:");
            var formatStr = $"| {{0, -5}} | {{1, -{reprLength + 3}}} | {{2, -{typeLength + 3}}} | {{3, -{eventLength + 3}}} |";
            Console.WriteLine(string.Format(formatStr, "Id", "Repr", "Type", "Bounded Events"));
            foreach (var ((repr, ty, eventTypes), id) in extractInfo.Select((x, i) => (x, i)))
            {
                Console.WriteLine(string.Format(formatStr, id, repr, ty, eventTypes));
            }
        }
    }

    internal sealed class Terms
    {
        [System.Text.Json.Serialization.JsonPropertyName("repr")]
        public string Repr { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("events")]
        public IEnumerable<int> Events { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string TypeStr { get; set; }
    }

    internal sealed class AtomicPredicates
    {
        [System.Text.Json.Serialization.JsonPropertyName("order")]
        public int Order { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("repr")]
        public string Repr { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("terms")]
        public IEnumerable<int> Terms { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("contradictions")]
        public IEnumerable<int> Contradictions { get; set; }
    }
}