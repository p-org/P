using System;
using System.IO;
using Plang.Compiler.Backend.ASTExt;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Expressions;

namespace Plang.Compiler.Backend
{
    /// <summary>
    /// Uniform contract for emitting P expressions, implemented by the imperative
    /// code-generator backends (PChecker, PEx, PObserve). Each backend supplies one
    /// method per <see cref="IPExpr"/> node; the single dispatch over the node kinds
    /// lives in <see cref="ExpressionEmitterExtensions.WriteExpr{TContext}"/>.
    ///
    /// Because the contract has one method per node, adding a new <see cref="IPExpr"/>
    /// node (and a dispatch arm) forces every backend to handle it (or explicitly throw)
    /// or fail to compile — the property that keeps the backends from silently drifting
    /// out of sync. An interface is used rather than a base class so that a backend with
    /// its own class hierarchy (e.g. PObserve's <c>JavaSourceGenerator</c>) can still
    /// participate.
    ///
    /// <typeparamref name="TContext"/> is the backend's own compilation context; it and
    /// the output writer are simply threaded through to the per-node methods.
    /// </summary>
    internal interface IExpressionEmitter<TContext>
        where TContext : CompilationContextBase
    {
        void WriteCloneExpr(TContext context, StringWriter output, CloneExpr expr);
        void WriteBinOpExpr(TContext context, StringWriter output, BinOpExpr expr);
        void WriteBoolLiteralExpr(TContext context, StringWriter output, BoolLiteralExpr expr);
        void WriteCastExpr(TContext context, StringWriter output, CastExpr expr);
        void WriteCoerceExpr(TContext context, StringWriter output, CoerceExpr expr);
        void WriteChooseExpr(TContext context, StringWriter output, ChooseExpr expr);
        void WriteContainsExpr(TContext context, StringWriter output, ContainsExpr expr);
        void WriteCtorExpr(TContext context, StringWriter output, CtorExpr expr);
        void WriteDefaultExpr(TContext context, StringWriter output, DefaultExpr expr);
        void WriteEnumElemRefExpr(TContext context, StringWriter output, EnumElemRefExpr expr);
        void WriteEventRefExpr(TContext context, StringWriter output, EventRefExpr expr);
        void WriteFairNondetExpr(TContext context, StringWriter output, FairNondetExpr expr);
        void WriteFloatLiteralExpr(TContext context, StringWriter output, FloatLiteralExpr expr);
        void WriteFunCallExpr(TContext context, StringWriter output, FunCallExpr expr);
        void WriteIntLiteralExpr(TContext context, StringWriter output, IntLiteralExpr expr);
        void WriteKeysExpr(TContext context, StringWriter output, KeysExpr expr);
        void WriteNamedTupleExpr(TContext context, StringWriter output, NamedTupleExpr expr);
        void WriteNondetExpr(TContext context, StringWriter output, NondetExpr expr);
        void WriteNullLiteralExpr(TContext context, StringWriter output, NullLiteralExpr expr);
        void WriteSizeofExpr(TContext context, StringWriter output, SizeofExpr expr);
        void WriteStringExpr(TContext context, StringWriter output, StringExpr expr);
        void WriteThisRefExpr(TContext context, StringWriter output, ThisRefExpr expr);
        void WriteUnaryOpExpr(TContext context, StringWriter output, UnaryOpExpr expr);
        void WriteUnnamedTupleExpr(TContext context, StringWriter output, UnnamedTupleExpr expr);
        void WriteValuesExpr(TContext context, StringWriter output, ValuesExpr expr);
        void WriteMapAccessExpr(TContext context, StringWriter output, MapAccessExpr expr);
        void WriteSetAccessExpr(TContext context, StringWriter output, SetAccessExpr expr);
        void WriteNamedTupleAccessExpr(TContext context, StringWriter output, NamedTupleAccessExpr expr);
        void WriteSeqAccessExpr(TContext context, StringWriter output, SeqAccessExpr expr);
        void WriteTupleAccessExpr(TContext context, StringWriter output, TupleAccessExpr expr);
        void WriteVariableAccessExpr(TContext context, StringWriter output, VariableAccessExpr expr);
    }

    internal static class ExpressionEmitterExtensions
    {
        /// <summary>
        /// The single dispatch over <see cref="IPExpr"/> node kinds. Routes each node to
        /// the implementing backend's per-node method. Backends call this (via <c>this</c>)
        /// to emit subexpressions.
        /// </summary>
        public static void WriteExpr<TContext>(
            this IExpressionEmitter<TContext> emitter, TContext context, StringWriter output, IPExpr expr)
            where TContext : CompilationContextBase
        {
            switch (expr)
            {
                case CloneExpr e: emitter.WriteCloneExpr(context, output, e); break;
                case BinOpExpr e: emitter.WriteBinOpExpr(context, output, e); break;
                case BoolLiteralExpr e: emitter.WriteBoolLiteralExpr(context, output, e); break;
                case CastExpr e: emitter.WriteCastExpr(context, output, e); break;
                case CoerceExpr e: emitter.WriteCoerceExpr(context, output, e); break;
                case ChooseExpr e: emitter.WriteChooseExpr(context, output, e); break;
                case ContainsExpr e: emitter.WriteContainsExpr(context, output, e); break;
                case CtorExpr e: emitter.WriteCtorExpr(context, output, e); break;
                case DefaultExpr e: emitter.WriteDefaultExpr(context, output, e); break;
                case EnumElemRefExpr e: emitter.WriteEnumElemRefExpr(context, output, e); break;
                case EventRefExpr e: emitter.WriteEventRefExpr(context, output, e); break;
                case FairNondetExpr e: emitter.WriteFairNondetExpr(context, output, e); break;
                case FloatLiteralExpr e: emitter.WriteFloatLiteralExpr(context, output, e); break;
                case FunCallExpr e: emitter.WriteFunCallExpr(context, output, e); break;
                case IntLiteralExpr e: emitter.WriteIntLiteralExpr(context, output, e); break;
                case KeysExpr e: emitter.WriteKeysExpr(context, output, e); break;
                case NamedTupleExpr e: emitter.WriteNamedTupleExpr(context, output, e); break;
                case NondetExpr e: emitter.WriteNondetExpr(context, output, e); break;
                case NullLiteralExpr e: emitter.WriteNullLiteralExpr(context, output, e); break;
                case SizeofExpr e: emitter.WriteSizeofExpr(context, output, e); break;
                case StringExpr e: emitter.WriteStringExpr(context, output, e); break;
                case ThisRefExpr e: emitter.WriteThisRefExpr(context, output, e); break;
                case UnaryOpExpr e: emitter.WriteUnaryOpExpr(context, output, e); break;
                case UnnamedTupleExpr e: emitter.WriteUnnamedTupleExpr(context, output, e); break;
                case ValuesExpr e: emitter.WriteValuesExpr(context, output, e); break;
                case MapAccessExpr e: emitter.WriteMapAccessExpr(context, output, e); break;
                case SetAccessExpr e: emitter.WriteSetAccessExpr(context, output, e); break;
                case NamedTupleAccessExpr e: emitter.WriteNamedTupleAccessExpr(context, output, e); break;
                case SeqAccessExpr e: emitter.WriteSeqAccessExpr(context, output, e); break;
                case TupleAccessExpr e: emitter.WriteTupleAccessExpr(context, output, e); break;
                case VariableAccessExpr e: emitter.WriteVariableAccessExpr(context, output, e); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(expr), $"type was {expr?.GetType().FullName}");
            }
        }
    }
}
