using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class Variable : IPDecl, ITypedName
    {
        public Variable(string name, ParserRuleContext sourceNode, VariableRole role)
        {
            Name = name;
            SourceLocation = sourceNode;
            Role = role;
        }
        
        public VariableRole Role { get; }

        public string Name { get; set; }
        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type { get; set; }
    }

    public enum VariableRole
    {
        Local,
        Param,
        Field
    }
}
