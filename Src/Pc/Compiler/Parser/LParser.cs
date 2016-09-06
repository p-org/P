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

    internal partial class LParser : ShiftReduceParser<LexValue, LexLocation>
    {
        public LParser()
            : base(null)
        {

        }
    }

    internal partial class DummyTokenParser : ShiftReduceParser<LexValue, LexLocation>
    {
        public DummyTokenParser()
            : base(null)
        {

        }
    }
}


