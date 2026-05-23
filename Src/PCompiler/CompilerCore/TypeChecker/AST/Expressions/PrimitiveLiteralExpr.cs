using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    /// <summary>
    /// Shared base for primitive-typed literal expressions (bool, int, float).
    /// Holds the value, source location, and language type. Concrete subclasses
    /// fix the value type and primitive type; their identity is preserved so
    /// pattern matching like <c>case BoolLiteralExpr ble:</c> across the
    /// codebase continues to work.
    /// </summary>
    public abstract class PrimitiveLiteralExpr<T> : IStaticTerm<T>
    {
        protected PrimitiveLiteralExpr(ParserRuleContext sourceLocation, T value, PLanguageType type)
        {
            SourceLocation = sourceLocation;
            Value = value;
            Type = type;
        }

        public T Value { get; }
        public PLanguageType Type { get; }
        public ParserRuleContext SourceLocation { get; }

        public override string ToString() => Value.ToString();
    }
}
