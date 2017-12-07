using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker
{
    internal static class TypeCheckingUtils
    {
        public static void ValidatePayloadTypes(ITranslationErrorHandler handler, ParserRuleContext context,
                                             PLanguageType payloadType, IReadOnlyList<IPExpr> arguments)
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
                foreach (Tuple<IPExpr, PLanguageType> pair in arguments.Zip(tuple.Types, Tuple.Create))
                {
                    CheckArgument(handler, context, pair.Item2, pair.Item1);
                }
            }
            else
            {
                throw handler.IncorrectArgumentCount(context, arguments.Count, 1);
            }
        }

        public static void CheckArgument(ITranslationErrorHandler handler, ParserRuleContext context,
                                          PLanguageType argumentType, IPExpr arg)
        {
            if (arg is ILinearRef linearRef)
            {
                if (linearRef.LinearType.Equals(LinearType.Swap) && !arg.Type.IsSameTypeAs(argumentType))
                {
                    throw handler.TypeMismatch(context, arg.Type, argumentType);
                }
            }
            if (!argumentType.IsAssignableFrom(arg.Type))
            {
                throw handler.TypeMismatch(context, arg.Type, argumentType);
            }
        }
    }
}