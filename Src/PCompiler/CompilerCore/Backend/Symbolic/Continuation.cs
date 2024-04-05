using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;

namespace Plang.Compiler.Backend.Symbolic
{
    public class Continuation : Function
    {
        public Continuation(string name, IReadOnlyDictionary<PEvent, Function> cases, IPStmt after, ParserRuleContext location) : base(name, location)
        {
            Cases = cases;
            After = after;
        }

        public void AddParameter(Variable local, Variable store)
        {
            if (!storeParameters.Select(param => param.Name).Contains(store.Name))
                storeParameters.Add(store);
            if (!localParameters.Select(param => param.Name).Contains(local.Name))
                localParameters.Add(local);
            var localAccess = new VariableAccessExpr(SourceLocation, local);
            var storeAccess = new VariableAccessExpr(SourceLocation, store);
            var storeStmt = new AssignStmt(SourceLocation, storeAccess, localAccess);
            storeStmts.Add(storeStmt);
            storeForLocal.Add(local, store);
        }

        public IReadOnlyDictionary<PEvent, Function> Cases { get; }
        public IPStmt After { get; }
        public IEnumerable<Variable> StoreParameters => storeParameters;
        public IEnumerable<Variable> LocalParameters => localParameters;
        public IEnumerable<AssignStmt> StoreStmts => storeStmts;
        public IReadOnlyDictionary<Variable, Variable> StoreForLocal => storeForLocal;
        private readonly List<Variable> storeParameters = new List<Variable>();
        private readonly List<Variable> localParameters = new List<Variable>();
        private readonly List<AssignStmt> storeStmts = new List<AssignStmt>();
        private readonly Dictionary<Variable, Variable> storeForLocal = new Dictionary<Variable, Variable>();
    }
}