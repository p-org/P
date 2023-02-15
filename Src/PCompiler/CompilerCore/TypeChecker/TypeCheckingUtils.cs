using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker
{
    public static class TypeCheckingUtils
    {
        public static void ValidatePayloadTypes(
            ITranslationErrorHandler handler,
            ParserRuleContext context,
            PLanguageType payloadType,
            IReadOnlyList<IPExpr> arguments)
        {
            if (arguments.Count == 0)
            {
                if (!payloadType.IsSameTypeAs(PrimitiveType.Null))
                {
                    throw handler.TypeMismatch(context, PrimitiveType.Null, payloadType);
                }
            }
            else if (arguments.Count == 1)
            {
                CheckArgument(handler, context, payloadType, arguments[0]);
            }
            else if (payloadType.Canonicalize() is TupleType tuple)
            {
                foreach (var pair in tuple.Types.Zip(arguments, Tuple.Create))
                {
                    CheckArgument(handler, context, pair.Item1, pair.Item2);
                }
            }
            else
            {
                throw handler.IncorrectArgumentCount(context, arguments.Count, 1);
            }
        }

        public static void CheckArgument(
            ITranslationErrorHandler handler,
            ParserRuleContext context,
            PLanguageType argumentType,
            IPExpr arg)
        {
            if (!argumentType.IsAssignableFrom(arg.Type))
            {
                throw handler.TypeMismatch(context, arg.Type, argumentType);
            }
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