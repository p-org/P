using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plang.Compiler.TypeChecker
{
    public static class TypeResolver
    {
        public static PLanguageType ResolveType(
            ParserRuleContext context,
            Scope scope,
            ITranslationErrorHandler handler)
        {
            return context == null ? PrimitiveType.Null : new TypeVisitor(scope, handler).Visit(context);
        }

        private class TypeVisitor : PParserBaseVisitor<PLanguageType>
        {
            private readonly ITranslationErrorHandler handler;
            private readonly Scope scope;
            private readonly HashSet<TypeDef> visitedTypeDefs = new HashSet<TypeDef>();

            public TypeVisitor(Scope scope, ITranslationErrorHandler handler)
            {
                this.scope = scope;
                this.handler = handler;
            }

            public override PLanguageType VisitSeqType(PParser.SeqTypeContext context)
            {
                return new SequenceType(Visit(context.type()));
            }

            public override PLanguageType VisitSetType(PParser.SetTypeContext context)
            {
                return new SetType(Visit(context.type()));
            }

            public override PLanguageType VisitNamedType(PParser.NamedTypeContext context)
            {
                string typeName = context.name.GetText();
                if (scope.Lookup(typeName, out PEnum pEnum))
                {
                    return new EnumType(pEnum);
                }

                if (scope.Lookup(typeName, out TypeDef typeDef))
                {
                    if (visitedTypeDefs.Contains(typeDef))
                    {
                        throw handler.CircularTypeDef(context.name, typeDef);
                    }

                    if (typeDef.Type == null)
                    {
                        visitedTypeDefs.Add(typeDef);
                        switch (typeDef.SourceLocation)
                        {
                            case PParser.ForeignTypeDefContext foreignType:
                                typeDef.Type = new ForeignType(foreignType.name.GetText());
                                break;

                            case PParser.PTypeDefContext typedefDecl:
                                typeDef.Type = Visit(typedefDecl.type());
                                break;

                            default:
                                throw handler.InternalError(typeDef.SourceLocation,
                                    new ArgumentOutOfRangeException(nameof(context)));
                        }
                    }

                    return new TypeDefType(typeDef);
                }

                if (scope.Lookup(typeName, out NamedEventSet eventSet))
                {
                    return new PermissionType(eventSet);
                }

                if (scope.Lookup(typeName, out Interface pInterface))
                {
                    return new PermissionType(pInterface);
                }

                if (scope.Lookup(typeName, out Machine machine))
                {
                    return new PermissionType(machine);
                }

                throw handler.MissingDeclaration(context.name, "enum, typedef, event set, machine, or interface",
                    typeName);
            }

            public override PLanguageType VisitTupleType(PParser.TupleTypeContext context)
            {
                if (context._tupTypes.Count > 8)
                {
                    throw handler.TupleSizeMoreThanEight(context);
                }

                return new TupleType(context._tupTypes.Select(Visit).ToArray());
            }

            public override PLanguageType VisitNamedTupleType(PParser.NamedTupleTypeContext context)
            {
                HashSet<string> names = new HashSet<string>();
                PParser.IdenTypeContext[] namedTupleFields = context.idenTypeList().idenType();
                if (context.idenTypeList().idenType().Length > 8)
                {
                    throw handler.TupleSizeMoreThanEight(context);
                }

                NamedTupleEntry[] fields = new NamedTupleEntry[namedTupleFields.Length];
                for (int i = 0; i < namedTupleFields.Length; i++)
                {
                    PParser.IdenTypeContext field = namedTupleFields[i];
                    string fieldName = field.name.GetText();
                    if (names.Contains(fieldName))
                    {
                        throw handler.DuplicateNamedTupleEntry(field.name, fieldName);
                    }

                    names.Add(fieldName);
                    fields[i] = new NamedTupleEntry { Name = fieldName, FieldNo = i, Type = Visit(field.type()) };
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
                    case "string": return PrimitiveType.String;
                    case "event": return PrimitiveType.Event;
                    case "machine": return PrimitiveType.Machine;
                    case "data": return PrimitiveType.Data;
                    case "any": return PrimitiveType.Any;
                    default:
                        throw handler.InternalError(context, new ArgumentOutOfRangeException(nameof(context)));
                }
            }

            public override PLanguageType VisitMapType(PParser.MapTypeContext context)
            {
                return new MapType(Visit(context.keyType), Visit(context.valueType));
            }
        }
    }
}