using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class SeqAccessExpr : IPExpr
    {
        public SeqAccessExpr(ParserRuleContext sourceLocation, IPExpr seqExpr, IPExpr indexExpr, PLanguageType type)
        {
            SourceLocation = sourceLocation;
            SeqExpr = seqExpr;
            IndexExpr = indexExpr;
            Type = type;
        }

        public ParserRuleContext SourceLocation { get; }
        public IPExpr SeqExpr { get; }
        public IPExpr IndexExpr { get; }

        public PLanguageType Type { get; }
    }
}
