using System;
using System.Linq;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Java
{
    /// <summary>
    /// The TypeManager is responsible for mechanisms related to extracting to Java-specific
    /// datatypes (including boxing, if desired.)
    /// </summary>
    internal class TypeManager
    {
        internal NameManager Names { get;  }

        public TypeManager(NameManager names)
        {
            Names = names;
        }

        internal class JType
        {
            private string _unboxedType;
            private string _refType;

            /// <summary>
            /// The name of the Java class that corresponds to this type.
            /// </summary>
            internal string TypeName
            {
                get
                {
                    if (_unboxedType == null)
                    {
                        throw new Exception($"TypeName not implemented for {GetType()}");
                    }

                    return _unboxedType;
                }
            }

            /// <summary>
            /// The name of the Java class that corresponds to this type, should it be treated.
            /// as a reference type.  Objects are automatically reference types so for them
            /// this is the same as `TypeName`, but for primitive types this returns the boxed
            /// type name instead.
            /// </summary>
            internal string ReferenceTypeName
            {
                get
                {
                    if (_refType == null)
                    {
                        return TypeName;
                    }
                    return _refType;
                }
            }

            /// <summary>
            /// Whether this can be represented as a primitive (or potentially-boxed) Java type
            /// or if it is strictly a reference type.
            /// </summary>
            internal virtual bool IsPrimitive => true;

            /// <summary>
            /// A default value for a given type, used for initializing fields and local variables.
            /// </summary>
            internal virtual string DefaultValue => $"new {ReferenceTypeName}()";

            /// <summary>
            /// The name of the method (K) -> V that returns the element V keyed on K.
            /// Throws for non-collection types!!
            /// </summary>
            internal virtual string AccessorMethodName =>
                throw new Exception($"AccessorMethodName not implemented for {TypeName}");

            /// <summary>
            /// The name of the method (K) -> bool that returns whether K is contained in an
            /// instance of this type.  Throws for non-collection types!!
            /// </summary>
            internal virtual string ContainsMethodName =>
                throw new Exception($"ContainsMethodName not implemented for {TypeName}");

            /// <summary>
            /// The name of the method (K, V) -> void that inserts V by key K.
            /// Throws for non-collection types!
            /// </summary>
            internal virtual string MutatorMethodName =>
                throw new Exception($"MutatorMethodName not implemented for {TypeName}");

            /// <summary>
            /// The name of the method K -> void that removes key K from the collection.  Throws for
            /// non-collection types!
            /// </summary>
            internal virtual string RemoveMethodName =>
                throw new Exception($"RemoveMethodName not implemented for {TypeName}");

            internal class JAny : JType
            {
                /// A JAny can either be a PEvent, a PValue, or a collection type like ArrayList, HashSet, etc.
                /// For a complete list, see the implementation of `deepEquals()` and `deepClone` in the P java runtime:
                internal JAny()
                {
                    _unboxedType = "Object";
                }

                internal override bool IsPrimitive => false;

                internal override string DefaultValue => "null";
            }

            internal class JBool : JType
            {
                internal JBool()
                {
                    _unboxedType = "boolean";
                    _refType = "Boolean";
                }

                internal override string DefaultValue => ToJavaLiteral(false);

                internal static string ToJavaLiteral(bool b)
                {
                    return b ? "true" : "false";
                }
            }

            internal class JInt : JType
            {
                internal JInt()
                {
                    _unboxedType = "long";
                    _refType = "Long";
                }

                internal override string DefaultValue => ToJavaLiteral(0L);

                internal static string ToJavaLiteral(long l)
                {
                    return l + "L";
                }
            }

            internal class JFloat : JType
            {
                internal JFloat()
                {
                    _unboxedType = "float";
                    _refType = "Float";
                }
                internal override string DefaultValue => ToJavaLiteral(0.0);

                internal static string ToJavaLiteral(double d)
                {
                    return d + "f";
                }
            }

            internal class JString : JType
            {
                internal JString()
                {
                    _unboxedType = "String";
                }

                internal override string DefaultValue => ToJavaLiteral("");
                internal override bool IsPrimitive => false;

                internal static string ToJavaLiteral(string s)
                {
                    return "\"" + s + "\"";
                }
            }

            internal class JMachine : JType
            {
                // Source/CheckerCore/Actors/ActorId.cs stores ActorID values as ulongs

                internal JMachine()
                {
                    _unboxedType = "long";
                    _refType = "Long";
                }
                internal override string DefaultValue => ToJavaLiteral(0L);
                internal static string ToJavaLiteral(long l)
                {
                    return l + "L";
                }
            }

            internal class JList : JType
            {
                internal JList(JType t)
                {
                    _unboxedType = $"ArrayList<{t.ReferenceTypeName}>";
                }

                internal override bool IsPrimitive => false;
                internal override string AccessorMethodName => "get";
                internal override string ContainsMethodName => "contains";
                internal override string MutatorMethodName => "set";
                internal override string RemoveMethodName => "remove";
            }
            internal class JMap : JType
            {
                private readonly JType _k, _v;

                internal JMap(JType k, JType v)
                {
                    _k = k;
                    _v = v;
                    _unboxedType = $"HashMap<{_k.ReferenceTypeName}, {_v.ReferenceTypeName}>";
                }

                internal override bool IsPrimitive => false;
                internal override string AccessorMethodName => "get";
                internal override string ContainsMethodName => "containsKey";
                internal override string MutatorMethodName => "put";
                internal override string RemoveMethodName => "remove";

                /// <summary>
                /// The name of the () -> Set method that produces the keys in the map.
                /// </summary>
                internal string KeysMethodName => "keySet";

                /// <summary>
                /// The type of a collection containing the keys of this Map.
                /// </summary>
                internal string KeyCollectionType => $"ArrayList<{_k.ReferenceTypeName}>";

                /// <summary>
                /// The type of a collection containing the keys of this Map.
                /// </summary>
                internal string ValueCollectionType => $"ArrayList<{_v.ReferenceTypeName}>";
            }

            internal class JSet : JType
            {
                internal JSet(JType t)
                {
                    _unboxedType = $"LinkedHashSet<{t.ReferenceTypeName}>";
                }

                internal override bool IsPrimitive => false;

                // Note: There's no AccessorMethodName for a JSet because, unfortunately,
                // we have to build a bit more mechanism in order to "index" into a
                // LinkedHashSet that the C# set datatype gives us directly.  For this,
                // the code generator emits a call to setElementAt(LinkedHashSet, int) in
                // the Java PRT runtime.

                internal override string ContainsMethodName => "contains";
                internal override string MutatorMethodName => "add";
                internal override string RemoveMethodName => "remove";
            }

            internal class JNamedTuple : JType
            {
                internal JNamedTuple(string jClassName)
                {
                    _unboxedType = $"{Constants.TypesNamespaceName}.{jClassName}";
                }

                internal override bool IsPrimitive => false;
            }

            internal class JForeign : JType
            {
                internal JForeign(string clazz)
                {
                    _unboxedType = $"{clazz}";
                }

                internal override bool IsPrimitive => false;

                internal override string DefaultValue => "null";
            }

            internal class JEnum : JType
            {
                private readonly string _defaultValue;

                internal JEnum(EnumType e)
                {
                    _unboxedType = Constants.TypesNamespaceName + "." + e.CanonicalRepresentation;
                    _defaultValue = e.EnumDecl.Values.First().Name; // An arbitrary enum value is fine.
                }


                internal override string DefaultValue => $"{_unboxedType}.{_defaultValue}";
            }

            //TODO: not sure about this one.  Is the base class sufficient?
            //Generate some Java files and see.
            internal class JEvent : JType
            {
                internal JEvent()
                {
                    _unboxedType = $"{Constants.PEventsClass}<?>";
                }
                internal override bool IsPrimitive => false;
            }

            internal class JVoid : JType
            {
                internal override string DefaultValue => "null";
                // XXX: This is slightly hacky in that `void` can be a return type but
                // not a variable type, and `Void` can be a variable type and a return
                // type but a valueless-return statement doesn't "autobox" into a Void.
                public JVoid()
                {
                    _refType = "Void";
                    _unboxedType = "void";
                }
            }
        }

        /// <summary>
        /// Produces the Java type that corresponds to the location (variable or data structure)
        /// represented by `e`.  Note that this isn't the same thing as the type that accessing
        /// that structure would produce (as that can be pulled out from the expr's fields).
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        internal JType JavaTypeForVarLocation(IPExpr e)
        {
            switch (e)
            {

                case MapAccessExpr mapAccessExpr:
                {
                    var k = JavaTypeFor(mapAccessExpr.IndexExpr.Type);
                    var v = JavaTypeFor(mapAccessExpr.Type);
                    return new JType.JMap(k, v);
                }

                case NamedTupleAccessExpr namedTupleAccessExpr:
                    return JavaTypeFor(namedTupleAccessExpr.Type);

                case SetAccessExpr setAccessExpr:
                {
                    var t = JavaTypeFor(setAccessExpr.SetExpr.Type);
                    return new JType.JSet(t);
                }

                case SeqAccessExpr seqAccessExpr:
                {
                    var t = JavaTypeFor(seqAccessExpr.SeqExpr.Type);
                    return new JType.JList(t);
                }

                case TupleAccessExpr tupleAccessExpr:
                    return JavaTypeFor(tupleAccessExpr.Type);

                case VariableAccessExpr variableAccessExpr:
                    return JavaTypeFor(e.Type);

                default:
                    throw new Exception($"Unknown location of {e}");
            }
        }

        /// <summary>
        /// Produces the Java type used to represent a value of type `type`.
        /// </summary>
        /// <param name="type">The P type.</param>
        /// <returns>The Java type.</returns>
        /// TODO: Make this private and stick a weak ref cache in front of it.
        internal JType JavaTypeFor(PLanguageType type)
        {
            switch (type.Canonicalize())
            {
                case DataType _:
                    throw new NotImplementedException($"{type.CanonicalRepresentation} values not implemented");

                case EnumType e:
                    return new JType.JEnum(e);

                case ForeignType ft:
                    return new JType.JForeign(ft.CanonicalRepresentation);

                case MapType m:
                {
                    var k = JavaTypeFor(m.KeyType);
                    var v = JavaTypeFor(m.ValueType);
                    return new JType.JMap(k, v);
                }

                case NamedTupleType nt:
                    return new JType.JNamedTuple(Names.NameForNamedTuple(nt));

                case PermissionType _:
                    return new JType.JMachine();

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Any):
                    return new JType.JAny();

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return new JType.JBool();

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return new JType.JInt();

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return new JType.JFloat();

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.String):
                    return new JType.JString();

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Event):
                    return new JType.JEvent();

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Machine):
                    return new JType.JMachine();

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Null):
                    return new JType.JVoid();

                case SequenceType s:
                    return new JType.JList(JavaTypeFor(s.ElementType));

                case SetType s:
                    return new JType.JSet(JavaTypeFor(s.ElementType));

                case TupleType t:
                    return JavaTypeFor(t.ToNamedTuple());

                default:
                    throw new NotImplementedException($"{type} values not implemented");
            }
        }
    }

        public static class BinOpExtensions
        {
            /// <summary>
            /// Produces the binary operator that should be used for a binary operation between
            /// two Java primitive values.
            /// </summary>
            /// <param name="op"></param>
            /// <returns></returns>
            public static string JavaPrimitiveBinOp(this BinOpType op)
            {
                switch (op)
                {
                    // Comparison operators
                    case BinOpType.Lt:
                        return "<";
                    case BinOpType.Le:
                        return "<=";
                    case BinOpType.Ge:
                        return ">=";
                    case BinOpType.Gt:
                        return ">";

                    // Equality operators
                    case BinOpType.Neq:
                        return "!=";
                    case BinOpType.Eq:
                        return "==";

                    // Arithmetic operators
                    case BinOpType.Add:
                        return "+";
                    case BinOpType.Sub:
                        return "-";
                    case BinOpType.Mul:
                        return "*";
                    case BinOpType.Div:
                        return "/";
                    case BinOpType.Mod:
                        return "%";

                    // Boolean operators:
                    case BinOpType.And:
                        return "&&";
                    case BinOpType.Or:
                        return "||";

                    // This should be dead code.
                    default:
                        throw new NotImplementedException(op.ToString());
                }
            }
        }
}
