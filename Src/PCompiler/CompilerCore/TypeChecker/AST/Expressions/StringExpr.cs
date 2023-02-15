using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class StringExpr : IPExpr
    {
        public StringExpr(ParserRuleContext sourceLocation, string value, List<IPExpr> args)
        {
            Contract.Requires(sourceLocation != null);
            Contract.Requires(value != null);
            SourceLocation = sourceLocation;
            BaseString = value;
            Args = args;
        }

        public string BaseString { get; }
        public List<IPExpr> Args { get; }

        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type { get; } = PrimitiveType.String;
    }
}