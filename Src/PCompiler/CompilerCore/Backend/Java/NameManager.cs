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


        /// <summary>
        /// Produces the name of the Java type used to represent a value of type `type`.
        /// TODO: Do we want the values boxed??
        /// </summary>
        /// <param name="type">The P type.</param>
        /// <param name="isVar"></param>
        /// <returns>The Java type's name.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If we're not implemented yet.</exception>
        internal string JavaTypeFor(PLanguageType type, bool isVar = false)
        {
            switch (type.Canonicalize())
            {
                case DataType _:
                    throw new NotImplementedException("Datatype values not implemented");

                case EnumType _:
                    return "Integer";

                case ForeignType _:
                    // return type.CanonicalRepresentation;
                    // TODO: The above might be wrong for .NET -> Java extraction!
                    throw new ArgumentOutOfRangeException(nameof(type));

                case MapType m:
                    return $"HashMap<{JavaTypeFor(m.KeyType)}, {JavaTypeFor(m.ValueType)}";

                case NamedTupleType _:
                    return "HashMap<String, Object>"; //TODO: do better than Object!

                case PermissionType _:
                    return "PMachineValue";

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Any):
                    return "Object";

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "Boolean";

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "Integer";

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "Double";

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.String):
                    return "String";

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Event):
                    return "PEvent";

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Machine):
                    throw new ArgumentOutOfRangeException(nameof(type)); // return "PMachineValue"

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Null):
                    return isVar ? "Object" : "void";

                case SequenceType s:
                    return $"ArrayList<{s.ElementType}>";

                case SetType s:
                    return $"HashSet<{s.ElementType}>";

                case TupleType _:
                    // TODO: maybe we can hack it as a Record??  A bummer that we don't have Scala's Tuple[A,B,C,...].
                    return "List<Object>";

                default:
                    throw new ArgumentOutOfRangeException("foo");
            }
        }
    }
}