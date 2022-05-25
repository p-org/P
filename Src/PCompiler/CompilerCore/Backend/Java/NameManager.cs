using System;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Java
{
    public class NameManager : NameManagerBase
    {
        public NameManager(string namePrefix) : base(namePrefix)
        {
        }


        protected override string ComputeNameForDecl(IPDecl decl)
        {
            string name;

            switch (decl)
            {
                case PEvent { IsNullEvent: true }:
                    return "DefaultEvent";
                    break;
                case PEvent { IsHaltEvent: true }:
                    return "PHalt";
                    break;
                case Interface i:
                    name = "I_" + i.Name;
                    break;
                default:
                    name = decl.Name;
                    break;
            }

            if (string.IsNullOrEmpty(name))
            {
                name = "Anon";
            }

            if (name.StartsWith("$"))
            {
                name = "TMP_" + name.Substring(1);
            }

            return UniquifyName(name);
        }


    }
}