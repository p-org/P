using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;
using System.Collections.Generic;

namespace Plang.Compiler.Backend.Coyote
{
    internal class CoyoteNameManager : NameManagerBase
    {
        private readonly Dictionary<PLanguageType, string> typeNames = new Dictionary<PLanguageType, string>();

        public CoyoteNameManager(string namePrefix) : base(namePrefix)
        {
        }

        public IEnumerable<PLanguageType> UsedTypes => typeNames.Keys;

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

        protected override string ComputeNameForDecl(IPDecl decl)
        {
            string name = decl.Name;

            //Handle null and halt events separately
            switch (decl)
            {
                case PEvent pEvent:
                    if (pEvent.IsNullEvent)
                    {
                        name = "DefaultEvent";
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