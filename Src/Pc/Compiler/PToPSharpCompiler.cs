using System.Collections.Generic;
using Microsoft.Formula.API;
using Microsoft.Formula.API.Nodes;

namespace Microsoft.Pc
{
    internal class PToPSharpCompiler : PTranslation
    {
        public PToPSharpCompiler(
            Compiler compiler,
            AST<Model> model,
            Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo) : base(compiler, model, idToSourceInfo)
        {
            
        }

        public string GenerateCode()
        {
            return string.Empty;
        }
    }
}