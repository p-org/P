using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker
{

    internal class ParamExprVisitor(ITranslationErrorHandler handler) : PParserBaseVisitor<IPExpr>
    {
        public override IPExpr VisitParam([NotNull] PParser.ParamContext context)
        {
            return Visit(context.paramBody());
        }
        
        public override IPExpr VisitParamBody([NotNull] PParser.ParamBodyContext context)
        {
            var fields = context.intListLiteral().Select(Visit).ToArray();
            var entries = new NamedTupleEntry[fields.Length];
            var names = new List<string>();
            for (var i = 0; i < fields.Length; i++)
            {
                var entryName = context.iden()[i].GetText();
                if (names.Contains(entryName))
                {
                    throw handler.DuplicateNamedTupleEntry(context.iden()[i], entryName);
                }
                names.Add(entryName);
                entries[i] = new NamedTupleEntry { Name = entryName, FieldNo = i, Type = fields[i].Type };
            }

            var type = new NamedTupleType(entries);
            return new NamedTupleExpr(context, fields, type);
        }
        
        public override IPExpr VisitIntListLiteral([NotNull] PParser.IntListLiteralContext context)
        {
            return Visit(context.intListLiteralBody());
        }
        
        public override IPExpr VisitIntListLiteralBody([NotNull] PParser.IntListLiteralBodyContext context)
        {
            var values = context.IntLiteral().Select(ctx =>
                (IPExpr) new IntLiteralExpr(context, int.Parse(ctx.GetText())
                )).ToList();
            if (values.Count == 0) return new SeqLiteralExpr(context, values, PrimitiveType.Int);
            Console.WriteLine($"value[0] = {values[0].GetType()}");
            var type = (PrimitiveType)values[0].Type;
            foreach (var v in values.Where(v => !v.Type.Equals(type)))
            {
                throw handler.TypeMismatch(v.SourceLocation, v.Type, type);
            }
            return new SeqLiteralExpr(context, values, new SequenceType(type));
        }
        
        public override IPExpr VisitInt([NotNull] PParser.IntContext context)
        {
            return new IntLiteralExpr(context, int.Parse(context.IntLiteral().GetText()));
        }
        
    }
}