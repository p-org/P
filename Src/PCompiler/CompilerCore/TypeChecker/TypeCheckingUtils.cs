using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker
{
    public static class TypeCheckingUtils
    {
        // ─── Cascade-suppression helpers (Phase 2 multi-error type checker) ─────────
        //
        // In strict mode (the historical behavior) handler.Diagnostics.Report
        // re-throws immediately, so these helpers are functionally identical to
        // the old `throw handler.X(...)` sites. In collecting mode, Report
        // appends and returns, and the boolean return value lets the caller
        // decide whether to substitute ErrorExpr or otherwise short-circuit
        // further checks on the same node.
        //
        // Cascade suppression rule: an ErrorType operand silently passes the
        // check. This prevents one undeclared variable from generating a chain
        // of "incompatible operand" / "wrong argument type" diagnostics.
        // PLanguageType.IsSameTypeAs has a symmetric short-circuit in the base
        // class for the same reason; here we cover the asymmetric
        // IsAssignableFrom direction.

        /// <summary>
        /// Report-and-recover assignability check. Returns true when
        /// <paramref name="expected"/> can hold a value of <paramref name="actual"/>'s
        /// type — or when either side is the <see cref="ErrorType"/> sentinel,
        /// in which case the mismatch is silently suppressed because a more
        /// fundamental diagnostic has already been reported upstream.
        /// </summary>
        public static bool CheckAssignable(
            ITranslationErrorHandler handler,
            ParserRuleContext context,
            PLanguageType expected,
            IPExpr actual)
        {
            if (actual.Type is ErrorType || expected is ErrorType) return true;
            if (expected.IsAssignableFrom(actual.Type)) return true;
            handler.Diagnostics.Report(handler.TypeMismatch(context, actual.Type, expected));
            return false;
        }

        /// <summary>
        /// Variant of <see cref="CheckAssignable"/> that takes a raw type for
        /// the actual side, used when the caller doesn't have an
        /// <see cref="IPExpr"/> handy (e.g. payload checks on event refs).
        /// </summary>
        public static bool CheckAssignable(
            ITranslationErrorHandler handler,
            ParserRuleContext context,
            PLanguageType expected,
            PLanguageType actual)
        {
            if (actual is ErrorType || expected is ErrorType) return true;
            if (expected.IsAssignableFrom(actual)) return true;
            handler.Diagnostics.Report(handler.TypeMismatch(context, actual, expected));
            return false;
        }

        public static void ValidatePayloadTypes(
            ITranslationErrorHandler handler,
            ParserRuleContext context,
            PLanguageType payloadType,
            IReadOnlyList<IPExpr> arguments)
        {
            // If the payload type itself failed to resolve there's nothing to
            // validate against. Per-argument ErrorType is handled below by
            // CheckAssignable's own short-circuit, so independent diagnostics
            // like arity mismatches still fire even when one argument errored
            // upstream. Accept null as ErrorType: pass 2a (MachineChecker) may
            // leave a PayloadType null when an event/interface declaration
            // itself errored — once Phase 3 starts running passes 4+ on a
            // partial AST, treating null as "already-errored" gives the same
            // cascade suppression as ErrorType without requiring every upstream
            // producer to wire an ErrorType sentinel.
            if (payloadType is null || payloadType is ErrorType) return;

            if (arguments.Count == 0)
            {
                if (!payloadType.IsSameTypeAs(PrimitiveType.Null))
                {
                    handler.Diagnostics.Report(handler.TypeMismatch(context, PrimitiveType.Null, payloadType));
                }
            }
            else if (arguments.Count == 1)
            {
                CheckArgument(handler, context, payloadType, arguments[0]);
            }
            else if (payloadType.Canonicalize() is TupleType tuple)
            {
                // Arity check fires regardless of whether any argument has
                // ErrorType — a count mismatch is independent of per-arg type.
                if (tuple.Types.Count != arguments.Count)
                {
                    handler.Diagnostics.Report(handler.IncorrectArgumentCount(context, arguments.Count, tuple.Types.Count));
                    return;
                }
                foreach (var pair in tuple.Types.Zip(arguments, Tuple.Create))
                {
                    CheckArgument(handler, context, pair.Item1, pair.Item2);
                }
            }
            else
            {
                handler.Diagnostics.Report(handler.IncorrectArgumentCount(context, arguments.Count, 1));
            }
        }

        public static void CheckArgument(
            ITranslationErrorHandler handler,
            ParserRuleContext context,
            PLanguageType argumentType,
            IPExpr arg)
        {
            CheckAssignable(handler, context, argumentType, arg);
        }

        public static IEnumerable<IPExpr> VisitRvalueList(PParser.RvalueListContext context, ExprVisitor visitor)
        {
            return context?.rvalue().Select(visitor.Visit) ?? Enumerable.Empty<IPExpr>();
        }

        public static int PrintStmtNumArgs(string message)
        {
            // Tried using regex for this and it became a hotspot.
            // There are specific unit tests for this method.
            // Do not modify without adding tests.
            var max = 0;
            for (var i = 0; i < message.Length; i++)
            {
                if (message[i] == '{')
                {
                    if (++i >= message.Length)
                    {
                        return -1; // error - opened { at end of string
                    }

                    if (message[i] == '{')
                    {
                        continue;
                    }

                    var cur = 0;
                    do
                    {
                        if (!char.IsDigit(message[i]))
                        {
                            return -1; // error - expecting only digits within { ... }
                        }

                        cur = 10 * cur + (message[i] - '0');
                    } while (++i < message.Length && message[i] != '}');

                    if (i >= message.Length)
                    {
                        return -1; // error - missing closing } at end of string.
                    }

                    max = Math.Max(cur + 1, max);
                }
                else if (message[i] == '}')
                {
                    if (++i >= message.Length || message[i] != '}')
                    {
                        return -1; // error - stray, unescaped }
                    }
                }
            }

            return max;
        }
    }
}