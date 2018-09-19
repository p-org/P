using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;

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

            //Handle null and halt events separately
            switch (decl)
            {
                case PEvent pEvent:
                    if (pEvent.IsNullEvent)
                    {
                        name = "Default";
                    }

                    if (pEvent.IsHaltEvent)
                    {
                        name = "Halt";
                    }

                    break;
            }

            name = string.IsNullOrEmpty(name) ? "Anon" : name;
            if (name.StartsWith("$"))
            {
                name = "TMP_" + name.Substring(1);
            }

            return name;
        }
    }
}
