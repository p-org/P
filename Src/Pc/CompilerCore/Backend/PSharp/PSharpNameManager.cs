using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend.PSharp
{
    internal class PSharpNameManager : NameManagerBase
    {
        public PSharpNameManager(string namePrefix) : base(namePrefix)
        {
        }

        private readonly Dictionary<PLanguageType, string> typeNames = new Dictionary<PLanguageType, string>();

        public string GetTypeName(PLanguageType type)
        {
            type = type.Canonicalize();
            if (typeNames.TryGetValue(type, out string name))
            {
                return name;
            }

            // TODO: generate "nicer" names for generated types.
            name = UniquifyName(type.TypeKind.Name);
            typeNames[type] = name;
            return name;
        }

        public IEnumerable<PLanguageType> UsedTypes => typeNames.Keys;

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
                        name = "PHalt";
                    }

                    return name;
                case Interface _:
                    return "I_" + name;
            }

            name = string.IsNullOrEmpty(name) ? "Anon" : name;
            if (name.StartsWith("$"))
            {
                name = "TMP_" + name.Substring(1);
            }

            return UniquifyName(name);
        }
    }
}
