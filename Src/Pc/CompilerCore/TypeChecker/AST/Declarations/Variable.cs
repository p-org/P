using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class Variable : IPDecl
    {
        public Variable(string name, ParserRuleContext sourceNode, VariableRole role)
        {
            Name = name;
            SourceLocation = sourceNode;
            Role = role;
        }

        public VariableRole Role { get; }
        public PLanguageType Type { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
    }

    public enum VariableRole
    {
        Local,
        Param,
        Field
    }
}