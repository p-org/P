using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Statements;

public class ConstraintStmt : IPStmt
{
    public ConstraintStmt(ParserRuleContext sourceLocation, IPExpr constraint)
    {
        SourceLocation = sourceLocation;
        Constraint = constraint;
    }

    public IPExpr Constraint { get; }
    public ParserRuleContext SourceLocation { get; }
}