using System;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Java
{
    /// <summary>
    /// The TypeManager is responsible for mechanisms related to extracting to Java-specific
    /// datatypes (including boxing, if desired.)
    /// </summary>
    internal class TypeManager
    {

        internal class JType
        {
            /// <summary>
            /// The name of the Java class that corresponds to this type.
            /// </summary>
            internal virtual string TypeName => "Object";

            /// <summary>
            /// The name of the Java class that corresponds to this type, should it be treated.
            /// as a reference type.  Objects are automatically reference types so for them
            /// this is the same as `TypeName`, but for primitive types this returns the boxed
            /// type name instead.
            /// </summary>
            internal virtual string ReferenceTypeName => TypeName;

            /// <summary>
            /// A default value for a given type, used for initializing fields and local variables.
            /// </summary>
            internal virtual string DefaultValue => $"new {ReferenceTypeName}()";

            /// <summary>
            /// The name of the method (K) -> V that returns the element V keyed on K.
            /// Returns null for non-collection types!!
            /// </summary>
            internal virtual string? AccessorMethodName => null;

            /// <summary>
            /// The name of the method (K) -> bool that returns whether K is contained in an
            /// instance of this type.  Returns null for non-collection types!!
            /// </summary>
            internal virtual string? ContainsMethodName => null;

            /// <summary>
            /// The name of the method (K, V) -> void that inserts V by key K.
            /// Returns null for non-collection types!
            /// </summary>
            internal virtual string? MutatorMethodName => null;
            
            internal class JBool : JType
            {
                internal override string TypeName => "boolean";
                internal override string ReferenceTypeName => "Boolean";
                internal override string DefaultValue => ToJavaLiteral(false);

                internal static string ToJavaLiteral(bool b)
                {
                    return b ? "true" : "false";
                }
            }
            
            internal class JInt : JType
            {
                internal override string TypeName => "int";
                internal override string ReferenceTypeName => "Integer";
                internal override string DefaultValue => ToJavaLiteral(0);
                
                internal static string ToJavaLiteral(int i)
                {
                    return i.ToString();
                }
            }
            
            internal class JFloat : JType
            {
                internal override string TypeName => "float";
                internal override string ReferenceTypeName => "Float";
                internal override string DefaultValue => ToJavaLiteral(0.0);
                
                internal static string ToJavaLiteral(double d)
                {
                    return d + "d";
                }
            }
            
            internal class JString : JType
            {
                internal override string TypeName => "String";
                internal override string DefaultValue => ToJavaLiteral("");
                
                internal static string ToJavaLiteral(string s)
                {
                    return "\"" + s + "\"";
                }
            }

            internal class JList : JType
            {
                private readonly JType _t;
                internal JList(JType t)
                {
                    _t = t;
                }

                internal override string TypeName => $"ArrayList<{_t.ReferenceTypeName}>";
                internal override string AccessorMethodName => "get";
                internal override string ContainsMethodName => "contains";
                internal override string MutatorMethodName => "set";
            }
            internal class JMap : JType
            {
                private readonly JType _k, _v;

                internal JMap(JType k, JType v)
                {
                    _k = k;
                    _v = v;
                }

                internal override string TypeName => 
                    $"Map<{_k.ReferenceTypeName},{_v.ReferenceTypeName}>";
                internal override string AccessorMethodName => "get";
                internal override string ContainsMethodName => "containsKey";
                internal override string MutatorMethodName => "set";
            }
            
            internal class JSet : JType
            {
                private readonly JType _t;

                internal JSet(JType t)
                {
                    _t = t;
                }

                internal override string TypeName => 
                    $"Set<{_t.ReferenceTypeName}>";
                
                internal override string AccessorMethodName => "contains";
                internal override string ContainsMethodName => "contains";
                internal override string MutatorMethodName => "add";
            }

            // TODO: maybe we can hack it as a Record??  A bummer that we don't have Scala's Tuple[A,B,C,...].
            internal class JNamedTuple : JType
            {
                internal override string TypeName => "HashMap<String, Object>";
                
                internal override string AccessorMethodName => "get";
                internal override string ContainsMethodName => "containsKey";
                internal override string MutatorMethodName => "set";
            }
           
            internal class JTuple : JType
            {
                internal override string TypeName => "ArrayList<Object>";
                
                internal override string AccessorMethodName => "get";
                internal override string ContainsMethodName => "contains";
                internal override string MutatorMethodName => "set";
            }
           
            //TODO: not sure about this one.  Is the base class sufficient?
            //Generate some Java files and see.
            internal class JEvent : JType
            {
                internal override string TypeName => "PEvent";
            }

            internal class JVoid : JType
            {
                internal override string DefaultValue => "null";
                internal override string TypeName => "Void";
                
            }
        }
        
        /// <summary>
        /// Produces the name of the Java type used to represent a value of type `type`.
        /// TODO: Do we want the values boxed??
        /// </summary>
        /// <param name="type">The P type.</param>
        /// <returns>The Java type's name.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If we're not implemented yet.</exception>
        /// TODO: Make this private and stick a weak ref cache in front of it.
        internal JType JavaTypeFor(PLanguageType type)
        {
            switch (type.Canonicalize())
            {
                case DataType _:
                    throw new NotImplementedException($"{type.CanonicalRepresentation} values not implemented");

                case EnumType _:
                    return new JType.JInt();

                case ForeignType _:
                    // return type.CanonicalRepresentation;
                    // TODO: The above might be wrong for .NET -> Java extraction!
                    throw new NotImplementedException($"{type.CanonicalRepresentation} values not implemented");

                case MapType m:
                    JType k = JavaTypeFor(m.KeyType);
                    JType v = JavaTypeFor(m.ValueType);
                    return new JType.JMap(k, v);

                case NamedTupleType _:
                    return new JType.JNamedTuple();

                case PermissionType _:
                    throw new NotImplementedException($"{type.CanonicalRepresentation} values not implemented");

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Any):
                    return new JType();

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
                    throw new NotImplementedException($"{type.CanonicalRepresentation} values not implemented");

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Null):
                    return new JType.JVoid();

                case SequenceType s:
                    return new JType.JList(JavaTypeFor(s.ElementType));

                case SetType s:
                    return new JType.JSet(JavaTypeFor(s.ElementType));

                case TupleType _:
                    //TODO: return new JType.JList(Jtype.Int, JType.Any) ???  Is this cleaner/clearer?
                    return new JType.JTuple();

                default:
                    throw new NotImplementedException($"{type.CanonicalRepresentation} values not implemented");
            }
        }
    }
}