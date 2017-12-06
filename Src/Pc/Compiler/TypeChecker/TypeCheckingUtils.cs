using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker
{
    internal static class TypeCheckingUtils
    {
        public static bool ArgListMatchesTupleType(PLanguageType expected, IReadOnlyList<IPExpr> arguments)
        {
            if (arguments.Count == 1 && expected.IsAssignableFrom(arguments[0].Type))
            {
                return true;
            }
            return expected.IsAssignableFrom(new TupleType(arguments.Select(arg => arg.Type).ToArray()));
        }
    }
}