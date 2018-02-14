using System.Collections.Generic;
using Microsoft.Pc.TypeChecker;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc
{
    public class PProgramModel
    {
        public Scope GlobalScope { get; set; }
    }

    public class PSharpProgramModel : PProgramModel
    {
        public string Namespace { get; set; }
        public List<PLanguageType> AllTypes { get; set; }
    }
}
