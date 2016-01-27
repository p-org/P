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
        public bool profile;
        public bool analyzeOnly;
        public LivenessOption liveness;
        public string outputDir;
        public bool outputFormula;
        public bool erase;
        public bool shortFileNames;
        public bool printTypeInference;

        public CommandLineOptions()
        {
            this.profile = false;
            this.analyzeOnly = false;
            this.liveness = LivenessOption.None;
            this.outputDir = null;
            this.outputFormula = false;
            this.erase = true;
            this.shortFileNames = false;
            this.printTypeInference = false;
        }
    }
}
