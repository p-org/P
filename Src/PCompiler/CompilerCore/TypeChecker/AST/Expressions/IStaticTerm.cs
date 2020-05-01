namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    /// <inheritdoc />
    /// <summary>
    ///     Empty interface to identify base terms (literals, this, nondet, default)
    /// </summary>
    public interface IStaticTerm : IExprTerm
    {
    }

    /// <inheritdoc />
    /// <summary>
    ///     Interface to get C#-language-level values out of static terms
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    public interface IStaticTerm<out T> : IStaticTerm
    {
        T Value { get; }
    }
}