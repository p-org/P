using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker.Types;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Expressions;
using System.IO;
using System;

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
