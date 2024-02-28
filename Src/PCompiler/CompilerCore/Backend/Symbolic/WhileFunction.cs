using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.Backend.Symbolic
{
    public class WhileFunction : Function
    {
        public WhileFunction(string name, ParserRuleContext location) : base(name, location)
        {
        }

        public void AddParameter(Variable param)
        {
            Signature.Parameters.Add(param);
        }
    }
}