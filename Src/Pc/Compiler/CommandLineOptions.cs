namespace Microsoft.Pc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Domains;
    using Microsoft.Formula.API;
    using Microsoft.Formula.API.Generators;
    using Microsoft.Formula.API.Nodes;

    public class CommandLineOptions
    {
        public bool profile { get; set; }
        public bool analyzeOnly { get; set; }
        public LivenessOption liveness { get; set; }
        public string outputDir { get; set; }
        public string outputFileName { get; set; }
        public bool outputFormula { get; set; }
        public bool test { get; set; }
        public bool shortFileNames { get; set; }
        public bool printTypeInference { get; set; }
        public bool noCOutput { get; set; }
        public bool noSourceInfo { get; set; }
        public string inputFileName { get; set; }
        public string pipeName { get; set; }

        public CommandLineOptions()
        {
        }
    }
}
