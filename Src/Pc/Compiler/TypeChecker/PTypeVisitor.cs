using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    public class Function : IPDeclaration
    {
        public Function(ParserRuleContext origin, string name)
        {
            Origin = origin;
            Name = name;
        }

        public ParserRuleContext Origin { get; }
        public string Name { get; }
    }

    public class Machine : IPDeclaration
    {
        public Machine(ParserRuleContext origin, string name)
        {
            Origin = origin;
            Name = name;
        }

        public ParserRuleContext Origin { get; }
        public string Name { get; }
    }

    public class MachineType : PLanguageType
    {
        private Machine declaration;

        public MachineType(Machine declaration) : base(declaration.Name, TypeKind.Machine, declaration.Name)
        {
            this.declaration = declaration;
        }
    }

    public class ForeignType : PLanguageType, IPDeclaration
    {
        public ForeignType(string name, PParser.ForeignTypeDefContext origin) : base(name, TypeKind.Foreign, name)
        {
            Name = name;
            Origin = origin;
        }

        public ParserRuleContext Origin { get; }
        public string Name { get; }
    }

    public interface IPDeclaration
    {
        ParserRuleContext Origin { get; }

        string Name { get; }
    }

    public class MachineInterface : IPDeclaration
    {
        public MachineInterface(ParserRuleContext origin, string name)
        {
            Origin = origin;
            Name = name;
        }

        public ParserRuleContext Origin { get; }
        public string Name { get; }
    }

    public class PEnum : IPDeclaration
    {
        public PEnum(string enumName, IList<string> itemNames, IList<int> values, ParserRuleContext origin)
        {
            Name = enumName;
            ItemNames = itemNames;
            Values = values;
            Origin = origin;
        }

        public IList<string> ItemNames { get; }
        public IList<int> Values { get; }

        public ParserRuleContext Origin { get; }
        public string Name { get; }
    }

    /*
    public class PTypeVisitor : PParserBaseVisitor<PLanguageType>
    {
        private readonly SymbolTable symbolTable;
        private readonly PTypeUniverse typeUniverse;

        public PTypeVisitor(PTypeUniverse typeUniverse, SymbolTable symbolTable)
        {
            this.typeUniverse = typeUniverse;
            this.symbolTable = symbolTable;
        }

        public override PLanguageType VisitBoundedType(PParser.BoundedTypeContext context)
        {
            string eventSetName = context.eventSet.Text;
            if (symbolTable.Lookup(eventSetName, out EventSet eventSet))
            {
                return typeUniverse.GetOrCreateBoundedType(eventSet);
            }

            throw new TypeConstructionException($"Cannot find event set `{eventSetName}`.", context, context.eventSet);
        }

        public override PLanguageType VisitSeqType(PParser.SeqTypeContext context)
        {
            PParser.TypeContext elementTypeCtx = context.type();
            PLanguageType elementType = Visit(elementTypeCtx);
            return typeUniverse.GetOrCreateSeqType(elementType);
        }

        public override PLanguageType VisitNamedType(PParser.NamedTypeContext context)
        {
            string typeName = context.Iden().Symbol.Text;
            if (!symbolTable.Lookup(typeName, out IPDeclaration declaration))
            {
                throw new TypeConstructionException($"Could not find symbol {typeName}", context, context.Iden().Symbol);
            }

            if (declaration is TypedefType)
            {
                return ((TypedefType) declaration).ActualType;
            }

            if (declaration is ForeignType)
            {
                return (ForeignType) declaration;
            }

            if (declaration is Machine)
            {
                return typeUniverse.GetOrCreateMachineType((Machine) declaration);
            }

            throw new NotImplementedException("TODO!");
        }

        public override PLanguageType VisitTupleType(PParser.TupleTypeContext context)
        {
            PLanguageType[] types = context.type().Select(Visit).ToArray();
            return typeUniverse.GetOrCreateTupleType(types);
        }

        public override PLanguageType VisitNamedTupleType(PParser.NamedTupleTypeContext context)
        {
            PParser.IdenTypeListContext fieldsCtx = context.idenTypeList();
            TypedName[] fields = TypedNamesFromIdenTypeList(fieldsCtx);
            return typeUniverse.GetOrCreateNamedTupleType(fields);
        }

        private TypedName[] TypedNamesFromIdenTypeList(PParser.IdenTypeListContext context)
        {
            return context._names.Zip(context._types, (name, typeCtx) => new TypedName {Name = name.Text, Type = Visit(typeCtx)}).ToArray();
        }

        public override PLanguageType VisitPrimitiveType(PParser.PrimitiveTypeContext context)
        {
            if (context.BOOL() != null)
            {
                return PrimitiveType.Bool;
            }

            if (context.INT() != null)
            {
                return PrimitiveType.Int;
            }

            if (context.FLOAT() != null)
            {
                return PrimitiveType.Float;
            }

            if (context.EVENT() != null)
            {
                return PrimitiveType.Event;
            }

            if (context.MACHINE() != null)
            {
                return PrimitiveType.Machine;
            }

            if (context.DATA() != null)
            {
                return PrimitiveType.Data;
            }

            if (context.ANY() != null)
            {
                return PrimitiveType.Any;
            }

            Debug.Fail("Reached impossible primitive type");
            throw new InvalidOperationException("Reached impossible primitive type");
        }

        public override PLanguageType VisitMapType(PParser.MapTypeContext context)
        {
            PLanguageType keyType = Visit(context.keyType);
            PLanguageType valueType = Visit(context.valueType);
            return typeUniverse.GetOrCreateMapType(keyType, valueType);
        }
    }
    */
}