using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.Backend.ASTExt;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend
{
    /// <summary>
    /// How much <see cref="IRTransformer"/> lowers a function body.
    /// </summary>
    public enum LoweringMode
    {
        /// <summary>
        /// Every subexpression in a temporary and every value copied explicitly, which the Java
        /// backends (PEx, PObserve) rely on.
        /// </summary>
        Full,

        /// <summary>
        /// Only the rewrites the C# backend (PChecker) needs; expressions stay inline.
        /// </summary>
        CSharp
    }

    /// <summary>
    /// The C# lowering. It produces the same behavior as the full lowering (the same values,
    /// the same order of side effects and of reads that a call could change), but keeps
    /// expressions inline and copies only values that are not already fresh. It lifts a
    /// subexpression into a statement only when the generated C# needs it:
    /// <list type="bullet">
    /// <item>A call that may leave a pending transition, or that is foreign, must be a statement
    /// (or the whole value of an assignment), because only there does the generator write the
    /// pending-transition check and the foreign-call prologue and epilogue.</item>
    /// <item>A call that may change fields (a machine method, or code that calls foreign code)
    /// is lifted when C# would otherwise read a field before the call that the full lowering
    /// reads after it.</item>
    /// <item>When something is lifted, the subexpressions that the full lowering evaluates
    /// before it are lifted as well, so the order of evaluation is unchanged.</item>
    /// </list>
    /// </summary>
    public partial class IRTransformer
    {
        private readonly HashSet<Function> foreignCallers;
        private readonly HashSet<Variable> freshTemps = new HashSet<Variable>();
        private readonly IPStmt originalBody;

        private IRTransformer(Function function, HashSet<Function> foreignCallers)
        {
            this.function = function;
            this.foreignCallers = foreignCallers;
            originalBody = function.Body;
        }

        /// <summary>
        /// Lowers a function for the specified mode. <paramref name="foreignCallers"/> (see
        /// <see cref="ComputeForeignCallers"/>) is needed for the C# mode only.
        /// </summary>
        public static void SimplifyMethod(Function function, LoweringMode mode, HashSet<Function> foreignCallers)
        {
            if (mode == LoweringMode.Full)
            {
                SimplifyMethod(function);
                return;
            }

            if (function.IsForeign)
            {
                return;
            }

            var transformer = new IRTransformer(function, foreignCallers);
            IPStmt functionBody = function.Body;
            function.Body = new CompoundStmt(functionBody.SourceLocation, transformer.LowerStatement(functionBody));
        }

        /// <summary>
        /// Returns the foreign functions and every function that transitively calls one.
        /// </summary>
        public static HashSet<Function> ComputeForeignCallers(Scope globalScope)
        {
            var result = new HashSet<Function>();
            var worklist = new Stack<Function>(globalScope.GetAllMethods().Where(f => f.IsForeign));
            while (worklist.Count > 0)
            {
                var callee = worklist.Pop();
                if (!result.Add(callee))
                {
                    continue;
                }

                foreach (var caller in callee.Callers)
                {
                    worklist.Push(caller);
                }
            }

            return result;
        }

        /// <summary>
        /// Can a call to the function leave a pending transition, or is it foreign? The generator
        /// writes the pending-transition check and the foreign-call prologue and epilogue only
        /// after a call statement or an assignment of a call (see
        /// <c>PCheckerCodeGenerator.MayLeavePendingTransition</c>).
        /// </summary>
        private bool NeedsCallStatement(Function callee) =>
            callee.IsForeign || callee.CanRaiseEvent || callee.CanChangeState || callee.CanReceive ||
            foreignCallers.Contains(callee);

        /// <summary>
        /// Can a call to the function change a variable of the caller? A machine method can change
        /// fields, and foreign code can call back into the machine. Callees get copies of their
        /// arguments, and no callee can change locals.
        /// </summary>
        private bool MayChangeFields(Function callee) => callee.Owner != null || foreignCallers.Contains(callee);

        private (VariableAccessExpr, IPStmt) LowerToTemporary(IPExpr expr)
        {
            var location = expr.SourceLocation;
            var temp = new Variable($"$tmp{numTemp++}", location, VariableRole.Local | VariableRole.Temp)
            {
                Type = expr.Type
            };
            function.AddLocalVariable(temp);
            if (IsFresh(expr))
            {
                freshTemps.Add(temp);
            }

            return (new VariableAccessExpr(location, temp),
                new AssignStmt(location, new VariableAccessExpr(location, temp), expr));
        }

        private static bool IsField(IPExpr expr) =>
            expr is VariableAccessExpr { Variable: var variable } && variable.Role.HasFlag(VariableRole.Field);

        /// <summary>
        /// Values of these types are immutable, so they are never copied (see <c>PValues.Clone</c>).
        /// </summary>
        private static bool IsImmutable(PLanguageType type)
        {
            switch (type.Canonicalize())
            {
                case EnumType _:
                case PermissionType _:
                    return true;
                case PrimitiveType primitiveType:
                    return primitiveType.IsSameTypeAs(PrimitiveType.Int) ||
                           primitiveType.IsSameTypeAs(PrimitiveType.Bool) ||
                           primitiveType.IsSameTypeAs(PrimitiveType.Float) ||
                           primitiveType.IsSameTypeAs(PrimitiveType.String) ||
                           primitiveType.IsSameTypeAs(PrimitiveType.Machine);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Does the expression evaluate to a value that nothing else refers to? Accesses into values
        /// and choices from collections return elements of other values, and variables are visible
        /// to the rest of the program. Everything else builds a new value: calls return copies (see
        /// the return statement below), and operators, constructors and literals build new values.
        /// </summary>
        private bool IsFresh(IPExpr expr)
        {
            switch (expr)
            {
                case VariableAccessExpr variableAccessExpr:
                    return freshTemps.Contains(variableAccessExpr.Variable);
                case MapAccessExpr _:
                case SetAccessExpr _:
                case SeqAccessExpr _:
                case NamedTupleAccessExpr _:
                case TupleAccessExpr _:
                    return false;
                case ChooseExpr chooseExpr:
                    return chooseExpr.SubExpr == null || IsImmutable(chooseExpr.Type);
                case CastExpr castExpr:
                    return IsFresh(castExpr.SubExpr);
                default:
                    return true;
            }
        }

        /// <summary>
        /// P has value semantics: a value that is stored or passed on is copied unless it is fresh.
        /// </summary>
        private IPExpr CopyIfNeeded(IPExpr expr) =>
            IsFresh(expr) || IsImmutable(expr.Type) ? expr : new CloneExpr(expr);

        /// <summary>
        /// Is evaluating the expression later than the full lowering does unobservable? Constants
        /// read no state, and temporaries are not assigned after they are read.
        /// </summary>
        private static bool IsStable(IPExpr expr)
        {
            switch (expr)
            {
                case VariableAccessExpr variableAccessExpr:
                    return variableAccessExpr.Variable.Role.HasFlag(VariableRole.Temp);
                case BoolLiteralExpr _:
                case IntLiteralExpr _:
                case FloatLiteralExpr _:
                case NullLiteralExpr _:
                case EnumElemRefExpr _:
                case EventRefExpr _:
                case ThisRefExpr _:
                case DefaultExpr _:
                    return true;
                case StringExpr stringExpr:
                    return stringExpr.Args.Count == 0;
                default:
                    return false;
            }
        }

        /// <summary>
        /// One subexpression of a node, in the order in which both the full lowering and the generated
        /// C# evaluate them. If <paramref name="Late"/>, the full lowering reads the subexpression when
        /// the node is evaluated (if it is a term, i.e. a variable or a literal) instead of evaluating it
        /// into a temporary in order. If <paramref name="Copy"/>, the value is stored or passed on.
        /// </summary>
        private readonly record struct Operand(IPExpr Expr, bool Late, bool Copy);

        private static IEnumerable<Operand> Arguments(IEnumerable<IPExpr> args) =>
            args.Select(arg => new Operand(arg, false, true));

        /// <summary>
        /// Lowers the operands of a node. <paramref name="fieldReadEarly"/> tells whether the generated C#
        /// has read a field before the operands that the full lowering reads after them, so that calls that
        /// may change fields must be lifted; <paramref name="liftedAfter"/> tells whether the node is
        /// followed by lifted statements that the full lowering evaluates after the operands.
        /// </summary>
        private (List<IPExpr>, List<IPStmt>) LowerOperands(IEnumerable<Operand> operands, bool fieldReadEarly,
            bool liftedAfter = false)
        {
            var specs = operands.ToList();
            var exprs = new List<IPExpr>();
            var operandDeps = new List<List<IPStmt>>();
            var readEarly = fieldReadEarly;
            foreach (var operand in specs)
            {
                (var expr, var deps) = LowerExpression(operand.Expr, readEarly);
                exprs.Add(operand.Copy ? CopyIfNeeded(expr) : expr);
                operandDeps.Add(deps);
                // The full lowering reads a late term after the operands that follow it, the generated C#
                // before them.
                readEarly |= operand.Late && IsField(operand.Expr);
            }

            // The operands before the last one with lifted statements are evaluated before them, as in the
            // full lowering, unless the full lowering reads them late as well.
            var lastLifted = liftedAfter ? specs.Count : operandDeps.FindLastIndex(deps => deps.Count > 0);
            var result = new List<IPStmt>();
            for (var i = 0; i < specs.Count; i++)
            {
                result.AddRange(operandDeps[i]);
                if (i < lastLifted && !IsStable(exprs[i]) && !(specs[i].Late && specs[i].Expr is IExprTerm))
                {
                    (var temp, var store) = LowerToTemporary(exprs[i]);
                    result.Add(store);
                    exprs[i] = temp;
                }
            }

            return (exprs, result);
        }

        private (IPExpr, List<IPStmt>) LowerOperand(IPExpr expr, bool fieldReadEarly)
        {
            (var exprs, var deps) = LowerOperands(new[] { new Operand(expr, true, false) }, fieldReadEarly);
            return (exprs[0], deps);
        }

        private (IPExpr, IPExpr, List<IPStmt>) LowerOperands(IPExpr first, IPExpr second, bool fieldReadEarly)
        {
            (var exprs, var deps) = LowerOperands(new[] { new Operand(first, true, false), new Operand(second, true, false) },
                fieldReadEarly);
            return (exprs[0], exprs[1], deps);
        }

        private (IPExpr, List<IPStmt>) LowerExpression(IPExpr expr, bool fieldReadEarly)
        {
            var location = expr.SourceLocation;
            switch (expr)
            {
                case FunCallExpr funCallExpr:
                {
                    var lift = NeedsCallStatement(funCallExpr.Function) ||
                               (fieldReadEarly && MayChangeFields(funCallExpr.Function));
                    // A lifted call runs before the rest of the statement, so nothing has been read before it.
                    (var args, var deps) = LowerOperands(Arguments(funCallExpr.Arguments), !lift && fieldReadEarly);
                    var call = new FunCallExpr(location, funCallExpr.Function, args);
                    if (!lift)
                    {
                        return (call, deps);
                    }

                    (var temp, var store) = LowerToTemporary(call);
                    deps.Add(store);
                    return (temp, deps);
                }

                case IExprTerm _:
                case DefaultExpr _:
                case NondetExpr _:
                case FairNondetExpr _:
                case ChooseExpr { SubExpr: null }:
                    return (expr, new List<IPStmt>());

                case CtorExpr ctorExpr:
                {
                    (var args, var deps) = LowerOperands(Arguments(ctorExpr.Arguments), fieldReadEarly);
                    return (new CtorExpr(location, ctorExpr.Interface, args), deps);
                }

                case NamedTupleExpr namedTupleExpr:
                {
                    (var fields, var deps) = LowerOperands(Arguments(namedTupleExpr.TupleFields), fieldReadEarly);
                    return (new NamedTupleExpr(location, fields, namedTupleExpr.Type), deps);
                }

                case UnnamedTupleExpr unnamedTupleExpr:
                {
                    (var fields, var deps) = LowerOperands(Arguments(unnamedTupleExpr.TupleFields), fieldReadEarly);
                    return (new UnnamedTupleExpr(location, fields), deps);
                }

                case StringExpr stringExpr:
                {
                    // Formatting only reads its arguments, so they are not copied. The full lowering
                    // evaluates them into temporaries in order.
                    (var args, var deps) = LowerOperands(stringExpr.Args.Select(arg => new Operand(arg, false, false)),
                        fieldReadEarly);
                    return (new StringExpr(location, stringExpr.BaseString, args), deps);
                }

                case BinOpExpr { Operation: BinOpType.And or BinOpType.Or } binOpExpr:
                {
                    // C# && and || short-circuit (PBool defines true, false, & and |), so the right operand
                    // needs an if only if it has lifted statements, which must run only when it is evaluated.
                    (var lhs, var lhsDeps) = LowerExpression(binOpExpr.Lhs, fieldReadEarly);
                    (var rhs, var rhsDeps) = LowerExpression(binOpExpr.Rhs, fieldReadEarly);
                    if (rhsDeps.Count == 0)
                    {
                        return (new BinOpExpr(location, binOpExpr.Operation, lhs, rhs), lhsDeps);
                    }

                    (var temp, var store) = LowerToTemporary(lhs);
                    lhsDeps.Add(store);
                    var assignRhs = new CompoundStmt(location, rhsDeps.Append(new AssignStmt(location, temp, rhs)));
                    lhsDeps.Add(binOpExpr.Operation == BinOpType.And
                        ? new IfStmt(location, temp, assignRhs, null)
                        : new IfStmt(location, temp, new NoStmt(location), assignRhs));
                    return (temp, lhsDeps);
                }

                case BinOpExpr binOpExpr:
                {
                    (var lhs, var rhs, var deps) = LowerOperands(binOpExpr.Lhs, binOpExpr.Rhs, fieldReadEarly);
                    return (new BinOpExpr(location, binOpExpr.Operation, lhs, rhs), deps);
                }

                case CastExpr castExpr:
                {
                    (var sub, var deps) = LowerOperand(castExpr.SubExpr, fieldReadEarly);
                    return (new CastExpr(location, sub, castExpr.Type), deps);
                }

                case CoerceExpr coerceExpr:
                {
                    (var sub, var deps) = LowerOperand(coerceExpr.SubExpr, fieldReadEarly);
                    // The generator writes the subexpression of an interface coercion twice.
                    if (coerceExpr.Type.Canonicalize() is PermissionType && sub is not IExprTerm)
                    {
                        (var temp, var store) = LowerToTemporary(sub);
                        deps.Add(store);
                        sub = temp;
                    }

                    return (new CoerceExpr(location, sub, coerceExpr.NewType), deps);
                }

                case ChooseExpr chooseExpr:
                {
                    (var sub, var deps) = LowerOperand(chooseExpr.SubExpr, fieldReadEarly);
                    return (new ChooseExpr(location, sub, chooseExpr.Type), deps);
                }

                case ContainsExpr containsExpr:
                    return LowerContains(containsExpr, fieldReadEarly);

                case KeysExpr keysExpr:
                {
                    (var sub, var deps) = LowerOperand(keysExpr.Expr, fieldReadEarly);
                    return (new KeysExpr(location, sub, keysExpr.Type), deps);
                }

                case ValuesExpr valuesExpr:
                {
                    (var sub, var deps) = LowerOperand(valuesExpr.Expr, fieldReadEarly);
                    return (new ValuesExpr(location, sub, valuesExpr.Type), deps);
                }

                case SizeofExpr sizeofExpr:
                {
                    (var sub, var deps) = LowerOperand(sizeofExpr.Expr, fieldReadEarly);
                    return (new SizeofExpr(location, sub), deps);
                }

                case UnaryOpExpr unaryOpExpr:
                {
                    (var sub, var deps) = LowerOperand(unaryOpExpr.SubExpr, fieldReadEarly);
                    return (new UnaryOpExpr(location, unaryOpExpr.Operation, sub), deps);
                }

                case MapAccessExpr mapAccessExpr:
                {
                    (var map, var index, var deps) =
                        LowerOperands(mapAccessExpr.MapExpr, mapAccessExpr.IndexExpr, fieldReadEarly);
                    return (new MapAccessExpr(location, map, index, mapAccessExpr.Type), deps);
                }

                case SetAccessExpr setAccessExpr:
                {
                    (var set, var index, var deps) =
                        LowerOperands(setAccessExpr.SetExpr, setAccessExpr.IndexExpr, fieldReadEarly);
                    return (new SetAccessExpr(location, set, index, setAccessExpr.Type), deps);
                }

                case SeqAccessExpr seqAccessExpr:
                {
                    (var seq, var index, var deps) =
                        LowerOperands(seqAccessExpr.SeqExpr, seqAccessExpr.IndexExpr, fieldReadEarly);
                    return (new SeqAccessExpr(location, seq, index, seqAccessExpr.Type), deps);
                }

                case NamedTupleAccessExpr namedTupleAccessExpr:
                {
                    (var sub, var deps) = LowerOperand(namedTupleAccessExpr.SubExpr, fieldReadEarly);
                    return (new NamedTupleAccessExpr(location, sub, namedTupleAccessExpr.Entry), deps);
                }

                case TupleAccessExpr tupleAccessExpr:
                {
                    (var sub, var deps) = LowerOperand(tupleAccessExpr.SubExpr, fieldReadEarly);
                    return (new TupleAccessExpr(location, sub, tupleAccessExpr.FieldNo, tupleAccessExpr.Type), deps);
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(expr));
            }
        }

        /// <summary>
        /// The generated C# evaluates the collection before the item, the full lowering the item first.
        /// </summary>
        private (IPExpr, List<IPStmt>) LowerContains(ContainsExpr containsExpr, bool fieldReadEarly)
        {
            var location = containsExpr.SourceLocation;
            if (containsExpr.Collection is IExprTerm)
            {
                // Both read the collection last; C# reads it first, so a call in the item must not change it.
                (var item, var itemDeps) = LowerExpression(containsExpr.Item,
                    fieldReadEarly || IsField(containsExpr.Collection));
                return (new ContainsExpr(location, item, containsExpr.Collection), itemDeps);
            }

            (var lowItem, var deps) = LowerExpression(containsExpr.Item, fieldReadEarly);
            (var collection, var collectionDeps) = LowerExpression(containsExpr.Collection, fieldReadEarly);
            if (containsExpr.Item is not IExprTerm && !IsStable(lowItem))
            {
                (var temp, var store) = LowerToTemporary(lowItem);
                deps.Add(store);
                lowItem = temp;
            }

            deps.AddRange(collectionDeps);
            return (new ContainsExpr(location, lowItem, collection), deps);
        }

        /// <summary>
        /// Returns the index expressions of an lvalue, in the order in which they are evaluated.
        /// </summary>
        private static List<IPExpr> LvalueIndices(IPExpr lvalue)
        {
            switch (lvalue)
            {
                case MapAccessExpr mapAccessExpr:
                    return LvalueIndices(mapAccessExpr.MapExpr).Append(mapAccessExpr.IndexExpr).ToList();
                case SetAccessExpr setAccessExpr:
                    return LvalueIndices(setAccessExpr.SetExpr).Append(setAccessExpr.IndexExpr).ToList();
                case SeqAccessExpr seqAccessExpr:
                    return LvalueIndices(seqAccessExpr.SeqExpr).Append(seqAccessExpr.IndexExpr).ToList();
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    return LvalueIndices(namedTupleAccessExpr.SubExpr);
                case TupleAccessExpr tupleAccessExpr:
                    return LvalueIndices(tupleAccessExpr.SubExpr);
                case VariableAccessExpr _:
                    return new List<IPExpr>();
                default:
                    throw new ArgumentOutOfRangeException(nameof(lvalue));
            }
        }

        private static IPExpr RebuildLvalue(IPExpr lvalue, Queue<IPExpr> indices)
        {
            var location = lvalue.SourceLocation;
            switch (lvalue)
            {
                case MapAccessExpr mapAccessExpr:
                    var map = RebuildLvalue(mapAccessExpr.MapExpr, indices);
                    return new MapAccessExpr(location, map, indices.Dequeue(), mapAccessExpr.Type);
                case SetAccessExpr setAccessExpr:
                    var set = RebuildLvalue(setAccessExpr.SetExpr, indices);
                    return new SetAccessExpr(location, set, indices.Dequeue(), setAccessExpr.Type);
                case SeqAccessExpr seqAccessExpr:
                    var seq = RebuildLvalue(seqAccessExpr.SeqExpr, indices);
                    return new SeqAccessExpr(location, seq, indices.Dequeue(), seqAccessExpr.Type);
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    return new NamedTupleAccessExpr(location, RebuildLvalue(namedTupleAccessExpr.SubExpr, indices),
                        namedTupleAccessExpr.Entry);
                case TupleAccessExpr tupleAccessExpr:
                    return new TupleAccessExpr(location, RebuildLvalue(tupleAccessExpr.SubExpr, indices),
                        tupleAccessExpr.FieldNo, tupleAccessExpr.Type);
                default:
                    return lvalue;
            }
        }

        private static Variable LvalueRoot(IPExpr lvalue)
        {
            switch (lvalue)
            {
                case MapAccessExpr mapAccessExpr:
                    return LvalueRoot(mapAccessExpr.MapExpr);
                case SetAccessExpr setAccessExpr:
                    return LvalueRoot(setAccessExpr.SetExpr);
                case SeqAccessExpr seqAccessExpr:
                    return LvalueRoot(seqAccessExpr.SeqExpr);
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    return LvalueRoot(namedTupleAccessExpr.SubExpr);
                case TupleAccessExpr tupleAccessExpr:
                    return LvalueRoot(tupleAccessExpr.SubExpr);
                case VariableAccessExpr variableAccessExpr:
                    return variableAccessExpr.Variable;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lvalue));
            }
        }

        /// <summary>
        /// Lowers a statement that changes an lvalue and then evaluates the specified operands. The full
        /// lowering reads the lvalue's variable, and its indices that are terms, when the statement runs;
        /// the generated C# reads the variable first when it changes the collection or value it refers to.
        /// </summary>
        private (IPExpr, List<IPExpr>, List<IPStmt>) LowerWithLvalue(IPExpr lvalue, bool readsRoot,
            IEnumerable<Operand> operands, bool liftedAfter = false)
        {
            var indices = LvalueIndices(lvalue);
            var rootIsField = LvalueRoot(lvalue).Role.HasFlag(VariableRole.Field);
            (var exprs, var deps) = LowerOperands(
                indices.Select(index => new Operand(index, true, false)).Concat(operands),
                readsRoot && rootIsField, liftedAfter);
            var newLvalue = RebuildLvalue(lvalue, new Queue<IPExpr>(exprs.Take(indices.Count)));
            return (newLvalue, exprs.Skip(indices.Count).ToList(), deps);
        }

        private List<IPStmt> LowerStatement(IPStmt statement)
        {
            var location = statement?.SourceLocation;
            switch (statement)
            {
                case null:
                    throw new ArgumentNullException(nameof(statement));

                case AnnounceStmt announceStmt:
                {
                    // The monitor gets the payload as a parameter it may change, so it gets a copy.
                    var operands = new List<Operand> { new Operand(announceStmt.Event, true, false) };
                    if (announceStmt.Payload != null)
                    {
                        operands.Add(new Operand(announceStmt.Payload, false, true));
                    }

                    (var exprs, var deps) = LowerOperands(operands, false);
                    deps.Add(new AnnounceStmt(location, exprs[0], exprs.ElementAtOrDefault(1)));
                    return deps;
                }

                case AssertStmt assertStmt:
                {
                    (var assertion, var deps) = LowerExpression(assertStmt.Assertion, false);
                    (var message, var messageDeps) = LowerExpression(assertStmt.Message, false);
                    if (assertion is BoolLiteralExpr)
                    {
                        return deps.Concat(messageDeps).Append(new AssertStmt(location, assertion, message)).ToList();
                    }

                    // C# evaluates the message of Assert eagerly; build it only when the assertion fails.
                    deps.Add(new IfStmt(location, assertion, new NoStmt(location), new CompoundStmt(location,
                        messageDeps.Append(new AssertStmt(location, new BoolLiteralExpr(location, false), message)))));
                    return deps;
                }

                case AssumeStmt assumeStmt:
                {
                    (var assumption, var deps) = LowerExpression(assumeStmt.Assumption, false);
                    (var message, var messageDeps) = LowerExpression(assumeStmt.Message, false);
                    if (assumption is BoolLiteralExpr)
                    {
                        return deps.Concat(messageDeps).Append(new AssumeStmt(location, assumption, message)).ToList();
                    }

                    deps.Add(new IfStmt(location, assumption, new NoStmt(location), new CompoundStmt(location,
                        messageDeps.Append(new AssumeStmt(location, assumption, message)))));
                    return deps;
                }

                case AssignStmt assignStmt:
                    return LowerAssignment(assignStmt);

                case CompoundStmt compoundStmt:
                    return new List<IPStmt>
                    {
                        new CompoundStmt(location, compoundStmt.Statements.SelectMany(LowerStatement).ToList())
                    };

                case CtorStmt ctorStmt:
                {
                    // The payload becomes the new machine's, so it gets a copy.
                    (var args, var deps) = LowerOperands(Arguments(ctorStmt.Arguments), false);
                    deps.Add(new CtorStmt(location, ctorStmt.Interface, args));
                    return deps;
                }

                case FunCallStmt funCallStmt:
                {
                    // Arguments are passed by value.
                    (var args, var deps) = LowerOperands(Arguments(funCallStmt.ArgsList), false);
                    deps.Add(new FunCallStmt(location, funCallStmt.Function, args));
                    return deps;
                }

                case GotoStmt gotoStmt:
                {
                    if (gotoStmt.Payload == null)
                    {
                        return new List<IPStmt> { gotoStmt };
                    }

                    (var payload, var deps) = LowerExpression(gotoStmt.Payload, false);
                    deps.Add(new GotoStmt(location, gotoStmt.State, CopyIfNeeded(payload)));
                    return deps;
                }

                case IfStmt ifStmt:
                {
                    (var condition, var deps) = LowerExpression(ifStmt.Condition, false);
                    deps.Add(new IfStmt(location, condition,
                        new CompoundStmt(ifStmt.ThenBranch.SourceLocation, LowerStatement(ifStmt.ThenBranch)),
                        new CompoundStmt(ifStmt.ElseBranch.SourceLocation, LowerStatement(ifStmt.ElseBranch))));
                    return deps;
                }

                case AddStmt addStmt:
                {
                    // Collections store copies of the values added to them (PSet.Add, PMap.Add).
                    (var collection, var values, var deps) = LowerWithLvalue(addStmt.Variable, true,
                        new[] { new Operand(addStmt.Value, false, false) });
                    deps.Add(new AddStmt(location, collection, values[0]));
                    return deps;
                }

                case InsertStmt insertStmt:
                {
                    // Collections store copies of the values inserted into them (PSeq.Insert, PMap.Add).
                    (var collection, var values, var deps) = LowerWithLvalue(insertStmt.Variable, true,
                        new[] { new Operand(insertStmt.Index, true, false), new Operand(insertStmt.Value, false, false) });
                    deps.Add(new InsertStmt(location, collection, values[0], values[1]));
                    return deps;
                }

                case RemoveStmt removeStmt:
                {
                    (var collection, var values, var deps) = LowerWithLvalue(removeStmt.Variable, true,
                        new[] { new Operand(removeStmt.Value, true, false) });
                    deps.Add(new RemoveStmt(location, collection, values[0]));
                    return deps;
                }

                case MoveAssignStmt moveAssignStmt:
                {
                    (var destination, _, var deps) = LowerWithLvalue(moveAssignStmt.ToLocation, false,
                        Enumerable.Empty<Operand>());
                    deps.Add(new MoveAssignStmt(location, destination, moveAssignStmt.FromVariable));
                    return deps;
                }

                case NoStmt _:
                    return new List<IPStmt>();

                case PrintStmt printStmt:
                    return LowerPrint(printStmt);

                case RaiseStmt raiseStmt:
                {
                    // The full lowering reads the event after the payload; the payload is copied.
                    (var exprs, var deps) = LowerOperands(
                        new[] { new Operand(raiseStmt.Event, true, false) }.Concat(Arguments(raiseStmt.Payload)), false);
                    var pEvent = exprs[0];
                    var payload = exprs.Skip(1).ToList();
                    if (!IsEventLiteral(pEvent, payload.Count > 0))
                    {
                        // The generator sets the payload on the event and then raises it, so an event
                        // variable is copied first (as the full lowering does).
                        pEvent = new CloneExpr(pEvent);
                        if (payload.Count > 0)
                        {
                            (var eventTemp, var eventStore) = LowerToTemporary(pEvent);
                            deps.Add(eventStore);
                            pEvent = eventTemp;
                        }
                    }

                    deps.Add(new RaiseStmt(location, pEvent, payload));
                    return deps;
                }

                case ReceiveStmt receiveStmt:
                    foreach (var recvCase in receiveStmt.Cases.Values)
                    {
                        IPStmt caseBody = recvCase.Body;
                        recvCase.Body = new CompoundStmt(caseBody.SourceLocation, LowerStatement(caseBody));
                    }

                    return new List<IPStmt> { receiveStmt };

                case ReturnStmt returnStmt:
                {
                    if (returnStmt.ReturnValue == null)
                    {
                        return new List<IPStmt> { returnStmt };
                    }

                    if (returnStmt.ReturnValue is FunCallExpr { Function.IsForeign: false } returnCall)
                    {
                        // The caller checks for a pending transition right after its call, so a returned
                        // call needs no check of its own. (A foreign call is lifted: the generator writes
                        // the foreign-call prologue and epilogue only around statements.)
                        (var args, var argDeps) = LowerOperands(Arguments(returnCall.Arguments), false);
                        argDeps.Add(new ReturnStmt(location,
                            new FunCallExpr(returnCall.SourceLocation, returnCall.Function, args)));
                        return argDeps;
                    }

                    // Callers take the returned value without copying it.
                    (var value, var deps) = LowerExpression(returnStmt.ReturnValue, false);
                    deps.Add(new ReturnStmt(location, CopyIfNeeded(value)));
                    return deps;
                }

                case BreakStmt _:
                case ContinueStmt _:
                    return new List<IPStmt> { statement };

                case SendStmt sendStmt:
                {
                    // The full lowering evaluates the target, the event and the payload in order; the
                    // receiver gets a copy of the payload.
                    var operands = new[] { new Operand(sendStmt.MachineExpr, false, false), new Operand(sendStmt.Evt, false, false) }
                        .Concat(Arguments(sendStmt.Arguments));
                    (var exprs, var deps) = LowerOperands(operands, false);
                    var target = exprs[0];
                    var pEvent = exprs[1];
                    var payload = exprs.Skip(2).ToList();
                    if (!IsEventLiteral(pEvent, payload.Count > 0))
                    {
                        pEvent = new CloneExpr(pEvent);
                        if (payload.Count > 0)
                        {
                            // The generator then writes the payload before the target.
                            if (!IsStable(target))
                            {
                                (var targetTemp, var targetStore) = LowerToTemporary(target);
                                deps.Add(targetStore);
                                target = targetTemp;
                            }

                            (var eventTemp, var eventStore) = LowerToTemporary(pEvent);
                            deps.Add(eventStore);
                            pEvent = eventTemp;
                        }
                    }

                    deps.Add(new SendStmt(location, target, pEvent, payload));
                    return deps;
                }

                case ForeachStmt foreachStmt:
                    return LowerForeach(foreachStmt);

                case WhileStmt whileStmt:
                {
                    (var condition, var conditionDeps) = LowerExpression(whileStmt.Condition, false);
                    var body = LowerStatement(whileStmt.Body);
                    if (conditionDeps.Count == 0)
                    {
                        return new List<IPStmt>
                        {
                            new WhileStmt(location, condition, new CompoundStmt(whileStmt.Body.SourceLocation, body))
                        };
                    }

                    // The lifted statements of the condition run before each test of it.
                    var conditionLocation = whileStmt.Condition.SourceLocation;
                    var loopBody = conditionDeps
                        .Append(new IfStmt(conditionLocation, condition, new NoStmt(conditionLocation),
                            new BreakStmt(conditionLocation)))
                        .Concat(body);
                    return new List<IPStmt>
                    {
                        new WhileStmt(location, new BoolLiteralExpr(location, true),
                            new CompoundStmt(whileStmt.Body.SourceLocation, loopBody))
                    };
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(statement));
            }
        }

        /// <summary>
        /// Can the generator build the event with its payload (<c>new E(payload)</c>), writing the event
        /// expression once? Event variables are copied instead, and the built-in events have no payload.
        /// </summary>
        private static bool IsEventLiteral(IPExpr pEvent, bool hasPayload) =>
            pEvent is EventRefExpr eventRef && !(hasPayload && eventRef.Value.IsBuiltIn);

        private List<IPStmt> LowerAssignment(AssignStmt assignStmt)
        {
            var location = assignStmt.SourceLocation;
            var lvalue = assignStmt.Location;
            // C# reads the variable of an lvalue that is an access before the value, the full lowering after it.
            var readsRoot = lvalue is not VariableAccessExpr;
            if (assignStmt.Value is not FunCallExpr call)
            {
                (var target, var values, var deps) = LowerWithLvalue(lvalue, readsRoot,
                    new[] { new Operand(assignStmt.Value, true, true) });
                deps.Add(new AssignStmt(location, target, values[0]));
                return deps;
            }

            // The generator writes the pending-transition check after the assignment of a call. The call
            // result can be assigned directly if a pending transition leaves only locals behind, and if C#
            // reads no field before the call that the call could change.
            var root = LvalueRoot(lvalue);
            var fieldReadEarly = (readsRoot && root.Role.HasFlag(VariableRole.Field)) ||
                                 LvalueIndices(lvalue).Any(IsField);
            var direct = (!NeedsCallStatement(call.Function) || !root.Role.HasFlag(VariableRole.Field)) &&
                         !(fieldReadEarly && MayChangeFields(call.Function));
            if (direct)
            {
                (var target, var args, var deps) = LowerWithLvalue(lvalue, readsRoot, Arguments(call.Arguments));
                deps.Add(new AssignStmt(location, target, new FunCallExpr(call.SourceLocation, call.Function, args)));
                return deps;
            }

            // Otherwise the call goes into a temporary (after the lvalue's indices, as in the full lowering).
            (var callArgs, var callDeps) = LowerOperands(Arguments(call.Arguments), false);
            (var temp, var store) = LowerToTemporary(new FunCallExpr(call.SourceLocation, call.Function, callArgs));
            (var lowLvalue, _, var lvalueDeps) =
                LowerWithLvalue(lvalue, readsRoot, Enumerable.Empty<Operand>(), liftedAfter: true);
            return lvalueDeps.Concat(callDeps).Append(store).Append(new AssignStmt(location, lowLvalue, temp)).ToList();
        }

        /// <summary>
        /// The generator formats a print message only when tracing is enabled. Arguments that can have
        /// side effects or fail are evaluated before (always); the others are left inline, so that
        /// nothing is formatted when nothing is logged.
        /// </summary>
        private List<IPStmt> LowerPrint(PrintStmt printStmt)
        {
            var location = printStmt.SourceLocation;
            if (printStmt.Message is not StringExpr printString ||
                printString.Args.Any(arg => MayFormatForeignValue(arg.Type)))
            {
                // Formatting may run foreign code, so the message is formatted even when nothing is logged.
                (var message, var deps) = LowerExpression(printStmt.Message, false);
                if (message is not IExprTerm)
                {
                    (var temp, var store) = LowerToTemporary(message);
                    deps.Add(store);
                    message = temp;
                }

                deps.Add(new PrintStmt(location, message));
                return deps;
            }

            // An argument that is left inline is read after the arguments that are evaluated before the
            // print. That is unobservable unless one of them calls a function, which could change it: then
            // (as the full lowering does) every argument that is not a term is evaluated before the print,
            // and a term is copied if a later argument is.
            var hasCall = printString.Args.Any(ContainsCall);
            var args = new List<IPExpr>();
            var allDeps = new List<IPStmt>();
            for (var i = 0; i < printString.Args.Count; i++)
            {
                var arg = printString.Args[i];
                var inline = hasCall
                    ? arg is IExprTerm && !printString.Args.Skip(i + 1).Any(later => later is not IExprTerm)
                    : IsSkippable(arg);
                if (inline)
                {
                    args.Add(arg);
                    continue;
                }

                (var expr, var deps) = LowerExpression(arg, false);
                allDeps.AddRange(deps);
                if (!IsStable(expr))
                {
                    (var temp, var store) = LowerToTemporary(arg is IExprTerm ? CopyIfNeeded(expr) : expr);
                    allDeps.Add(store);
                    expr = temp;
                }

                args.Add(expr);
            }

            allDeps.Add(new PrintStmt(location, new StringExpr(printString.SourceLocation, printString.BaseString, args)));
            return allDeps;
        }

        /// <summary>
        /// Can evaluating the expression be skipped? It must have no side effects (calls, choices) and
        /// must not fail (accesses, casts, division).
        /// </summary>
        private static bool IsSkippable(IPExpr expr)
        {
            switch (expr)
            {
                case VariableAccessExpr _:
                case BoolLiteralExpr _:
                case IntLiteralExpr _:
                case FloatLiteralExpr _:
                case NullLiteralExpr _:
                case EnumElemRefExpr _:
                case EventRefExpr _:
                case ThisRefExpr _:
                case DefaultExpr _:
                    return true;
                case BinOpExpr binOpExpr:
                    return binOpExpr.Operation != BinOpType.Div && binOpExpr.Operation != BinOpType.Mod &&
                           IsSkippable(binOpExpr.Lhs) && IsSkippable(binOpExpr.Rhs);
                case UnaryOpExpr unaryOpExpr:
                    return IsSkippable(unaryOpExpr.SubExpr);
                case SizeofExpr sizeofExpr:
                    return IsSkippable(sizeofExpr.Expr);
                case KeysExpr keysExpr:
                    return IsSkippable(keysExpr.Expr);
                case ValuesExpr valuesExpr:
                    return IsSkippable(valuesExpr.Expr);
                case ContainsExpr containsExpr:
                    return IsSkippable(containsExpr.Item) && IsSkippable(containsExpr.Collection);
                case CoerceExpr coerceExpr:
                    return IsSkippable(coerceExpr.SubExpr);
                default:
                    return false;
            }
        }

        private static IEnumerable<IPExpr> Subexpressions(IPExpr expr)
        {
            switch (expr)
            {
                case FunCallExpr e: return e.Arguments;
                case CtorExpr e: return e.Arguments;
                case NamedTupleExpr e: return e.TupleFields;
                case UnnamedTupleExpr e: return e.TupleFields;
                case StringExpr e: return e.Args;
                case BinOpExpr e: return new[] { e.Lhs, e.Rhs };
                case CastExpr e: return new[] { e.SubExpr };
                case CoerceExpr e: return new[] { e.SubExpr };
                case ChooseExpr { SubExpr: not null } e: return new[] { e.SubExpr };
                case ContainsExpr e: return new[] { e.Item, e.Collection };
                case KeysExpr e: return new[] { e.Expr };
                case ValuesExpr e: return new[] { e.Expr };
                case SizeofExpr e: return new[] { e.Expr };
                case UnaryOpExpr e: return new[] { e.SubExpr };
                case MapAccessExpr e: return new[] { e.MapExpr, e.IndexExpr };
                case SetAccessExpr e: return new[] { e.SetExpr, e.IndexExpr };
                case SeqAccessExpr e: return new[] { e.SeqExpr, e.IndexExpr };
                case NamedTupleAccessExpr e: return new[] { e.SubExpr };
                case TupleAccessExpr e: return new[] { e.SubExpr };
                default: return Enumerable.Empty<IPExpr>();
            }
        }

        private static bool ContainsCall(IPExpr expr) => expr is FunCallExpr || Subexpressions(expr).Any(ContainsCall);

        /// <summary>
        /// The generated C# iterates over the collection natively. It iterates over a snapshot, as the
        /// full lowering does, unless the collection is fresh: the body may change the collection.
        /// </summary>
        private List<IPStmt> LowerForeach(ForeachStmt foreachStmt)
        {
            var location = foreachStmt.SourceLocation;
            (var collection, var deps) = LowerExpression(foreachStmt.IterCollection, false);
            if (!IsFresh(collection))
            {
                collection = new CloneExpr(collection);
            }

            var body = LowerStatement(foreachStmt.Body);
            var item = foreachStmt.Item;
            // The elements of a set are frozen (they are its keys), and the full lowering gives the item a
            // copy of each. Copy the item only if the function changes it in place.
            if (collection.Type.Canonicalize() is SetType setType && !IsImmutable(setType.ElementType) &&
                ChangesInPlace(originalBody, item))
            {
                var itemAccess = new VariableAccessExpr(location, item);
                body.Insert(0, new AssignStmt(location, itemAccess, new CloneExpr(itemAccess)));
            }

            deps.Add(new ForeachStmt(location, item, collection, new CompoundStmt(foreachStmt.Body.SourceLocation, body),
                foreachStmt.Invariants));
            return deps;
        }

        /// <summary>
        /// Does the statement change the value of the variable in place (rather than assign it)?
        /// </summary>
        private static bool ChangesInPlace(IPStmt statement, Variable variable)
        {
            bool IsRootedAtVariable(IPExpr lvalue) => LvalueRoot(lvalue) == variable;

            switch (statement)
            {
                case CompoundStmt compoundStmt:
                    return compoundStmt.Statements.Any(s => ChangesInPlace(s, variable));
                case IfStmt ifStmt:
                    return ChangesInPlace(ifStmt.ThenBranch, variable) ||
                           (ifStmt.ElseBranch != null && ChangesInPlace(ifStmt.ElseBranch, variable));
                case WhileStmt whileStmt:
                    return ChangesInPlace(whileStmt.Body, variable);
                case ForeachStmt foreachStmt:
                    return ChangesInPlace(foreachStmt.Body, variable);
                case ReceiveStmt receiveStmt:
                    return receiveStmt.Cases.Values.Any(c => ChangesInPlace(c.Body, variable));
                case AssignStmt assignStmt:
                    return assignStmt.Location is not VariableAccessExpr && IsRootedAtVariable(assignStmt.Location);
                case MoveAssignStmt moveAssignStmt:
                    return moveAssignStmt.ToLocation is not VariableAccessExpr &&
                           IsRootedAtVariable(moveAssignStmt.ToLocation);
                case AddStmt addStmt:
                    return IsRootedAtVariable(addStmt.Variable);
                case InsertStmt insertStmt:
                    return IsRootedAtVariable(insertStmt.Variable);
                case RemoveStmt removeStmt:
                    return IsRootedAtVariable(removeStmt.Variable);
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Lowers every function of a compilation unit for one backend at a time. The lowering replaces
    /// function bodies in place, and the backends of one job need different lowerings, so the original
    /// bodies (and the lowered bodies of each mode) are kept and swapped in.
    /// </summary>
    internal sealed class ScopeLowering
    {
        private readonly Scope scope;
        private readonly List<Function> functions;
        private readonly Dictionary<LoweringMode, State> lowered = new Dictionary<LoweringMode, State>();
        private readonly State original;
        private LoweringMode? current;

        public ScopeLowering(Scope scope)
        {
            this.scope = scope;
            functions = scope.GetAllMethods().ToList();
            // Receive cases are functions too; their bodies are lowered in place.
            var receiveCases = new List<Function>();
            foreach (var function in functions.Where(f => f.Body != null))
            {
                CollectReceiveCases(function.Body, receiveCases);
            }

            functions.AddRange(receiveCases);
            original = Save();
        }

        public static LoweringMode ModeFor(CompilerOutput output) =>
            output == CompilerOutput.PChecker ? LoweringMode.CSharp : LoweringMode.Full;

        public void LowerFor(CompilerOutput output)
        {
            var mode = ModeFor(output);
            if (current == mode)
            {
                return;
            }

            if (lowered.TryGetValue(mode, out var state))
            {
                Restore(state);
            }
            else
            {
                Restore(original);
                var foreignCallers = mode == LoweringMode.CSharp ? IRTransformer.ComputeForeignCallers(scope) : null;
                foreach (var function in scope.GetAllMethods())
                {
                    IRTransformer.SimplifyMethod(function, mode, foreignCallers);
                }

                lowered[mode] = Save();
            }

            current = mode;
        }

        private sealed class State
        {
            public readonly Dictionary<Function, (CompoundStmt Body, List<Variable> Locals)> Functions =
                new Dictionary<Function, (CompoundStmt, List<Variable>)>();
        }

        private State Save()
        {
            var state = new State();
            foreach (var function in functions)
            {
                state.Functions[function] = (function.Body, function.LocalVariables.ToList());
            }

            return state;
        }

        private void Restore(State state)
        {
            foreach (var (function, (body, locals)) in state.Functions)
            {
                function.Body = body;
                foreach (var local in function.LocalVariables.ToList())
                {
                    function.RemoveLocalVariable(local);
                }

                function.AddLocalVariables(locals);
            }
        }

        private static void CollectReceiveCases(IPStmt statement, List<Function> result)
        {
            switch (statement)
            {
                case CompoundStmt compoundStmt:
                    foreach (var s in compoundStmt.Statements)
                    {
                        CollectReceiveCases(s, result);
                    }

                    break;
                case IfStmt ifStmt:
                    CollectReceiveCases(ifStmt.ThenBranch, result);
                    if (ifStmt.ElseBranch != null)
                    {
                        CollectReceiveCases(ifStmt.ElseBranch, result);
                    }

                    break;
                case WhileStmt whileStmt:
                    CollectReceiveCases(whileStmt.Body, result);
                    break;
                case ForeachStmt foreachStmt:
                    CollectReceiveCases(foreachStmt.Body, result);
                    break;
                case ReceiveStmt receiveStmt:
                    foreach (var recvCase in receiveStmt.Cases.Values)
                    {
                        result.Add(recvCase);
                        CollectReceiveCases(recvCase.Body, result);
                    }

                    break;
            }
        }
    }
}
