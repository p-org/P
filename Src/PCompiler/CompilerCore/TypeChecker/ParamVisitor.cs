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
            var fields = context.seqLiteral().Select(Visit).ToArray();
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
        
        public override IPExpr VisitSeqLiteral([NotNull] PParser.SeqLiteralContext context)
        {
            return Visit(context.seqLiteralBody());
        }
        
        public override IPExpr VisitSeqLiteralBody([NotNull] PParser.SeqLiteralBodyContext context)
        {
            if (context.primitive() != null)
            {
                var values = context.primitive().Select(Visit).ToList();

                if (values.Count == 2 &&
                    values[0] is IntLiteralExpr first &&
                    values[1] is IntLiteralExpr second &&
                    first.Value == second.Value)
                {
                    throw handler.InternalError(context, new Exception("Invalid range: start and end must not be equal (e.g., [2, 2] is not allowed)"));                }

                if (values.Count == 0)
                    return new SeqLiteralExpr(context, values, PrimitiveType.Int);

                var type = values[0].Type;
                foreach (var v in values.Where(v => !v.Type.Equals(type)))
                {
                    throw handler.TypeMismatch(v.SourceLocation, v.Type, type);
                }

                return new SeqLiteralExpr(context, values, new SequenceType(type));
            }
            if (context.seqLiteral() != null)
            {
                return Visit(context.seqLiteral());
            }
            throw handler.InternalError(context, new ArgumentOutOfRangeException(nameof(context), "unknown primitive literal"));
        }
        
        public override IPExpr VisitPrimitive(PParser.PrimitiveContext context)
        {

            if (context.floatLiteral() != null)
            {
                return Visit(context.floatLiteral());
            }

            if (context.BoolLiteral() != null)
            {
                return new BoolLiteralExpr(context, context.BoolLiteral().GetText().Equals("true"));
            }

            if (context.IntLiteral() != null)
            {
                return new IntLiteralExpr(context, int.Parse(context.IntLiteral().GetText()));
            }

            if (context.NullLiteral() != null)
            {
                return new NullLiteralExpr(context);
            }

            throw handler.InternalError(context, new ArgumentOutOfRangeException(nameof(context), "unknown primitive literal"));
        }
        
        public override IPExpr VisitExpFloat(PParser.ExpFloatContext context)
        {
            throw new NotImplementedException("float(x,y) syntax");
        }

        public override IPExpr VisitDecimalFloat(PParser.DecimalFloatContext context)
        {
            var value = double.Parse($"{context.pre?.Text ?? ""}.{context.post.Text}");
            return new FloatLiteralExpr(context, value);
        }
        public override IPExpr VisitInt([NotNull] PParser.IntContext context)
        {
            return new IntLiteralExpr(context, int.Parse(context.IntLiteral().GetText()));
        }
        
    }
}