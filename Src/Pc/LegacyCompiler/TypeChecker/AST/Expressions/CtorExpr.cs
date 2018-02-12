using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class CtorExpr : IPExpr
    {
        public CtorExpr(ParserRuleContext sourceLocation, Machine machine, IPExpr[] arguments)
        {
            Machine = machine;
            Arguments = arguments;
            SourceLocation = sourceLocation;
            Type = new PermissionType(machine);
        }

        public Machine Machine { get; }
        public IPExpr[] Arguments { get; }

        public PLanguageType Type { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}
