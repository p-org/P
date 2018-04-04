using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend
{
    public class NameManager
    {
        private readonly string namePrefix;
        private readonly Dictionary<string, int> nameUsages = new Dictionary<string, int>();
        private readonly ConditionalWeakTable<IPDecl, string> declNames = new ConditionalWeakTable<IPDecl, string>();
        private readonly ConditionalWeakTable<IPDecl, string> funcNames = new ConditionalWeakTable<IPDecl, string>();
        private readonly Dictionary<PLanguageType, string> typeNames = new Dictionary<PLanguageType, string>();

        public NameManager(string namePrefix)
        {
            this.namePrefix = namePrefix;
        }

        public ImmutableHashSet<PLanguageType> UsedTypes => typeNames.Keys.ToImmutableHashSet();

        public string GetNameForNode(IPDecl node, string prefix = "")
        {
            if (declNames.TryGetValue(node, out string name))
            {
                return name;
            }

            name = node.Name;
            if (node is State state)
            {
                name = state.QualifiedName;
            }
            name = string.IsNullOrEmpty(name) ? "Anon" : name;
            name = name.Replace('.', '_');

            if (name.StartsWith("$"))
            {
                name = "PTMP_" + name.Substring(1);
            }
            else
            {
                name = namePrefix + prefix + name;
            }

            name = AdjustName(name);
            declNames.Add(node, name);
            return name;
        }

        public string GetNameForFunctionImpl(Function function, string prefix = "")
        {
            if (funcNames.TryGetValue(function, out string name))
            {
                return name;
            }

            name = function.IsAnon ? "Anon" : function.Name;
            name = AdjustName(namePrefix + prefix + name + "_IMPL");
            funcNames.Add(function, name);
            return name;
        }

        public string GetTemporaryName(string baseName)
        {
            return AdjustName(namePrefix + baseName);
        }

        private string AdjustName(string baseName)
        {
            string name = baseName;
            while (nameUsages.TryGetValue(name, out int usages))
            {
                nameUsages[name] = usages + 1;
                name = $"{baseName}_{usages}";
            }

            nameUsages.Add(name, 1);
            return name;
        }

        public string GetNameForType(PLanguageType type)
        {
            if (typeNames.TryGetValue(type, out string name))
            {
                return name;
            }

            name = namePrefix + "GEND_TYPE_" + SimplifiedRep(type);
            name = AdjustName(name);
            typeNames.Add(type, name);
            return name;
        }

        private string SimplifiedRep(PLanguageType type)
        {
            switch (type.Canonicalize())
            {
                case BoundedType _:
                    return "B";
                case EnumType _:
                    return "E";
                case ForeignType _:
                    return "F";
                case MapType mapType:
                    return $"MK{SimplifiedRep(mapType.KeyType)}V{SimplifiedRep(mapType.ValueType)}";
                case PermissionType _:
                    return "R";
                case PrimitiveType primitiveType when primitiveType == PrimitiveType.Bool:
                    return "b";
                case PrimitiveType primitiveType when primitiveType == PrimitiveType.Int:
                    return "i";
                case PrimitiveType primitiveType when primitiveType == PrimitiveType.Float:
                    return "f";
                case PrimitiveType primitiveType when primitiveType == PrimitiveType.Event:
                    return "e";
                case PrimitiveType primitiveType when primitiveType == PrimitiveType.Machine:
                    return "m";
                case PrimitiveType primitiveType when primitiveType == PrimitiveType.Any:
                    return "a";
                case PrimitiveType primitiveType when primitiveType == PrimitiveType.Null:
                    return "n";
                case PrimitiveType _:
                    throw new ArgumentException("unrecognized primitive type", nameof(type));
                case SequenceType sequenceType:
                    return $"S{SimplifiedRep(sequenceType.ElementType)}";
                case TupleType tupleType:
                    return $"T{tupleType.Types.Count}{string.Join("", tupleType.Types.Select(SimplifiedRep))}";
                case TypeDefType _:
                    throw new ArgumentException("typedefs should be impossible after canonicalization", nameof(type));
            }
            throw new ArgumentException("unrecognized type kind", nameof(type));
        }
    }
}
