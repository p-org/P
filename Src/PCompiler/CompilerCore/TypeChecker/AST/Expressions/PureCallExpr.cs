using System.Collections.Generic;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class PureCallExpr : IPExpr
    {
        public PureCallExpr(ParserRuleContext sourceLocation, Pure function, IReadOnlyList<IPExpr> arguments)
        {
            SourceLocation = sourceLocation;
            Pure = function;
            Arguments = arguments;
            Type = function.Signature.ReturnType;
        }

        public Pure Pure { get; }
        public IReadOnlyList<IPExpr> Arguments { get; }

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; }
    }
}