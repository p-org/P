using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    /// <summary>
    /// Represents an emit_coverage statement in the P language.
    /// This statement is used to record code coverage metrics during program execution.
    /// </summary>
    public class EmitCoverageStmt : IPStmt
    {
        public EmitCoverageStmt(ParserRuleContext sourceLocation, IPExpr label, IPExpr payload = null)
        {
            SourceLocation = sourceLocation;
            Label = label;
            Payload = payload;
        }

        /// <summary>
        /// The label used to identify this coverage point.
        /// Must be a string expression.
        /// </summary>
        public IPExpr Label { get; }

        /// <summary>
        /// Optional payload data to record with this coverage point.
        /// Can be any expression type.
        /// </summary>
        public IPExpr Payload { get; }
        
        /// <summary>
        /// Source location in the P program.
        /// </summary>
        public ParserRuleContext SourceLocation { get; }
    }
}
