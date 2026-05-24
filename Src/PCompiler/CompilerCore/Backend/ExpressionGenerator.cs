using System;
using System.IO;
using Plang.Compiler.Backend.ASTExt;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Expressions;

namespace Plang.Compiler.Backend
{
    /// <summary>
    /// Shared dispatch for emitting P expressions across the imperative code-generator
    /// backends (PChecker, PEx, PObserve).
    ///
    /// The <c>switch</c> over <see cref="IPExpr"/> lives here exactly once; each backend
    /// supplies the per-node emission by overriding the abstract <c>Write*Expr</c> methods.
    /// Because every node has a dedicated abstract method, adding a new <see cref="IPExpr"/>
    /// node (and a dispatch arm here) forces every backend to handle it or fail to compile —
    /// which is the property that keeps the backends from silently drifting out of sync.
    ///
    /// <typeparamref name="TContext"/> is the backend's own compilation context; the base
    /// only threads it (and the output writer) through to the per-node methods.
    /// </summary>
    internal abstract class ExpressionGenerator<TContext>
        where TContext : CompilationContextBase
    {
        protected void WriteExpr(TContext context, StringWriter output, IPExpr expr)
        {
            switch (expr)
            {
                case CloneExpr e: WriteCloneExpr(context, output, e); break;
                case BinOpExpr e: WriteBinOpExpr(context, output, e); break;
                case BoolLiteralExpr e: WriteBoolLiteralExpr(context, output, e); break;
                case CastExpr e: WriteCastExpr(context, output, e); break;
                case CoerceExpr e: WriteCoerceExpr(context, output, e); break;
                case ChooseExpr e: WriteChooseExpr(context, output, e); break;
                case ContainsExpr e: WriteContainsExpr(context, output, e); break;
                case CtorExpr e: WriteCtorExpr(context, output, e); break;
                case DefaultExpr e: WriteDefaultExpr(context, output, e); break;
                case EnumElemRefExpr e: WriteEnumElemRefExpr(context, output, e); break;
                case EventRefExpr e: WriteEventRefExpr(context, output, e); break;
                case FairNondetExpr e: WriteFairNondetExpr(context, output, e); break;
                case FloatLiteralExpr e: WriteFloatLiteralExpr(context, output, e); break;
                case FunCallExpr e: WriteFunCallExpr(context, output, e); break;
                case IntLiteralExpr e: WriteIntLiteralExpr(context, output, e); break;
                case KeysExpr e: WriteKeysExpr(context, output, e); break;
                case NamedTupleExpr e: WriteNamedTupleExpr(context, output, e); break;
                case NondetExpr e: WriteNondetExpr(context, output, e); break;
                case NullLiteralExpr e: WriteNullLiteralExpr(context, output, e); break;
                case SizeofExpr e: WriteSizeofExpr(context, output, e); break;
                case StringExpr e: WriteStringExpr(context, output, e); break;
                case ThisRefExpr e: WriteThisRefExpr(context, output, e); break;
                case UnaryOpExpr e: WriteUnaryOpExpr(context, output, e); break;
                case UnnamedTupleExpr e: WriteUnnamedTupleExpr(context, output, e); break;
                case ValuesExpr e: WriteValuesExpr(context, output, e); break;
                case MapAccessExpr e: WriteMapAccessExpr(context, output, e); break;
                case SetAccessExpr e: WriteSetAccessExpr(context, output, e); break;
                case NamedTupleAccessExpr e: WriteNamedTupleAccessExpr(context, output, e); break;
                case SeqAccessExpr e: WriteSeqAccessExpr(context, output, e); break;
                case TupleAccessExpr e: WriteTupleAccessExpr(context, output, e); break;
                case VariableAccessExpr e: WriteVariableAccessExpr(context, output, e); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(expr), $"type was {expr?.GetType().FullName}");
            }
        }

        protected abstract void WriteCloneExpr(TContext context, StringWriter output, CloneExpr expr);
        protected abstract void WriteBinOpExpr(TContext context, StringWriter output, BinOpExpr expr);
        protected abstract void WriteBoolLiteralExpr(TContext context, StringWriter output, BoolLiteralExpr expr);
        protected abstract void WriteCastExpr(TContext context, StringWriter output, CastExpr expr);
        protected abstract void WriteCoerceExpr(TContext context, StringWriter output, CoerceExpr expr);
        protected abstract void WriteChooseExpr(TContext context, StringWriter output, ChooseExpr expr);
        protected abstract void WriteContainsExpr(TContext context, StringWriter output, ContainsExpr expr);
        protected abstract void WriteCtorExpr(TContext context, StringWriter output, CtorExpr expr);
        protected abstract void WriteDefaultExpr(TContext context, StringWriter output, DefaultExpr expr);
        protected abstract void WriteEnumElemRefExpr(TContext context, StringWriter output, EnumElemRefExpr expr);
        protected abstract void WriteEventRefExpr(TContext context, StringWriter output, EventRefExpr expr);
        protected abstract void WriteFairNondetExpr(TContext context, StringWriter output, FairNondetExpr expr);
        protected abstract void WriteFloatLiteralExpr(TContext context, StringWriter output, FloatLiteralExpr expr);
        protected abstract void WriteFunCallExpr(TContext context, StringWriter output, FunCallExpr expr);
        protected abstract void WriteIntLiteralExpr(TContext context, StringWriter output, IntLiteralExpr expr);
        protected abstract void WriteKeysExpr(TContext context, StringWriter output, KeysExpr expr);
        protected abstract void WriteNamedTupleExpr(TContext context, StringWriter output, NamedTupleExpr expr);
        protected abstract void WriteNondetExpr(TContext context, StringWriter output, NondetExpr expr);
        protected abstract void WriteNullLiteralExpr(TContext context, StringWriter output, NullLiteralExpr expr);
        protected abstract void WriteSizeofExpr(TContext context, StringWriter output, SizeofExpr expr);
        protected abstract void WriteStringExpr(TContext context, StringWriter output, StringExpr expr);
        protected abstract void WriteThisRefExpr(TContext context, StringWriter output, ThisRefExpr expr);
        protected abstract void WriteUnaryOpExpr(TContext context, StringWriter output, UnaryOpExpr expr);
        protected abstract void WriteUnnamedTupleExpr(TContext context, StringWriter output, UnnamedTupleExpr expr);
        protected abstract void WriteValuesExpr(TContext context, StringWriter output, ValuesExpr expr);
        protected abstract void WriteMapAccessExpr(TContext context, StringWriter output, MapAccessExpr expr);
        protected abstract void WriteSetAccessExpr(TContext context, StringWriter output, SetAccessExpr expr);
        protected abstract void WriteNamedTupleAccessExpr(TContext context, StringWriter output, NamedTupleAccessExpr expr);
        protected abstract void WriteSeqAccessExpr(TContext context, StringWriter output, SeqAccessExpr expr);
        protected abstract void WriteTupleAccessExpr(TContext context, StringWriter output, TupleAccessExpr expr);
        protected abstract void WriteVariableAccessExpr(TContext context, StringWriter output, VariableAccessExpr expr);
    }
}
