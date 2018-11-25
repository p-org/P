using System;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Declarations
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

    [Flags]
    public enum VariableRole
    {
        Local = 1 << 0,
        Param = 1 << 1,
        Field = 1 << 2,
        Temp = 1 << 3
    }
}