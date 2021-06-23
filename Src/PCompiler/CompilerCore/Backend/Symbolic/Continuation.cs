using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker.Types;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST;
using System.IO;
using System;

namespace Plang.Compiler.Backend.Symbolic
{
    public class Continuation : Function
    {
        public Continuation(FunctionSignature signature, IReadOnlyDictionary<PEvent, Function> cases, IPStmt after, ParserRuleContext location) : base(null, location)
        {
            Cases = cases;
            After = after;
        }

        public void AddParameter(Variable param)
        {
            Signature.Parameters.Add(param);
/*
            IEnumerable<Function> functions = new HashSet<Function>(Cases.Values);
            foreach (var f in functions)
            {
               f.Signature.Parameters.Add(param);
            } 
*/
        }

        public IReadOnlyDictionary<PEvent, Function> Cases { get; }
        public IPStmt After { get; } 
    }
}
