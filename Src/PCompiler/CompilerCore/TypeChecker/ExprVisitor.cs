using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime.Misc;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker
{
    /// <summary>
    /// Visits expression parse nodes and produces typed <see cref="IPExpr"/> AST.
    ///
    /// Multi-error type checking (Phase 2): every error site reports through
    /// <c>handler.Diagnostics</c> and returns an <see cref="ErrorExpr"/>
    /// instead of throwing. In strict mode (default), Report re-throws so
    /// behavior is unchanged. In collecting mode, ErrorExpr propagates up the
    /// expression tree and cascade-suppression rules in
    /// <see cref="TypeCheckingUtils.CheckAssignable"/> stop one upstream error
    /// from generating a chain of downstream "incompatible type" diagnostics.
    ///
    /// Conventions adopted per visit method:
    ///   1. Visit children first.
    ///   2. If any child's <c>Type</c> is <see cref="ErrorType"/>, return
    ///      <c>new ErrorExpr(context)</c> without further checks.
    ///   3. Each original `throw handler.X(...)` becomes
    ///      `Report(handler.X(...)); return new ErrorExpr(context);` — and
    ///      remaining type-compatibility checks route through
    ///      <see cref="TypeCheckingUtils.CheckAssignable"/>.
    /// </summary>
    public class ExprVisitor : PParserBaseVisitor<IPExpr>
    {
        private readonly ITranslationErrorHandler handler;
        private readonly Function method;
        private readonly bool isPVerifier;
        private Scope table;

        public ExprVisitor(Function method, ITranslationErrorHandler handler, bool isPVerifier = false)
        {
            table = method.Scope;
            this.method = method;
            this.handler = handler;
            this.isPVerifier = isPVerifier;
        }

        public ExprVisitor(Scope scope, ITranslationErrorHandler handler, bool isPVerifier = false)
        {
            table = scope;
            this.handler = handler;
            this.isPVerifier = isPVerifier;
        }

        public override IPExpr VisitPrimitiveExpr(PParser.PrimitiveExprContext context)
        {
            return Visit(context.primitive());
        }

        public override IPExpr VisitUnnamedTupleExpr(PParser.UnnamedTupleExprContext context)
        {
            return Visit(context.unnamedTupleBody());
        }

        public override IPExpr VisitNamedTupleExpr(PParser.NamedTupleExprContext context)
        {
            return Visit(context.namedTupleBody());
        }

        public override IPExpr VisitParenExpr(PParser.ParenExprContext context)
        {
            return Visit(context.expr());
        }

        public override IPExpr VisitNamedTupleAccessExpr(PParser.NamedTupleAccessExprContext context)
        {
            IPExpr subExpr = Visit(context.expr());
            if (subExpr.Type is ErrorType) return new ErrorExpr(context);

            var fieldName = context.field.GetText();

            switch (subExpr.Type.Canonicalize())
            {
                case NamedTupleType tuple:
                    if (!tuple.LookupEntry(fieldName, out var entry))
                    {
                        handler.Diagnostics.Report(handler.MissingNamedTupleEntry(context.field, tuple));
                        return new ErrorExpr(context);
                    }

                    return new NamedTupleAccessExpr(context, subExpr, entry);

                case PermissionType {Origin: Machine} permission when isPVerifier:
                    var machine = (Machine) permission.Origin;

                    if (!machine.LookupEntry(fieldName, out var field))
                    {
                        handler.Diagnostics.Report(handler.MissingMachineField(context.field, machine));
                        return new ErrorExpr(context);
                    }
                    return new MachineAccessExpr(context, machine, subExpr, field);

                case PermissionType {Origin: Interface} permission when isPVerifier:
                    var pname = permission.Origin.Name;

                    if (!table.Lookup(pname, out Machine m))
                    {
                        handler.Diagnostics.Report(handler.TypeMismatch(subExpr, [TypeKind.NamedTuple, TypeKind.Base]));
                        return new ErrorExpr(context);
                    }

                    if (!m.LookupEntry(fieldName, out var mfield))
                    {
                        handler.Diagnostics.Report(handler.MissingMachineField(context.field, m));
                        return new ErrorExpr(context);
                    }
                    return new MachineAccessExpr(context, m, subExpr, mfield);

                case PermissionType {Origin: NamedEventSet} permission when isPVerifier:

                    var pevents = ((NamedEventSet)permission.Origin).Events.ToList();

                    foreach (var pevent in pevents)
                    {
                        switch (pevent.PayloadType.Canonicalize())
                        {
                            case NamedTupleType namedTupleType:
                                if (namedTupleType.LookupEntry(fieldName, out var pentry))
                                {
                                    return new EventAccessExpr(context, pevent, subExpr, pentry);
                                }
                                break;
                        }
                    }

                    handler.Diagnostics.Report(handler.MissingEventField(context.field, pevents.First()));
                    return new ErrorExpr(context);

                case PrimitiveType pt when pt.IsSameTypeAs(PrimitiveType.Machine) && isPVerifier:
                    Machine spec;

                    switch (subExpr)
                    {
                        case SpecRefExpr specRefExpr:
                            spec = specRefExpr.Value;
                            break;
                        default:
                            handler.Diagnostics.Report(handler.TypeMismatch(subExpr, [TypeKind.NamedTuple, TypeKind.Base]));
                            return new ErrorExpr(context);
                    }

                    if (!spec.LookupEntry(fieldName, out var sfield))
                    {
                        handler.Diagnostics.Report(handler.MissingMachineField(context.field, spec));
                        return new ErrorExpr(context);
                    }
                    return new SpecAccessExpr(context, spec, subExpr, sfield);

                default:
                    handler.Diagnostics.Report(handler.TypeMismatch(subExpr, [TypeKind.NamedTuple, TypeKind.Base]));
                    return new ErrorExpr(context);
            }
        }

        public override IPExpr VisitTupleAccessExpr(PParser.TupleAccessExprContext context)
        {
            var subExpr = Visit(context.expr());
            if (subExpr.Type is ErrorType) return new ErrorExpr(context);

            if (!LiteralParsingUtils.TryParseIntLiteral(context.field.GetText(), out var fieldNo))
            {
                handler.Diagnostics.Report(handler.ValueOutOfRange(context, "int"));
                return new ErrorExpr(context);
            }
            if (!(subExpr.Type.Canonicalize() is TupleType tuple))
            {
                handler.Diagnostics.Report(handler.TypeMismatch(subExpr, TypeKind.Tuple, TypeKind.NamedTuple));
                return new ErrorExpr(context);
            }

            if (fieldNo >= tuple.Types.Count)
            {
                handler.Diagnostics.Report(handler.OutOfBoundsTupleAccess(context.field, tuple));
                return new ErrorExpr(context);
            }

            return new TupleAccessExpr(context, subExpr, fieldNo, tuple.Types[fieldNo]);
        }

        public override IPExpr VisitSeqAccessExpr(PParser.SeqAccessExprContext context)
        {
            var seqOrMap = Visit(context.seq);
            var indexExpr = Visit(context.index);
            // Combiner rule: if either operand carries an upstream error, the
            // result is also ErrorType and we don't add a new diagnostic.
            if (seqOrMap.Type is ErrorType || indexExpr.Type is ErrorType) return new ErrorExpr(context);

            switch (seqOrMap.Type.Canonicalize())
            {
                case SequenceType seqType:
                    if (!TypeCheckingUtils.CheckAssignable(handler, context.index, PrimitiveType.Int, indexExpr))
                    {
                        return new ErrorExpr(context);
                    }

                    return new SeqAccessExpr(context, seqOrMap, indexExpr, seqType.ElementType);

                case MapType mapType:
                    if (!TypeCheckingUtils.CheckAssignable(handler, context.index, mapType.KeyType, indexExpr))
                    {
                        return new ErrorExpr(context);
                    }

                    return new MapAccessExpr(context, seqOrMap, indexExpr, mapType.ValueType);

                case SetType setType:
                    if (!TypeCheckingUtils.CheckAssignable(handler, context.index, PrimitiveType.Int, indexExpr))
                    {
                        return new ErrorExpr(context);
                    }

                    return new SetAccessExpr(context, seqOrMap, indexExpr, setType.ElementType);
            }

            handler.Diagnostics.Report(handler.TypeMismatch(seqOrMap, TypeKind.Sequence, TypeKind.Map));
            return new ErrorExpr(context);
        }

        public override IPExpr VisitKeywordExpr(PParser.KeywordExprContext context)
        {
            switch (context.fun.Text)
            {
                case "keys":
                {
                    var expr = Visit(context.expr());
                    if (expr.Type is ErrorType) return new ErrorExpr(context);
                    if (!(expr.Type.Canonicalize() is MapType mapType))
                    {
                        handler.Diagnostics.Report(handler.TypeMismatch(expr, TypeKind.Map));
                        return new ErrorExpr(context);
                    }

                    return new KeysExpr(context, expr, new SequenceType(mapType.KeyType));
                }
                case "values":
                {
                    var expr = Visit(context.expr());
                    if (expr.Type is ErrorType) return new ErrorExpr(context);
                    if (!(expr.Type.Canonicalize() is MapType mapType))
                    {
                        handler.Diagnostics.Report(handler.TypeMismatch(expr, TypeKind.Map));
                        return new ErrorExpr(context);
                    }

                    return new ValuesExpr(context, expr, new SequenceType(mapType.ValueType));
                }
                case "sizeof":
                {
                    var expr = Visit(context.expr());
                    if (expr.Type is ErrorType) return new ErrorExpr(context);
                    if (!(expr.Type.Canonicalize() is SequenceType)
                        && !(expr.Type.Canonicalize() is MapType)
                        && !(expr.Type.Canonicalize() is SetType))
                    {
                        handler.Diagnostics.Report(handler.TypeMismatch(expr, TypeKind.Map, TypeKind.Sequence, TypeKind.Set));
                        return new ErrorExpr(context);
                    }
                    return new SizeofExpr(context, expr);
                }
                case "default":
                {
                    var type = TypeResolver.ResolveType(context.type(), table, handler);
                    return new DefaultExpr(context, type.Canonicalize());
                }
                default:
                {
                    handler.Diagnostics.Report(handler.InternalError(context,
                        new ArgumentException($"Unknown keyword expression {context.fun.Text}", nameof(context))));
                    return new ErrorExpr(context);
                }
            }
        }

        public override IPExpr VisitCtorExpr(PParser.CtorExprContext context)
        {
            var interfaceName = context.interfaceName.GetText();
            // Always visit the argument list so internal arg errors surface,
            // even if the interface itself is unknown.
            var arguments = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), this).ToArray();

            if (!table.Lookup(interfaceName, out Interface @interface))
            {
                handler.Diagnostics.Report(handler.MissingDeclaration(context.interfaceName, "interface", interfaceName));
                return new ErrorExpr(context);
            }

            if (method.Owner?.IsSpec == true)
            {
                handler.Diagnostics.Report(handler.IllegalMonitorOperation(context, context.NEW().Symbol, method.Owner));
                return new ErrorExpr(context);
            }

            TypeCheckingUtils.ValidatePayloadTypes(handler, context, @interface.PayloadType, arguments);

            method.CanCreate = true;

            return new CtorExpr(context, @interface, arguments);
        }

        public override IPExpr VisitFunCallExpr(PParser.FunCallExprContext context)
        {
            var funName = context.fun.GetText();
            // Visit arguments first so their internal errors surface regardless
            // of whether the callee resolves. Argument errors do not block
            // dispatch — we still try to bind a function/pure with the args we
            // got, treating ErrorExpr args as compatible with anything.
            var arguments = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), this).ToArray();

            if (table.Lookup(funName, out Function function))
            {
                if (function.Signature.Parameters.Count != arguments.Length)
                {
                    handler.Diagnostics.Report(handler.IncorrectArgumentCount(context, arguments.Length, function.Signature.Parameters.Count));
                    return new ErrorExpr(context);
                }

                for (var i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    var paramType = function.Signature.Parameters[i].Type;
                    TypeCheckingUtils.CheckAssignable(handler, context.rvalueList().rvalue(i), paramType, argument);
                }

                method.AddCallee(function);
                return new FunCallExpr(context, function, arguments);
            }
            if (table.Lookup(funName, out Pure pure))
            {
                if (pure.Signature.Parameters.Count != arguments.Length)
                {
                    handler.Diagnostics.Report(handler.IncorrectArgumentCount(context, arguments.Length, pure.Signature.Parameters.Count));
                    return new ErrorExpr(context);
                }

                for (var i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    var paramType = pure.Signature.Parameters[i].Type;
                    // Suppress cascade if the arg already errored upstream.
                    if (argument.Type is ErrorType) continue;
                    if (!paramType.IsAssignableFrom(argument.Type))
                    {
                        switch (paramType)
                        {
                            case PrimitiveType pt when pt.IsSameTypeAs(PrimitiveType.Event):
                                switch (argument.Type)
                                {
                                    case PermissionType {Origin: NamedEventSet} per when ((NamedEventSet)(per.Origin)).Events.Count() == 1:
                                        continue;
                                }
                                break;
                        }
                        handler.Diagnostics.Report(handler.TypeMismatch(context.rvalueList().rvalue(i), argument.Type, paramType));
                    }
                }

                return new PureCallExpr(context, pure, arguments);
            }

            handler.Diagnostics.Report(handler.MissingDeclaration(context.fun, "function", funName));
            return new ErrorExpr(context);
        }

        public override IPExpr VisitUnaryExpr(PParser.UnaryExprContext context)
        {
            var subExpr = Visit(context.expr());
            if (subExpr.Type is ErrorType) return new ErrorExpr(context);

            switch (context.op.Text)
            {
                case "-":
                    if (!PrimitiveType.Int.IsAssignableFrom(subExpr.Type) &&
                        !PrimitiveType.Float.IsAssignableFrom(subExpr.Type))
                    {
                        handler.Diagnostics.Report(handler.TypeMismatch(context.expr(),
                            subExpr.Type,
                            PrimitiveType.Int,
                            PrimitiveType.Float));
                        return new ErrorExpr(context);
                    }

                    return new UnaryOpExpr(context, UnaryOpType.Negate, subExpr);

                case "!":
                    if (!PrimitiveType.Bool.IsAssignableFrom(subExpr.Type))
                    {
                        handler.Diagnostics.Report(handler.TypeMismatch(context.expr(), subExpr.Type, PrimitiveType.Bool));
                        return new ErrorExpr(context);
                    }

                    return new UnaryOpExpr(context, UnaryOpType.Not, subExpr);

                default:
                    handler.Diagnostics.Report(handler.InternalError(context,
                        new ArgumentException($"Unknown unary op `{context.op.Text}`", nameof(context))));
                    return new ErrorExpr(context);
            }
        }

        public override IPExpr VisitQuantExpr(PParser.QuantExprContext context)
        {
            var oldTable = table;
            table = table.MakeChildScope();

            bool diff = context.diff != null;

            var bound = context.bound.funParam().Select(p =>
            {
                var symbolName = p.name.GetText();
                var param = table.Put(symbolName, p, VariableRole.Param);
                param.Type = TypeResolver.ResolveType(p.type(), table, handler);
                return param;
            }).Cast<Variable>().ToArray();

            if (diff && bound.ToList().Count != 1)
            {
                // we have the "new" annotation so the bound must be a single thing and it must be an event
                handler.Diagnostics.Report(handler.InternalError(context, new ArgumentException($"Difference quantifiers must have exactly one bound variable", nameof(context))));
                // Still visit the body so internal errors surface in collecting mode.
                Visit(context.body);
                table = oldTable;
                return new ErrorExpr(context);
            }

            if (diff)
            {
                // Bail silently if the bound's type itself failed to resolve
                // (TypeResolver may have already reported MissingDeclaration on
                // an undeclared bound type). Without this guard the switch
                // below emits a spurious "expected Event" diagnostic for the
                // same root cause.
                if (bound[0].Type is ErrorType)
                {
                    Visit(context.body);
                    table = oldTable;
                    return new ErrorExpr(context);
                }
                switch (bound[0].Type.Canonicalize())
                {
                    case PrimitiveType pt when pt.IsSameTypeAs(PrimitiveType.Event):
                        break;
                    case PermissionType {Origin: NamedEventSet} _:
                        break;
                    default:
                        handler.Diagnostics.Report(handler.TypeMismatch(context.bound, bound[0].Type, PrimitiveType.Event));
                        // Visit body for nested-error coverage.
                        Visit(context.body);
                        table = oldTable;
                        return new ErrorExpr(context);
                }
            }

            var body = Visit(context.body);

            table = oldTable;

            if (context.quant.Text == "forall")
            {
                return new QuantExpr(context, QuantType.Forall, bound.ToList(), body, diff);
            }

            return new QuantExpr(context, QuantType.Exists, bound.ToList(), body, diff);
        }

        public override IPExpr VisitTestExpr(PParser.TestExprContext context)
        {
            var instance = Visit(context.instance);
            string name = context.kind.GetText();

            // Resolve the kind identifier first so a missing-declaration error
            // is still reported even when the instance errored upstream. Only
            // bail to ErrorExpr after the lookup has had its chance to fire.
            //
            // The State branch uses TryResolveStateForInstance (PR #963) — NOT
            // the bare table.Lookup — so that a same-named state from an
            // unrelated machine doesn't satisfy `hasKind` and then silently
            // fall through to the "unreachable" branch below (it really WOULD
            // become reachable in that case, masking the missing-decl).
            var hasKind = table.Lookup(name, out Machine m)
                          || table.Lookup(name, out Event _)
                          || TryResolveStateForInstance(instance, name, out State _);
            if (!hasKind)
            {
                handler.Diagnostics.Report(handler.MissingDeclaration(context, "machine, event, or state", name));
                return new ErrorExpr(context);
            }

            // Propagate ErrorType: if the instance failed upstream, don't
            // construct a bool-typed TestExpr (which would mask the cascade).
            if (instance.Type is ErrorType) return new ErrorExpr(context);

            if (table.Lookup(name, out m))
            {
                return new TestExpr(context, instance, m);
            }

            if (table.Lookup(name, out Event e))
            {
                return new TestExpr(context, instance, e);
            }

            // Phase 2 + PR #963 reconciled: use the narrowed state lookup
            // (TryResolveStateForInstance, from #963) so a same-named state
            // from an unrelated machine can't be bound silently, AND on miss
            // use Report+ErrorExpr recovery (Phase 2) so collecting mode
            // continues to gather independent errors.
            if (TryResolveStateForInstance(instance, name, out State s))
            {
                return new TestExpr(context, instance, s);
            }

            // Unreachable: hasKind above guarantees at least one lookup succeeds.
            return new ErrorExpr(context);
        }

        // Resolves the state for an `x is &lt;State&gt;` test. State names are only unique within a
        // machine, so the bare cross-machine Scope.Lookup(out State) could return a same-named
        // state from an unrelated machine. When the instance's static type identifies a specific
        // machine, look the state up within THAT machine — and only that machine. Falling back
        // to the global lookup when the owner is statically known would re-introduce exactly
        // the cross-machine collision this fix exists to prevent (e.g. `myA is S2` where `myA`
        // is a MachineA reference but S2 lives in MachineB).
        //
        // The cross-machine global lookup remains the fallback only for the genuinely-untyped
        // case — when no `Machine` owner can be derived from the instance's static type, e.g.
        // an `any`-typed lvalue or a non-machine PermissionType.
        private bool TryResolveStateForInstance(IPExpr instance, string name, out State state)
        {
            var owner = instance switch
            {
                SpecRefExpr specRef => specRef.Value,
                _ => (instance.Type.Canonicalize() as PermissionType)?.Origin as Machine,
            };

            if (owner != null)
            {
                // Statically known owner: the state must belong to this machine.
                // If it doesn't, return false so the caller emits MissingDeclaration —
                // do NOT consult the global table.
                return owner.Scope.Get(name, out state);
            }

            return table.Lookup(name, out state);
        }

        public override IPExpr VisitTargetsExpr(PParser.TargetsExprContext context)
        {
            var instance = Visit(context.instance);
            var target = Visit(context.target);
            // Propagate cascade suppression from either operand.
            if (instance.Type is ErrorType || target.Type is ErrorType) return new ErrorExpr(context);

            // TODO: type check to make sure instance is an event and machine is a machine
            return new TargetsExpr(context, instance, target);
        }

        public override IPExpr VisitFlyingExpr(PParser.FlyingExprContext context)
        {
            var instance = Visit(context.instance);
            if (instance.Type is ErrorType) return new ErrorExpr(context);

            // TODO: type check to make sure instance is an event
            return new FlyingExpr(context, instance);
        }

        public override IPExpr VisitSentExpr(PParser.SentExprContext context)
        {
            var instance = Visit(context.instance);
            if (instance.Type is ErrorType) return new ErrorExpr(context);

            // TODO: type check to make sure instance is an event
            return new SentExpr(context, instance);
        }

        public override IPExpr VisitBinExpr(PParser.BinExprContext context)
        {
            var lhs = Visit(context.lhs);
            var rhs = Visit(context.rhs);
            var op = context.op.Text;
            // Combiner rule: a single guard at the top of VisitBinExpr handles
            // *all* 14 throws inside the switch, since any binary operation on
            // an upstream-errored operand cannot meaningfully type-check.
            if (lhs.Type is ErrorType || rhs.Type is ErrorType) return new ErrorExpr(context);

            var arithCtors = new Dictionary<string, Func<IPExpr, IPExpr, IPExpr>>
            {
                {"*", (elhs, erhs) => new BinOpExpr(context, BinOpType.Mul, elhs, erhs)},
                {"/", (elhs, erhs) => new BinOpExpr(context, BinOpType.Div, elhs, erhs)},
                {"%", (elhs, erhs) => new BinOpExpr(context, BinOpType.Mod, elhs, erhs)},
                {"+", (elhs, erhs) => new BinOpExpr(context, BinOpType.Add, elhs, erhs)},
                {"-", (elhs, erhs) => new BinOpExpr(context, BinOpType.Sub, elhs, erhs)},
                {"<", (elhs, erhs) => new BinOpExpr(context, BinOpType.Lt, elhs, erhs)},
                {"<=", (elhs, erhs) => new BinOpExpr(context, BinOpType.Le, elhs, erhs)},
                {">", (elhs, erhs) => new BinOpExpr(context, BinOpType.Gt, elhs, erhs)},
                {">=", (elhs, erhs) => new BinOpExpr(context, BinOpType.Ge, elhs, erhs)}
            };

            var logicCtors = new Dictionary<string, Func<IPExpr, IPExpr, IPExpr>>
            {
                {"&&", (elhs, erhs) => new BinOpExpr(context, BinOpType.And, elhs, erhs)},
                {"||", (elhs, erhs) => new BinOpExpr(context, BinOpType.Or, elhs, erhs)},
                {"==>", (elhs, erhs) => new BinOpExpr(context, BinOpType.Then, elhs, erhs)},
                {"<==>", (elhs, erhs) => new BinOpExpr(context, BinOpType.Iff, elhs, erhs)}
            };

            var compCtors = new Dictionary<string, Func<IPExpr, IPExpr, IPExpr>>
            {
                {"==", (elhs, erhs) => new BinOpExpr(context, BinOpType.Eq, elhs, erhs)},
                {"!=", (elhs, erhs) => new BinOpExpr(context, BinOpType.Neq, elhs, erhs)}
            };

            switch (op)
            {
                case "+":
                    if (PrimitiveType.String.IsAssignableFrom(lhs.Type) &&
                        PrimitiveType.String.IsAssignableFrom(rhs.Type) ||
                        PrimitiveType.Int.IsAssignableFrom(lhs.Type) &&
                        PrimitiveType.Int.IsAssignableFrom(rhs.Type) ||
                        PrimitiveType.Float.IsAssignableFrom(lhs.Type) &&
                        PrimitiveType.Float.IsAssignableFrom(rhs.Type))
                    {
                        return arithCtors[op](lhs, rhs);
                    }
                    handler.Diagnostics.Report(handler.BinOpTypeMismatch(context, lhs.Type, rhs.Type));
                    return new ErrorExpr(context);
                case "*":
                case "/":
                case "-":
                case "<":
                case ">":
                case ">=":
                case "<=":
                    if (PrimitiveType.Int.IsAssignableFrom(lhs.Type) &&
                        PrimitiveType.Int.IsAssignableFrom(rhs.Type) ||
                        PrimitiveType.Float.IsAssignableFrom(lhs.Type) &&
                        PrimitiveType.Float.IsAssignableFrom(rhs.Type) ||
                        PrimitiveType.String.IsAssignableFrom(lhs.Type) &&
                        PrimitiveType.String.IsAssignableFrom(rhs.Type))
                    {
                        return arithCtors[op](lhs, rhs);
                    }
                    handler.Diagnostics.Report(handler.BinOpTypeMismatch(context, lhs.Type, rhs.Type));
                    return new ErrorExpr(context);
                case "%":
                    if (PrimitiveType.Int.IsAssignableFrom(lhs.Type) &&
                        PrimitiveType.Int.IsAssignableFrom(rhs.Type) ||
                        PrimitiveType.Float.IsAssignableFrom(lhs.Type) &&
                        PrimitiveType.Float.IsAssignableFrom(rhs.Type))
                    {
                        return arithCtors[op](lhs, rhs);
                    }
                    handler.Diagnostics.Report(handler.IncomparableTypes(context, lhs.Type, rhs.Type));
                    return new ErrorExpr(context);
                case "in":
                    var rhsType = rhs.Type.Canonicalize();
                    if (rhsType is MapType rhsMap)
                    {
                        if (!TypeCheckingUtils.CheckAssignable(handler, context.lhs, rhsMap.KeyType, lhs))
                        {
                            return new ErrorExpr(context);
                        }
                    }
                    else if (rhsType is SequenceType rhsSeq)
                    {
                        if (!TypeCheckingUtils.CheckAssignable(handler, context.lhs, rhsSeq.ElementType, lhs))
                        {
                            return new ErrorExpr(context);
                        }
                    }
                    else if (rhsType is SetType rhsSet)
                    {
                        if (!TypeCheckingUtils.CheckAssignable(handler, context.lhs, rhsSet.ElementType, lhs))
                        {
                            return new ErrorExpr(context);
                        }
                    }
                    else
                    {
                        handler.Diagnostics.Report(handler.TypeMismatch(rhs, TypeKind.Map, TypeKind.Sequence));
                        return new ErrorExpr(context);
                    }
                    return new ContainsExpr(context, lhs, rhs);

                case "==":
                case "!=":
                    if (!lhs.Type.IsAssignableFrom(rhs.Type) && !rhs.Type.IsAssignableFrom(lhs.Type))
                    {
                        handler.Diagnostics.Report(handler.IncomparableTypes(context, lhs.Type, rhs.Type));
                        return new ErrorExpr(context);
                    }

                    return compCtors[op](lhs, rhs);

                case "&&":
                case "||":
                case "==>":
                case "<==>":
                    if (!TypeCheckingUtils.CheckAssignable(handler, context.lhs, PrimitiveType.Bool, lhs))
                    {
                        return new ErrorExpr(context);
                    }

                    if (!TypeCheckingUtils.CheckAssignable(handler, context.rhs, PrimitiveType.Bool, rhs))
                    {
                        return new ErrorExpr(context);
                    }

                    return logicCtors[op](lhs, rhs);

                default:
                    handler.Diagnostics.Report(handler.InternalError(context,
                        new ArgumentException($"unknown binary operation {op}", nameof(context))));
                    return new ErrorExpr(context);
            }
        }

        public override IPExpr VisitChooseExpr(PParser.ChooseExprContext context)
        {
            method.IsNondeterministic = true;
            // if choose is without an argument then its a choose boolean
            if (context.expr() == null)
            {
                return new ChooseExpr(context, null, PrimitiveType.Bool);
            }

            var subExpr = Visit(context.expr());
            if (subExpr.Type is ErrorType) return new ErrorExpr(context);
            var subExprType = subExpr.Type;

            switch (subExprType.Canonicalize())
            {
                case SequenceType seqType:
                    return new ChooseExpr(context, subExpr, seqType.ElementType);

                case SetType setType:
                    return new ChooseExpr(context, subExpr, setType.ElementType);

                case MapType mapType:
                    return new ChooseExpr(context, subExpr, mapType.KeyType);

                case PrimitiveType primType when primType.IsSameTypeAs(PrimitiveType.Int):
                {
                    if (subExpr is IntLiteralExpr subExprAsInt && subExprAsInt.Value > 10000)
                    {
                        handler.Diagnostics.Report(handler.IllegalChooseSubExprValue(context, subExprAsInt.Value));
                        return new ErrorExpr(context);
                    }
                    return new ChooseExpr(context, subExpr, PrimitiveType.Int);
                }

                default:
                    handler.Diagnostics.Report(handler.IllegalChooseSubExprType(context, subExprType));
                    return new ErrorExpr(context);
            }

        }

        public override IPExpr VisitCastExpr(PParser.CastExprContext context)
        {
            var subExpr = Visit(context.expr());
            // If the sub-expression already errored, still resolve the target
            // type so a malformed type expression also gets a chance to report,
            // but bail before any compatibility check.
            var oldType = subExpr.Type;
            var newType = TypeResolver.ResolveType(context.type(), table, handler);
            if (oldType is ErrorType || newType is ErrorType) return new ErrorExpr(context);

            if (context.cast.Text.Equals("as"))
            {
                if (!newType.IsAssignableFrom(oldType) && !oldType.IsAssignableFrom(newType))
                {
                    handler.Diagnostics.Report(handler.IncomparableTypes(context, oldType, newType));
                    return new ErrorExpr(context);
                }

                return new CastExpr(context, subExpr, newType);
            }

            if (context.cast.Text.Equals("to"))
            {
                if (!(newType is PermissionType || newType.IsSameTypeAs(PrimitiveType.Int) ||
                      newType.IsSameTypeAs(PrimitiveType.Float)))
                {
                    handler.Diagnostics.Report(handler.IllegalTypeInCoerceExpr(context));
                    return new ErrorExpr(context);
                }

                if (oldType.IsSameTypeAs(PrimitiveType.Int))
                {
                    if (newType.IsSameTypeAs(PrimitiveType.Int))
                    {
                        return subExpr;
                    }

                    if (newType.IsSameTypeAs(PrimitiveType.Float) || PLanguageType.TypeIsOfKind(newType, TypeKind.Enum))
                    {
                        return new CoerceExpr(context, subExpr, newType);
                    }
                }
                else if (oldType.IsSameTypeAs(PrimitiveType.Float))
                {
                    if (newType.IsSameTypeAs(PrimitiveType.Float))
                    {
                        return subExpr;
                    }

                    if (newType.IsSameTypeAs(PrimitiveType.Int))
                    {
                        return new CoerceExpr(context, subExpr, newType);
                    }
                }
                else if (PLanguageType.TypeIsOfKind(oldType, TypeKind.Enum))
                {
                    // TODO: are enum-to-enum coersions allowed?
                    if (newType.IsSameTypeAs(oldType))
                    {
                        return subExpr;
                    }

                    if (newType.IsSameTypeAs(PrimitiveType.Int))
                    {
                        return new CoerceExpr(context, subExpr, newType);
                    }
                }
                else if (oldType.IsSameTypeAs(PrimitiveType.Machine) || oldType is PermissionType)
                {
                    if (newType.IsSameTypeAs(oldType))
                    {
                        return subExpr;
                    }

                    if (newType is PermissionType)
                    {
                        if (oldType.IsSameTypeAs(PrimitiveType.Machine))
                        {
                            return new CoerceExpr(context, subExpr, newType);
                        }

                        if (oldType is PermissionType)
                        {
                            if (newType.AllowedPermissions.Value.Any(x => !oldType.AllowedPermissions.Value.Contains(x))
                               )
                            {
                                handler.Diagnostics.Report(handler.IllegalInterfaceCoerce(context, oldType, newType));
                                return new ErrorExpr(context);
                            }

                            return new CoerceExpr(context, subExpr, newType);
                        }
                    }
                }

                handler.Diagnostics.Report(handler.IncomparableTypes(context, oldType, newType));
                return new ErrorExpr(context);
            }

            handler.Diagnostics.Report(handler.InternalError(context, new ArgumentOutOfRangeException(nameof(context), "invalid cast")));
            return new ErrorExpr(context);
        }

        public override IPExpr VisitPrimitive(PParser.PrimitiveContext context)
        {
            if (context.iden() != null)
            {
                var symbolName = context.iden().GetText();
                if (table.Lookup(symbolName, out Variable variable))
                {
                    return new VariableAccessExpr(context, variable);
                }

                if (table.Lookup(symbolName, out EnumElem enumElem))
                {
                    return new EnumElemRefExpr(context, enumElem);
                }

                if (table.Lookup(symbolName, out Event evt))
                {
                    return new EventRefExpr(context, evt);
                }

                if (table.Lookup(symbolName, out Machine mac) && mac.IsSpec)
                {
                    return new SpecRefExpr(context, mac);
                }

                if (table.Lookup(symbolName, out Invariant inv))
                {
                    return new InvariantRefExpr(inv, context);
                }

                if (table.Lookup(symbolName, out InvariantGroup invGroup))
                {
                    return new InvariantGroupRefExpr(invGroup, context);
                }

                handler.Diagnostics.Report(handler.MissingDeclaration(context.iden(), "name", symbolName));
                return new ErrorExpr(context);
            }

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
                if (!LiteralParsingUtils.TryParseIntLiteral(context.IntLiteral().GetText(), out var intValue))
                {
                    handler.Diagnostics.Report(handler.ValueOutOfRange(context, "int"));
                    return new ErrorExpr(context);
                }
                return new IntLiteralExpr(context, intValue);
            }

            if (context.NullLiteral() != null)
            {
                return new NullLiteralExpr(context);
            }

            if (context.NONDET() != null)
            {
                if (method.Owner?.IsSpec == true)
                {
                    handler.Diagnostics.Report(handler.IllegalMonitorOperation(context, context.NONDET().Symbol, method.Owner));
                    return new ErrorExpr(context);
                }

                method.IsNondeterministic = true;
                return new NondetExpr(context);
            }

            if (context.FAIRNONDET() != null)
            {
                if (method.Owner?.IsSpec == true)
                {
                    handler.Diagnostics.Report(handler.IllegalMonitorOperation(context, context.FAIRNONDET().Symbol, method.Owner));
                    return new ErrorExpr(context);
                }

                method.IsNondeterministic = true;
                return new FairNondetExpr(context);
            }

            if (context.HALT() != null)
            {
                var success = table.Lookup("halt", out Event haltEvent);
                Debug.Assert(success);
                return new EventRefExpr(context, haltEvent);
            }

            if (context.THIS() != null)
            {
                if (method.Owner == null)
                {
                    handler.Diagnostics.Report(handler.MisplacedThis(context));
                    return new ErrorExpr(context);
                }

                if (method.Owner.IsSpec)
                {
                    handler.Diagnostics.Report(handler.IllegalMonitorOperation(context, context.THIS().Symbol, method.Owner));
                    return new ErrorExpr(context);
                }

                return new ThisRefExpr(context, method.Owner);
            }

            handler.Diagnostics.Report(handler.InternalError(context, new ArgumentOutOfRangeException(nameof(context), "unknown primitive")));
            return new ErrorExpr(context);
        }

        public override IPExpr VisitUnnamedTupleBody(PParser.UnnamedTupleBodyContext context)
        {
            var fields = context._fields.Select(Visit).ToArray();
            return new UnnamedTupleExpr(context, fields);
        }

        public override IPExpr VisitNamedTupleBody(PParser.NamedTupleBodyContext context)
        {
            var fields = context._values.Select(Visit).ToArray();

            var entries = new NamedTupleEntry[fields.Length];
            var names = new System.Collections.Generic.HashSet<string>();
            for (var i = 0; i < fields.Length; i++)
            {
                var entryName = context._names[i].GetText();
                if (names.Contains(entryName))
                {
                    handler.Diagnostics.Report(handler.DuplicateNamedTupleEntry(context._names[i], entryName));
                    // Mangle the name with a unique suffix so downstream
                    // NamedTupleType construction (which builds a Dictionary
                    // keyed by entry name) doesn't throw on the collision.
                    // The user already has the duplicate diagnostic; mangling
                    // just keeps the AST well-formed for any subsequent checks.
                    var mangled = $"{entryName}$dup${i}";
                    names.Add(mangled);
                    entries[i] = new NamedTupleEntry { Name = mangled, FieldNo = i, Type = fields[i].Type };
                    continue;
                }

                names.Add(entryName);
                entries[i] = new NamedTupleEntry { Name = entryName, FieldNo = i, Type = fields[i].Type };
            }

            var type = new NamedTupleType(entries);
            return new NamedTupleExpr(context, fields, type);
        }

        public override IPExpr VisitExpFloat(PParser.ExpFloatContext context)
        {
            throw new NotImplementedException("float(x,y) syntax");
        }

        public override IPExpr VisitDecimalFloat(PParser.DecimalFloatContext context)
        {
            if (!LiteralParsingUtils.TryParseFloatLiteral($"{context.pre?.Text ?? ""}.{context.post.Text}", out var value))
            {
                handler.Diagnostics.Report(handler.ValueOutOfRange(context, "float"));
                return new ErrorExpr(context);
            }
            return new FloatLiteralExpr(context, value);
        }

        public override IPExpr VisitRvalue(PParser.RvalueContext context)
        {
            return Visit(context.expr());
        }

        public override IPExpr VisitFormatedString([NotNull] PParser.FormatedStringContext context)
        {
            var baseString = context.StringLiteral().GetText();
            baseString = baseString.Substring(1, baseString.Length - 2); // strip beginning / end double quote
            var numNecessaryArgs = TypeCheckingUtils.PrintStmtNumArgs(baseString);
            // Visit args regardless of format validity so their internal errors surface.
            var args = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), this).ToList();

            if (numNecessaryArgs == -1)
            {
                handler.Diagnostics.Report(handler.InvalidStringExprFormat(context, context.StringLiteral().Symbol));
                return new ErrorExpr(context);
            }

            if (args.Count != numNecessaryArgs)
            {
                handler.Diagnostics.Report(handler.IncorrectArgumentCount(context, args.Count, numNecessaryArgs));
                return new ErrorExpr(context);
            }

            return new StringExpr(context, baseString, args);
        }

        public override IPExpr VisitStringExpr(PParser.StringExprContext context)
        {
            return VisitFormatedString(context.formatedString());
        }

        public override IPExpr VisitVarLvalue(PParser.VarLvalueContext context)
        {
            var varName = context.name.GetText();
            if (!table.LookupLvalue(handler, varName, context, out Variable variable))
            {
                handler.Diagnostics.Report(handler.MissingDeclaration(context, "variable", varName));
                return new ErrorExpr(context);
            }

            return new VariableAccessExpr(context, variable);
        }

        public override IPExpr VisitNamedTupleLvalue(PParser.NamedTupleLvalueContext context)
        {
            var lvalue = Visit(context.lvalue());
            if (lvalue.Type is ErrorType) return new ErrorExpr(context);

            if (!(lvalue.Type.Canonicalize() is NamedTupleType type))
            {
                handler.Diagnostics.Report(handler.TypeMismatch(lvalue, TypeKind.NamedTuple));
                return new ErrorExpr(context);
            }

            var field = context.field.GetText();
            if (!type.LookupEntry(field, out var entry))
            {
                handler.Diagnostics.Report(handler.MissingNamedTupleEntry(context.field, type));
                return new ErrorExpr(context);
            }

            return new NamedTupleAccessExpr(context, lvalue, entry);
        }

        public override IPExpr VisitTupleLvalue(PParser.TupleLvalueContext context)
        {
            var lvalue = Visit(context.lvalue());
            if (lvalue.Type is ErrorType) return new ErrorExpr(context);

            if (!(lvalue.Type.Canonicalize() is TupleType type))
            {
                handler.Diagnostics.Report(handler.TypeMismatch(lvalue, TypeKind.Tuple));
                return new ErrorExpr(context);
            }

            if (!LiteralParsingUtils.TryParseIntLiteral(context.@int().GetText(), out var field))
            {
                handler.Diagnostics.Report(handler.ValueOutOfRange(context.@int(), "int"));
                return new ErrorExpr(context);
            }
            if (field >= type.Types.Count)
            {
                handler.Diagnostics.Report(handler.OutOfBoundsTupleAccess(context.@int(), type));
                return new ErrorExpr(context);
            }

            return new TupleAccessExpr(context, lvalue, field, type.Types[field]);
        }

        public override IPExpr VisitMapOrSeqLvalue(PParser.MapOrSeqLvalueContext context)
        {
            var lvalue = Visit(context.lvalue());
            var index = Visit(context.expr());
            if (lvalue.Type is ErrorType || index.Type is ErrorType) return new ErrorExpr(context);

            var indexType = index.Type;
            switch (lvalue.Type.Canonicalize())
            {
                case MapType mapType:
                    if (!TypeCheckingUtils.CheckAssignable(handler, context.expr(), mapType.KeyType, index))
                    {
                        return new ErrorExpr(context);
                    }

                    return new MapAccessExpr(context, lvalue, index, mapType.ValueType);

                case SequenceType seqType:
                    if (!TypeCheckingUtils.CheckAssignable(handler, context.expr(), PrimitiveType.Int, index))
                    {
                        return new ErrorExpr(context);
                    }

                    return new SeqAccessExpr(context, lvalue, index, seqType.ElementType);

                default:
                    handler.Diagnostics.Report(handler.TypeMismatch(lvalue, TypeKind.Sequence, TypeKind.Map));
                    return new ErrorExpr(context);
            }
        }
    }
}
