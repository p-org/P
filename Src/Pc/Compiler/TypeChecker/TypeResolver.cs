using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker
{
    public static class TypeResolver
    {
        public static PLanguageType ResolveType(
            ParserRuleContext context,
            Scope table,
            ITranslationErrorHandler handler)
        {
            return context == null ? PrimitiveType.Null : new TypeVisitor(table, handler).Visit(context);
        }

        private class TypeVisitor : PParserBaseVisitor<PLanguageType>
        {
            private readonly Scope declarations;
            private readonly ITranslationErrorHandler handler;
            private readonly HashSet<TypeDef> visitedTypeDefs = new HashSet<TypeDef>();

            public TypeVisitor(Scope declarations, ITranslationErrorHandler handler)
            {
                this.declarations = declarations;
                this.handler = handler;
            }

            public override PLanguageType VisitBoundedType(PParser.BoundedTypeContext context)
            {
                string eventSetName = context.eventSet.GetText();
                if (!declarations.Lookup(eventSetName, out EventSet eventSet))
                {
                    throw handler.MissingDeclaration(context.eventSet, "event set", eventSetName);
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
                string typeName = context.name.GetText();
                if (declarations.Lookup(typeName, out PEnum pEnum))
                {
                    return new EnumType(pEnum);
                }

                if (declarations.Lookup(typeName, out TypeDef typeDef))
                {
                    if (visitedTypeDefs.Contains(typeDef))
                    {
                        throw handler.CircularTypeDef(context.name, typeDef);
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
                    throw new NotImplementedException("interface types");
                }

                throw handler.MissingDeclaration(context.name, "enum, typedef, or interface", typeName);
            }

            public override PLanguageType VisitTupleType(PParser.TupleTypeContext context)
            {
                var tupleTypes = context._tupTypes.Select(Visit).ToArray();
                return new TupleType(tupleTypes);
            }

            public override PLanguageType VisitNamedTupleType(PParser.NamedTupleTypeContext context)
            {
                var names = new HashSet<string>();
                var namedTupleFields = context.idenTypeList().idenType();
                var fields = new NamedTupleEntry[namedTupleFields.Length];
                for (var i = 0; i < namedTupleFields.Length; i++)
                {
                    PParser.IdenTypeContext field = namedTupleFields[i];
                    string fieldName = field.name.GetText();
                    if (names.Contains(fieldName))
                    {
                        throw handler.DuplicateNamedTupleEntry(field.name, fieldName);
                    }

                    names.Add(fieldName);
                    fields[i] = new NamedTupleEntry {Name = fieldName, Type = Visit(field.type())};
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
                    default:
                        throw new ArgumentException($"INTERNAL ERROR: Unrecognized type `{name}`!",
                                                    nameof(context));
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
