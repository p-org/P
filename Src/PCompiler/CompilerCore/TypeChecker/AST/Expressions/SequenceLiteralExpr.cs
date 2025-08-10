using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class SequenceLiteralExpr: IPExpr
    {
        public SequenceLiteralExpr(ParserRuleContext sourceLocation, IReadOnlyList<IPExpr> sequenceElements)
        {
            SourceLocation = sourceLocation;
            SequenceElements = sequenceElements;
            Type = new SequenceType(sequenceElements[0].Type);
        }

        public IReadOnlyList<IPExpr> SequenceElements { get; }

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; }
    }
}
