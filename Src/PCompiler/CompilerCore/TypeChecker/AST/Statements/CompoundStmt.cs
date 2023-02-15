using System.Collections.Generic;
using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class CompoundStmt : IPStmt
    {
        private readonly List<IPStmt> statements;

        public CompoundStmt(ParserRuleContext sourceLocation, IEnumerable<IPStmt> statements)
        {
            SourceLocation = sourceLocation;
            this.statements = new List<IPStmt>();
            foreach (var statement in statements)
            {
                if (statement is CompoundStmt compound)
                {
                    this.statements.AddRange(compound.statements);
                }
                else if (!(statement is NoStmt))
                {
                    this.statements.Add(statement);
                }
            }
        }

        public IReadOnlyList<IPStmt> Statements => statements;

        public ParserRuleContext SourceLocation { get; }

        public static CompoundStmt FromStatement(IPStmt statement)
        {
            if (statement is CompoundStmt compound)
            {
                return compound;
            }

            return new CompoundStmt(statement.SourceLocation, new[] { statement });
        }
    }
}