using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.C
{
    public class CNameManager : NameManagerBase
    {
        private static readonly Dictionary<Type, string> DeclNameParts = new Dictionary<Type, string>
        {
            {typeof(EnumElem), "ENUMELEM"},
            {typeof(Function), "FUNCTION"},
            {typeof(Implementation), "IMPL"},
            {typeof(Interface), "I"},
            {typeof(Machine), "MACHINE"},
            {typeof(NamedEventSet), "EVENTSET"},
            {typeof(NamedModule), "MODULE"},
            {typeof(PEnum), "ENUM"},
            {typeof(PEvent), "EVENT"},
            {typeof(RefinementTest), "REFINEMENT_TEST"},
            {typeof(SafetyTest), "SAFETY_TEST"},
            {typeof(State), "STATE"},
            {typeof(TypeDef), "TYPEDEF"},
            {typeof(Variable), "VAR"}
        };

        private readonly ConditionalWeakTable<ForeignType, string> foreignTypeDeclNames =
            new ConditionalWeakTable<ForeignType, string>();

        private readonly ConditionalWeakTable<Function, string>
            funcNames = new ConditionalWeakTable<Function, string>();

        private readonly ConditionalWeakTable<Function, string>
            retLabels = new ConditionalWeakTable<Function, string>();

        private readonly Dictionary<PLanguageType, string> typeNames = new Dictionary<PLanguageType, string>();

        public CNameManager(string namePrefix)
            : base(namePrefix)
        {
        }

        public IEnumerable<PLanguageType> UsedTypes => typeNames.Keys;

        public string GetReturnLabel(Function function, string hint = "p_return")
        {
            if (retLabels.TryGetValue(function, out var name))
            {
                return name;
            }

            name = UniquifyName(hint);
            retLabels.Add(function, name);
            return name;
        }

        public string GetNameForFunctionImpl(Function function)
        {
            if (funcNames.TryGetValue(function, out var name))
            {
                return name;
            }

            var namePrefix = NamePrefix;
            var methodName = function.IsAnon ? "Anon" : function.Name;
            var nameSuffix = "_IMPL";
            name = UniquifyName(namePrefix + methodName + nameSuffix);

            funcNames.Add(function, name);
            return name;
        }

        public string GetNameForType(PLanguageType type)
        {
            type = type.Canonicalize();

            if (typeNames.TryGetValue(type, out var name))
            {
                return name;
            }

            name = NamePrefix + "GEND_TYPE_" + SimplifiedRep(type);
            name = UniquifyName(name);
            typeNames.Add(type, name);
            return name;
        }

        public string GetNameForForeignTypeDecl(ForeignType foreignType)
        {
            if (foreignTypeDeclNames.TryGetValue(foreignType, out var name))
            {
                return name;
            }

            name = UniquifyName(NamePrefix + foreignType.CanonicalRepresentation);
            foreignTypeDeclNames.Add(foreignType, name);
            return name;
        }

        protected override string ComputeNameForDecl(IPDecl decl)
        {
            var enumName = "";
#pragma warning disable CCN0002 // Non exhaustive patterns in switch block
            switch (decl)
            {
                case EnumElem enumElem:
                    enumName = $"{enumElem.ParentEnum.Name}_";
                    break;

                case PEvent pEvent:
                    if (pEvent.IsNullEvent)
                    {
                        return "_P_EVENT_NULL_STRUCT";
                    }

                    if (pEvent.IsHaltEvent)
                    {
                        return "_P_EVENT_HALT_STRUCT";
                    }

                    break;

                case Implementation impl:
                    return $"P_GEND_IMPL_{impl.Name}";
            }
#pragma warning restore CCN0002 // Non exhaustive patterns in switch block

            if (DeclNameParts.TryGetValue(decl.GetType(), out var declTypePart))
            {
                declTypePart += "_";
            }
            else
            {
                declTypePart = "";
            }

            var name = decl.Name;
            if (decl is State state)
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
                name = NamePrefix + declTypePart + enumName + name;
            }

            return UniquifyName(name);
        }

        private string SimplifiedRep(PLanguageType type)
        {
            switch (type.Canonicalize())
            {
                case DataType _:
                    return "B";

                case EnumType _:
                    return "E";

                case ForeignType foreign:
                    return foreign.CanonicalRepresentation;

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

                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.String):
                    return "r";

                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.Any):
                    return "a";

                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.Null):
                    return "n";

                case PrimitiveType _:
                    throw new ArgumentException("unrecognized primitive type", nameof(type));

                case SequenceType sequenceType:
                    return $"S{SimplifiedRep(sequenceType.ElementType)}";

                case SetType setType:
                    return $"U{SimplifiedRep(setType.ElementType)}";

                case TupleType tupleType:
                    return $"T{tupleType.Types.Count}{string.Join("", tupleType.Types.Select(SimplifiedRep))}";
                
                case NamedTupleType namedTupleType:
                    return $"NT{namedTupleType.Types.Count}{string.Join("", namedTupleType.Types.Select(SimplifiedRep))}";

                case TypeDefType _:
                    throw new ArgumentException("typedefs should be impossible after canonicalization", nameof(type));
            }

            throw new ArgumentException("unrecognized type kind", nameof(type));
        }
    }
}