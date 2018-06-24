using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend.Prt
{

    public class NameManager : NameManagerBase
    {
        private readonly ConditionalWeakTable<IPDecl, string> funcNames = new ConditionalWeakTable<IPDecl, string>();
        private readonly Dictionary<PLanguageType, string> typeNames = new Dictionary<PLanguageType, string>();

        public NameManager(string namePrefix)
            : base(namePrefix)
        {
        }

        public ImmutableHashSet<PLanguageType> UsedTypes => typeNames.Keys.ToImmutableHashSet();
        
        public string GetNameForFunctionImpl(Function function, string prefix = "")
        {
            if (funcNames.TryGetValue(function, out string name))
            {
                return name;
            }

            name = function.IsAnon ? "Anon" : function.Name;
            name = AdjustName(NamePrefix + prefix + name + "_IMPL");
            funcNames.Add(function, name);
            return name;
        }

        public string GetNameForType(PLanguageType type)
        {
            type = type.Canonicalize();

            if (typeNames.TryGetValue(type, out string name))
            {
                return name;
            }

            name = NamePrefix + "GEND_TYPE_" + SimplifiedRep(type);
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
                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.Bool):
                    return "b";
                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.Int):
                    return "i";
                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.Float):
                    return "f";
                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.Event):
                    return "e";
                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.Machine):
                    return "m";
                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.Any):
                    return "a";
                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.Null):
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
