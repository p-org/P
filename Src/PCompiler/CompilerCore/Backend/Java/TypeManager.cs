using System;
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
            /// Throws for non-collection types!!
            /// </summary>
            internal virtual string AccessorMethodName =>
                throw new Exception($"AccessorMethodName not implemented for {this.TypeName}");

            /// <summary>
            /// The name of the method (K) -> bool that returns whether K is contained in an
            /// instance of this type.  Throws for non-collection types!!
            /// </summary>
            internal virtual string ContainsMethodName =>
                throw new Exception($"ContainsMethodName not implemented for {this.TypeName}");

            /// <summary>
            /// The name of the method (K, V) -> void that inserts V by key K.
            /// Throws for non-collection types!
            /// </summary>
            internal virtual string MutatorMethodName =>
                throw new Exception($"MutatorMethodName not implemented for {this.TypeName}");
            
            /// <summary>
            /// The name of the method K -> void that removes key K from the collection.  Throws for
            /// non-collection types!
            /// </summary>
            internal virtual string RemoveMethodName =>
                throw new Exception($"RemoveMethodName not implemented for {this.TypeName}");
            
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

            internal class JMachine : JType
            {
                // Source/Core/Actors/ActorId.cs stores ActorID values as ulongs
                
                internal override string TypeName => "long";
                internal override string ReferenceTypeName => "Long";
                internal override string DefaultValue => ToJavaLiteral(0L);
                internal static string ToJavaLiteral(long l)
                {
                    return l + "L";
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
                internal override string RemoveMethodName => "remove";
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
                    $"HashMap<{_k.ReferenceTypeName},{_v.ReferenceTypeName}>";
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
                internal string KeyCollectionType => $"List<{_k}>";
                
                /// <summary>
                /// The type of a collection containing the keys of this Map.
                /// </summary>
                internal string ValueCollectionType => $"List<{_v}>";
            }
            
            internal class JSet : JType
            {
                private readonly JType _t;

                internal JSet(JType t)
                {
                    _t = t;
                }

                internal override string TypeName => 
                    $"HashSet<{_t.ReferenceTypeName}>";
                
                internal override string AccessorMethodName => "contains";
                internal override string ContainsMethodName => "contains";
                internal override string MutatorMethodName => "add";
                internal override string RemoveMethodName => "remove";
            }

            // TODO: maybe we can hack it as a Record??  A bummer that we don't have Scala's Tuple[A,B,C,...].
            internal class JNamedTuple : JType
            {
                internal override string TypeName => "HashMap<String, Object>";
                
                internal override string AccessorMethodName => "get";
                internal override string ContainsMethodName => "containsKey";
                internal override string MutatorMethodName => "set";
                internal override string RemoveMethodName => "remove";
            }
           
            internal class JTuple : JType
            {
                internal override string TypeName => "ArrayList<Object>";
                
                internal override string AccessorMethodName => "get";
                internal override string ContainsMethodName => "contains";
                internal override string MutatorMethodName => "set";
                internal override string RemoveMethodName => "remove";
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
                // XXX: This is slightly hacky in that `void` can be a return type but 
                // not a variable type, and `Void` can be a variable type and a return
                // type but a valueless-return statement doesn't "autobox" into a Void.
                internal override string TypeName => "void";
                internal override string ReferenceTypeName => "Void";
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
                    JType k = JavaTypeFor(mapAccessExpr.IndexExpr.Type);
                    JType v = JavaTypeFor(mapAccessExpr.Type);
                    return new JType.JMap(k, v);
                }

                case NamedTupleAccessExpr namedTupleAccessExpr:
                    return new JType.JNamedTuple();

                case SeqAccessExpr seqAccessExpr:
                {
                    JType t = JavaTypeFor(seqAccessExpr.SeqExpr.Type);
                    return new JType.JList(t);
                }

                case TupleAccessExpr tupleAccessExpr:
                    return new JType.JTuple();

                case VariableAccessExpr variableAccessExpr:
                    return JavaTypeFor(e.Type);
                
                default:
                    throw new Exception($"Unknown location of {e}");
            }
        }
        
        /// <summary>
        /// Produces the Java type used to represent a value of type `type`.
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
                    return new JType.JMachine();

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
                    return new JType.JMachine();

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Null):
                    return new JType.JVoid();

                case SequenceType s:
                    return new JType.JList(JavaTypeFor(s.ElementType));

                case SetType s:
                    return new JType.JSet(JavaTypeFor(s.ElementType));

                case TupleType _:
                    //TODO: return new JType.JList(JType.Any) ???  Is this cleaner/clearer?
                    return new JType.JTuple();

                default:
                    throw new NotImplementedException($"{type} values not implemented");
            }
        }
    }
}