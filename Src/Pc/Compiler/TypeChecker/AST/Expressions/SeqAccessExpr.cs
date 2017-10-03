using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class SeqAccessExpr : IPExpr
    {
        public SeqAccessExpr(IPExpr seqExpr, IPExpr indexExpr, PLanguageType type)
        {
            SeqExpr = seqExpr;
            IndexExpr = indexExpr;
            Type = type;
        }

        public IPExpr SeqExpr { get; }
        public IPExpr IndexExpr { get; }

        public PLanguageType Type { get; }
    }
}
