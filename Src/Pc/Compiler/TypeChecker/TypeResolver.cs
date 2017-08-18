using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    public static class TypeResolver
    {
        public static PLanguageType ResolveType(ParserRuleContext context, DeclarationTable table)
        {
            return context == null ? PrimitiveType.Null : new TypeVisitor(table).Visit(context);
        }

        private class TypeVisitor : PParserBaseVisitor<PLanguageType>
        {
            private readonly DeclarationTable declarations;
            private readonly HashSet<TypeDef> visitedTypeDefs = new HashSet<TypeDef>();

            public TypeVisitor(DeclarationTable declarations)
            {
                this.declarations = declarations;
            }

            public override PLanguageType VisitBoundedType(PParser.BoundedTypeContext context)
            {
                string eventSetName = context.eventSet.Text;
                if (!declarations.Lookup(eventSetName, out EventSet eventSet))
                {
                    throw new TypeConstructionException($"Event set {eventSetName} does not exist.", context, context.eventSet);
                }

                return new BoundedType(eventSet);
            }

            public override PLanguageType VisitSeqType(PParser.SeqTypeContext context)
            {
                PLanguageType elemType = Visit(context.type());
                return new SequenceType(elemType);
            }

            public override PLanguageType VisitNamedType(PParser.NamedTypeContext context)
            {
                string typeName = context.name.Text;
                if (declarations.Lookup(typeName, out PEnum pEnum))
                {
                    return new EnumType(pEnum);
                }

                if (declarations.Lookup(typeName, out TypeDef typeDef))
                {
                    if (visitedTypeDefs.Contains(typeDef))
                    {
                        throw new TypeConstructionException($"Typedef {typeDef.Name} is circular!", context, context.name);
                    }

                    if (typeDef.Type == null)
                    {
                        visitedTypeDefs.Add(typeDef);
                        var typeDefContext = (PParser.PTypeDefContext) typeDef.SourceNode;
                        typeDef.Type = Visit(typeDefContext.type());
                    }

                    return new TypeDefType(typeDef);
                }

                if (declarations.Lookup(typeName, out Interface pInterface))
                {
                    throw new NotImplementedException("Have yet to implement interface types");
                }

                throw new TypeConstructionException(
                    $"Expected an enum, typedef, or interface name, but got {typeName}.",
                    context,
                    context.name);
            }

            public override PLanguageType VisitTupleType(PParser.TupleTypeContext context)
            {
                PLanguageType[] tupleTypes = context._tupTypes.Select(Visit).ToArray();
                return new TupleType(tupleTypes);
            }

            public override PLanguageType VisitNamedTupleType(PParser.NamedTupleTypeContext context)
            {
                var names = new HashSet<string>();
                PParser.IdenTypeContext[] namedTupleFields = context.idenTypeList().idenType();
                var fields = new NamedTupleEntry[namedTupleFields.Length];
                for (var i = 0; i < namedTupleFields.Length; i++)
                {
                    PParser.IdenTypeContext field = namedTupleFields[i];
                    string fieldName = field.name.Text;
                    if (names.Contains(fieldName))
                    {
                        throw new TypeConstructionException($"Duplicate entry {fieldName} in named tuple.", context, field.name);
                    }

                    names.Add(fieldName);
                    fields[i].Name = fieldName;
                    fields[i].Type = Visit(field.type());
                }

                return new NamedTupleType(fields);
            }

            public override PLanguageType VisitPrimitiveType(PParser.PrimitiveTypeContext context)
            {
                string name = context.GetText();
                switch (name)
                {
                    case "bool": return PrimitiveType.Bool;
                    case "int": return PrimitiveType.Int;
                    case "float": return PrimitiveType.Float;
                    case "event": return PrimitiveType.Event;
                    case "machine": return PrimitiveType.Machine;
                    case "data": return PrimitiveType.Data;
                    case "any": return PrimitiveType.Any;
                    default: throw new ArgumentException($"INTERNAL ERROR: Unrecognized primitive type `{name}`!", nameof(context));
                }
            }

            public override PLanguageType VisitMapType(PParser.MapTypeContext context)
            {
                PLanguageType keyType = Visit(context.keyType);
                PLanguageType valueType = Visit(context.valueType);
                return new MapType(keyType, valueType);
            }
        }
    }
}