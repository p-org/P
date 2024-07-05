using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class QuantExpr : IPExpr
    {
        public QuantExpr(ParserRuleContext sourceLocation, QuantType operation, List<Variable> bound, IPExpr body)
        {
            SourceLocation = sourceLocation;
            Quant = operation;
            Bound = bound;
            Body = body;
            Type = PrimitiveType.Bool;
        }

        public QuantType Quant { get; }
        public List<Variable> Bound { get; }
        public IPExpr Body { get; }

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; }
    }
}