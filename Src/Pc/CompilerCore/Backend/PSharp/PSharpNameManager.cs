using Microsoft.Pc.TypeChecker.AST;

namespace Microsoft.Pc.Backend.PSharp
{
    internal class PSharpNameManager : NameManagerBase
    {
        public PSharpNameManager(string namePrefix) : base(namePrefix)
        {
        }

        protected override string ComputeNameForDecl(IPDecl decl)
        {
            string name = decl.Name;
            name = string.IsNullOrEmpty(name) ? "Anon" : name;
            if (name.StartsWith("$"))
            {
                name = "TMP_" + name.Substring(1);
            }

            return name;
        }
    }
}
