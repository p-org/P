using System;
using System.IO;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Statements;

namespace Plang.Compiler.Backend
{
    /// <summary>
    /// Uniform contract for emitting P statements, implemented by the imperative
    /// code-generator backends (PChecker, PEx, PObserve). Each backend supplies one
    /// method per general <see cref="IPStmt"/> node; the single dispatch over the node
    /// kinds lives in <see cref="StatementEmitterExtensions.WriteStmt{TContext,TFrame}"/>.
    ///
    /// Two backend differences are absorbed by the shape of the contract:
    ///   * <typeparamref name="TFrame"/> carries whatever per-emission state a backend
    ///     threads through statement emission (PChecker: the enclosing <c>Function</c>;
    ///     PEx: the function plus its <c>ControlFlowContext</c>; PObserve: nothing).
    ///   * the <c>bool</c> result reports whether the statement terminated control flow
    ///     ("exited"). PEx uses this to elide fall-through returns; the others always
    ///     return <c>false</c> and ignore it.
    ///
    /// Backends whose IR lowering means a node never reaches them (e.g. PEx eliminates
    /// <see cref="ForeachStmt"/>/<see cref="NoStmt"/>/<see cref="ReceiveStmt"/>) implement
    /// that method as an explicit throw. PEx's own <c>ReceiveSplitStmt</c> is a
    /// PEx-internal node and is handled in PEx outside this contract.
    ///
    /// As with <see cref="IExpressionEmitter{TContext}"/>, an interface is used rather
    /// than a base class so a backend with its own class hierarchy (PObserve's
    /// <c>JavaSourceGenerator</c>) can participate, and adding a statement node forces
    /// every backend to handle it (or throw) or fail to compile.
    /// </summary>
    internal interface IStatementEmitter<TContext, TFrame>
        where TContext : CompilationContextBase
    {
        bool WriteAddStmt(TContext context, StringWriter output, TFrame frame, AddStmt stmt);
        bool WriteAnnounceStmt(TContext context, StringWriter output, TFrame frame, AnnounceStmt stmt);
        bool WriteAssertStmt(TContext context, StringWriter output, TFrame frame, AssertStmt stmt);
        bool WriteAssignStmt(TContext context, StringWriter output, TFrame frame, AssignStmt stmt);
        bool WriteAssumeStmt(TContext context, StringWriter output, TFrame frame, AssumeStmt stmt);
        bool WriteBreakStmt(TContext context, StringWriter output, TFrame frame, BreakStmt stmt);
        bool WriteCompoundStmt(TContext context, StringWriter output, TFrame frame, CompoundStmt stmt);
        bool WriteContinueStmt(TContext context, StringWriter output, TFrame frame, ContinueStmt stmt);
        bool WriteCtorStmt(TContext context, StringWriter output, TFrame frame, CtorStmt stmt);
        bool WriteForeachStmt(TContext context, StringWriter output, TFrame frame, ForeachStmt stmt);
        bool WriteFunCallStmt(TContext context, StringWriter output, TFrame frame, FunCallStmt stmt);
        bool WriteGotoStmt(TContext context, StringWriter output, TFrame frame, GotoStmt stmt);
        bool WriteIfStmt(TContext context, StringWriter output, TFrame frame, IfStmt stmt);
        bool WriteInsertStmt(TContext context, StringWriter output, TFrame frame, InsertStmt stmt);
        bool WriteMoveAssignStmt(TContext context, StringWriter output, TFrame frame, MoveAssignStmt stmt);
        bool WriteNoStmt(TContext context, StringWriter output, TFrame frame, NoStmt stmt);
        bool WritePrintStmt(TContext context, StringWriter output, TFrame frame, PrintStmt stmt);
        bool WriteRaiseStmt(TContext context, StringWriter output, TFrame frame, RaiseStmt stmt);
        bool WriteReceiveStmt(TContext context, StringWriter output, TFrame frame, ReceiveStmt stmt);
        bool WriteRemoveStmt(TContext context, StringWriter output, TFrame frame, RemoveStmt stmt);
        bool WriteReturnStmt(TContext context, StringWriter output, TFrame frame, ReturnStmt stmt);
        bool WriteSendStmt(TContext context, StringWriter output, TFrame frame, SendStmt stmt);
        bool WriteSwapAssignStmt(TContext context, StringWriter output, TFrame frame, SwapAssignStmt stmt);
        bool WriteWhileStmt(TContext context, StringWriter output, TFrame frame, WhileStmt stmt);
    }

    internal static class StatementEmitterExtensions
    {
        /// <summary>
        /// The single dispatch over general <see cref="IPStmt"/> node kinds. Returns the
        /// backend's per-node result (whether control flow exited). Backends call this
        /// (via <c>this</c>) to emit nested statements.
        /// </summary>
        public static bool WriteStmt<TContext, TFrame>(
            this IStatementEmitter<TContext, TFrame> emitter,
            TContext context, StringWriter output, TFrame frame, IPStmt stmt)
            where TContext : CompilationContextBase
        {
            switch (stmt)
            {
                case AddStmt s: return emitter.WriteAddStmt(context, output, frame, s);
                case AnnounceStmt s: return emitter.WriteAnnounceStmt(context, output, frame, s);
                case AssertStmt s: return emitter.WriteAssertStmt(context, output, frame, s);
                case AssignStmt s: return emitter.WriteAssignStmt(context, output, frame, s);
                case AssumeStmt s: return emitter.WriteAssumeStmt(context, output, frame, s);
                case BreakStmt s: return emitter.WriteBreakStmt(context, output, frame, s);
                case CompoundStmt s: return emitter.WriteCompoundStmt(context, output, frame, s);
                case ContinueStmt s: return emitter.WriteContinueStmt(context, output, frame, s);
                case CtorStmt s: return emitter.WriteCtorStmt(context, output, frame, s);
                case ForeachStmt s: return emitter.WriteForeachStmt(context, output, frame, s);
                case FunCallStmt s: return emitter.WriteFunCallStmt(context, output, frame, s);
                case GotoStmt s: return emitter.WriteGotoStmt(context, output, frame, s);
                case IfStmt s: return emitter.WriteIfStmt(context, output, frame, s);
                case InsertStmt s: return emitter.WriteInsertStmt(context, output, frame, s);
                case MoveAssignStmt s: return emitter.WriteMoveAssignStmt(context, output, frame, s);
                case NoStmt s: return emitter.WriteNoStmt(context, output, frame, s);
                case PrintStmt s: return emitter.WritePrintStmt(context, output, frame, s);
                case RaiseStmt s: return emitter.WriteRaiseStmt(context, output, frame, s);
                case ReceiveStmt s: return emitter.WriteReceiveStmt(context, output, frame, s);
                case RemoveStmt s: return emitter.WriteRemoveStmt(context, output, frame, s);
                case ReturnStmt s: return emitter.WriteReturnStmt(context, output, frame, s);
                case SendStmt s: return emitter.WriteSendStmt(context, output, frame, s);
                case SwapAssignStmt s: return emitter.WriteSwapAssignStmt(context, output, frame, s);
                case WhileStmt s: return emitter.WriteWhileStmt(context, output, frame, s);
                default:
                    throw new ArgumentOutOfRangeException(nameof(stmt), $"type was {stmt?.GetType().FullName}");
            }
        }
    }
}
