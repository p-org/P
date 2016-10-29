namespace Microsoft.Pc.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using QUT.Gppg;

    using Domains;
    using Microsoft.Formula.API;
    using Microsoft.Pc;
    using Microsoft.Formula.API.Generators;
    using Microsoft.Formula.API.Nodes;

    public enum LProgramTopDecl { Module, Test };
    public class LProgramTopDeclNames
    {
        public HashSet<string> testNames;
        public HashSet<string> moduleNames;

        public LProgramTopDeclNames()
        {
            testNames = new HashSet<string>();
            moduleNames = new HashSet<string>();
        }

        public void Reset()
        {
            testNames.Clear();
            moduleNames.Clear();
        }
    }

    internal partial class LParser : ShiftReduceParser<LexValue, LexLocation>
    {
        private List<Flag> parseFlags;
        private LProgram parseLinker;
        private ProgramName parseSource;

        private bool parseFailed = false;

        private LProgramTopDeclNames LinkTopDeclNames;
        private List<PLink_Root.EventName> crntEventList = new List<PLink_Root.EventName>();

        public LParser()
            : base(null)
        {

        }
    }




    // Dummy function for Tokens.y
    internal partial class DummyTokenParser : ShiftReduceParser<LexValue, LexLocation>
    {
        public DummyTokenParser()
            : base(null)
        {

        }
    }
}


