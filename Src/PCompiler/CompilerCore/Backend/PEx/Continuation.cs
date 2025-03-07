using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;

namespace Plang.Compiler.Backend.PEx;

public class Continuation : Function
{
    private readonly List<Variable> localParameters = new();
    private readonly Dictionary<Variable, Variable> storeForLocal = new();
    private readonly List<Variable> storeParameters = new();

    public Continuation(string name, IReadOnlyDictionary<Event, Function> cases, IPStmt after,
        ParserRuleContext location) : base(name, location)
    {
        Cases = cases;
        After = after;
    }

    public IReadOnlyDictionary<Event, Function> Cases { get; }
    public IPStmt After { get; }
    public IEnumerable<Variable> StoreParameters => storeParameters;
    public IEnumerable<Variable> LocalParameters => localParameters;
    public IReadOnlyDictionary<Variable, Variable> StoreForLocal => storeForLocal;

    public void AddParameter(Variable local, Variable store)
    {
        if (!storeParameters.Select(param => param.Name).Contains(store.Name))
            storeParameters.Add(store);
        if (!localParameters.Select(param => param.Name).Contains(local.Name))
            localParameters.Add(local);
        var localAccess = new VariableAccessExpr(SourceLocation, local);
        var storeAccess = new VariableAccessExpr(SourceLocation, store);
        var storeStmt = new AssignStmt(SourceLocation, storeAccess, localAccess);
        storeForLocal.Add(local, store);
    }
}