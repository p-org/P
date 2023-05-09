using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;
using System.Collections.Generic;
using System;
using Plang.Compiler.TypeChecker.AST.States;

namespace Plang.Compiler.Backend.CSharp
{
    internal class CSharpNameManager : NameManagerBase
    {
        private readonly Dictionary<PLanguageType, string> typeNames = new Dictionary<PLanguageType, string>();
        private readonly string[] reservedKeywords = new string[]
        {
            "bool", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "double", "float", "decimal",
            "string", "char", "void", "object", "typeof", "sizeof", "null", "true", "false", "if", "else", "while", "for", "foreach", "do", "switch",
            "case", "default", "lock", "try", "throw", "catch", "finally", "goto", "break", "continue", "return", "public", "private", "internal",
            "protected", "static", "readonly", "sealed", "const", "fixed", "stackalloc", "volatile", "new", "override", "abstract", "virtual",
            "event", "extern", "ref", "out", "in", "is", "as", "params", "__arglist", "__makeref", "__reftype", "__refvalue", "this", "base",
            "namespace", "using", "class", "struct", "interface", "enum", "delegate", "checked", "unchecked", "unsafe", "operator", "implicit", "explicit"
        };

        public CSharpNameManager(string namePrefix) : base(namePrefix)
        {
            Array.Sort(reservedKeywords);
        }

        public IEnumerable<PLanguageType> UsedTypes => typeNames.Keys;

        public string GetTypeName(PLanguageType type)
        {
            type = type.Canonicalize();
            if (typeNames.TryGetValue(type, out var name))
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
            var name = decl.Name;

            //Handle null and halt events separately
#pragma warning disable CCN0002 // Non exhaustive patterns in switch block
            switch (decl)
            {
                case PEvent pEvent:
                    if (pEvent.IsNullEvent)
                    {
                        return "DefaultEvent";
                    }
                    return pEvent.IsHaltEvent ? "PHalt" : name;


                case State pState:
                    return pState.Name;
                case Interface _:
                    return "I_" + name;
            }
#pragma warning restore CCN0002 // Non exhaustive patterns in switch block

            name = string.IsNullOrEmpty(name) ? "Anon" : name;
            if (name.StartsWith("$"))
            {
                name = string.Concat("TMP_", name.AsSpan(1));
            }
            else if (Array.BinarySearch(reservedKeywords, name) >= 0)
            {
                name = "P_" + name;
            }

            return UniquifyName(name);
        }
    }
}