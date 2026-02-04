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
        
        public override IPExpr VisitSeqPrimitive(PParser.SeqPrimitiveContext context)
        {
            if (context.BoolLiteral() != null)
            {
                return new BoolLiteralExpr(context, context.BoolLiteral().GetText().Equals("true"));
            }

            if (context.SUB() != null && context.IntLiteral() != null)
            {
                int value = -int.Parse(context.IntLiteral().GetText());
                return new IntLiteralExpr(context, value);
            }

            if (context.IntLiteral() != null)
            {
                int value = int.Parse(context.IntLiteral().GetText());
                return new IntLiteralExpr(context, value);
            }

            throw handler.InternalError(context, new Exception("Unrecognized seqPrimitive input"));
        }
        
        public override IPExpr VisitSeqLiteralBody(PParser.SeqLiteralBodyContext context)
        {
            var values = context.seqPrimitive().Select(p =>
            {
                var expr = Visit(p) as IPExpr;
                if (expr is null)
                {
                    throw handler.InternalError(p, new Exception("Visit returned null for a seqPrimitive."));
                }
                return expr;
            }).ToList();

            var baseType = values[0].Type;
            var seenIntValues = new HashSet<int>();

            foreach (var v in values)
            {
                if (!v.Type.Equals(baseType))
                {
                    throw handler.TypeMismatch(v.SourceLocation, v.Type, baseType);
                }

                if (v is IntLiteralExpr intLiteral && !seenIntValues.Add(intLiteral.Value))
                {
                    throw handler.InternalError(
                        context,
                        new Exception(
                            $"Invalid parameter list: Duplicate integer value '{intLiteral.Value}' found. " +
                            $"All test values must be unique (e.g., [2, 2] is not allowed)."
                        ));
                }
            }

            return new SeqLiteralExpr(context, values, new SequenceType(baseType));
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