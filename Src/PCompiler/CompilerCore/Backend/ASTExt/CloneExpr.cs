using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.ASTExt
{
    internal class CloneExpr : IPExpr
    {
        /// <summary>
        /// The full lowering only clones terms. The C# lowering (<see cref="LoweringMode.CSharp"/>) leaves
        /// expressions inline, so it also clones accesses into values and other non-terms.
        /// </summary>
        public CloneExpr(IPExpr term)
        {
            Term = term;
            SourceLocation = term.SourceLocation;
            Type = term.Type;
        }

        public IPExpr Term { get; }

        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type { get; }
    }
}