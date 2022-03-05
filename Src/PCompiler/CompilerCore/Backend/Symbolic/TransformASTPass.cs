using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Plang.Compiler.Backend.ASTExt;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;
using Antlr4.Runtime;

namespace Plang.Compiler.Backend.Symbolic
{
    class TransformASTPass 
    {

         static private int continuationNumber = 0;
         static private int whileNumber = 0;
         static private int callNum = 0;

         static public List<IPDecl> GetTransformedDecls(Scope globalScope)
         {
            continuationNumber = 0;
            callNum = 0;
            List<IPDecl> decls = new List<IPDecl>();

            foreach (var decl in globalScope.AllDecls)
            {
                IPDecl result = TransformDecl(decl);
                if (result != null)
                    decls.Add(result);
            }

            continuationNumber = 0;
            callNum = 0;
            return decls;
        }

        static private IPDecl TransformDecl(IPDecl decl)
        {
            switch (decl)
            {
                case Function function:
                    if (function.IsForeign)
                        return function;
                    else
                        return null;
                case Machine machine:
                    if (machine.Receives.Events.GetEnumerator().MoveNext())
                        return TransformMachine(machine);
                    else return machine; 
                default:
                    return decl;
            }
        }

        static private Machine TransformMachine(Machine machine)
        {
            Machine transformedMachine = new Machine(machine.Name, machine.SourceLocation);
            transformedMachine.Assume = machine.Assume;
            transformedMachine.Assert = machine.Assert;
            transformedMachine.Receives = machine.Receives;
            transformedMachine.Sends = machine.Sends;
            transformedMachine.Creates = machine.Creates;
            foreach (var field in machine.Fields) transformedMachine.AddField(field);
            Dictionary<Function, Function> functionMap = new Dictionary<Function, Function>();
            foreach (var method in machine.Methods)
            {
                InlineInFunction(method);
            }
            foreach (var method in machine.Methods)
            {
                Function transformedFunction = TransformFunction(method, transformedMachine);
                functionMap.Add(method, transformedFunction);
                transformedMachine.AddMethod(transformedFunction);
            }
            transformedMachine.StartState = machine.StartState;
            transformedMachine.Observes = machine.Observes;
            transformedMachine.PayloadType = machine.PayloadType;
            transformedMachine.Scope = machine.Scope;
            foreach (var state in machine.States)
            {
                transformedMachine.AddState(TransformState(state, functionMap));
            }
            
            foreach (var method in machine.Methods)
            {
                foreach (var callee in method.Callees)
                {
                    if (functionMap.ContainsKey(callee))
                        functionMap[method].AddCallee(functionMap[callee]);
                    else functionMap[method].AddCallee(callee);
                }
            }
            return transformedMachine;
         }

         static private State TransformState(State state, IDictionary<Function, Function> functionMap)
         {
             State transformedState = new State(state.SourceLocation, state.Name);
             transformedState.Temperature = state.Temperature;
             transformedState.IsStart = state.IsStart;
             if (state.Entry != null)
                 transformedState.Entry = functionMap[state.Entry];
             if (state.Exit != null)
                 transformedState.Exit = functionMap[state.Exit];
             transformedState.OwningMachine = state.OwningMachine;
             transformedState.Container = state.Container;
             
             foreach (var handler in state.AllEventHandlers)
             {
                 transformedState[handler.Key] = TransformAction(handler.Value, functionMap);
             }

             return transformedState;
         }

         static private IStateAction TransformAction(IStateAction action, IDictionary<Function, Function> functionMap)
         {
             switch (action)
             {
                 case EventDoAction doAction:
                     return new EventDoAction(doAction.SourceLocation, doAction.Trigger, functionMap[doAction.Target]);
                 case EventGotoState gotoState:
                     Function transition = null;
                     if(gotoState.TransitionFunction != null) transition = functionMap[gotoState.TransitionFunction];
                     return new EventGotoState(gotoState.SourceLocation, gotoState.Trigger, gotoState.Target, transition);
                 default:
                     return action;
             }
         }

         static private void GenerateInline(Function caller, Function callee, IReadOnlyList<IPExpr> argsList, List<IPStmt> body, ParserRuleContext sourceLocation)
         {
             Dictionary<Variable,Variable> newVarMap = new Dictionary<Variable,Variable>();
             for (int i = 0; i < callee.Signature.Parameters.Count; i++)
             {
                 IPExpr expr = argsList[i];
                 Variable newVar = new Variable($"inline_{callNum}_{callee.Signature.Parameters[i].Name}", sourceLocation, VariableRole.Temp);
                 newVar.Type = expr.Type;
                 body.Add(new AssignStmt(sourceLocation, new VariableAccessExpr(sourceLocation, newVar), expr));
                 newVarMap.Add(callee.Signature.Parameters[i], newVar);
                 caller.AddLocalVariable(newVar);
             }
             foreach(var local in callee.LocalVariables)
             {
                 Variable newVar = new Variable($"local_{callNum}_{local.Name}", sourceLocation, VariableRole.Temp);
                 newVar.Type = local.Type;
                 newVarMap.Add(local, newVar);
                 caller.AddLocalVariable(newVar);
             }
             foreach(var funStmt in callee.Body.Statements)
                 body.Add(ReplaceVars(funStmt, newVarMap));
             callNum++;
         }

         static private List<IPStmt> ReplaceReturn(IReadOnlyList<IPStmt> body, IPExpr location)
         {
             List<IPStmt> newBody = new List<IPStmt>();
             foreach (IPStmt stmt in body)
             {
                 switch (stmt)
                 {
                     case ReturnStmt returnStmt:
                         newBody.Add(new AssignStmt(returnStmt.SourceLocation, location, returnStmt.ReturnValue)); 
                         break;
                     case CompoundStmt compoundStmt:
                         List<IPStmt> replace = ReplaceReturn(compoundStmt.Statements, location); 
                         foreach (var statement in replace) newBody.Add(statement);
                         break;
                     case IfStmt ifStmt:
                         IPStmt thenStmt = null;
                         if (ifStmt.ThenBranch != null)
                         {
                             List<IPStmt> replaceThen = ReplaceReturn(ifStmt.ThenBranch.Statements, location);
                             thenStmt =  new CompoundStmt(ifStmt.ThenBranch.SourceLocation, replaceThen);
                         }
                         IPStmt elseStmt = null;
                         if (ifStmt.ElseBranch != null)
                         {
                             List<IPStmt> replaceElse = ReplaceReturn(ifStmt.ElseBranch.Statements, location); 
                             elseStmt =  new CompoundStmt(ifStmt.ElseBranch.SourceLocation, replaceElse);
                         }
                         newBody.Add(new IfStmt(ifStmt.SourceLocation, ifStmt.Condition, thenStmt, elseStmt));
                         break;
                     case ReceiveStmt receiveStmt:
                         foreach(KeyValuePair<PEvent, Function> entry in receiveStmt.Cases)
                         {
                             entry.Value.Body = new CompoundStmt(entry.Value.Body.SourceLocation, ReplaceReturn(entry.Value.Body.Statements, location));
                             entry.Value.Signature.ReturnType = null;
                         }
                         break;
                     case WhileStmt whileStmt:
                         List<IPStmt> bodyList = new List<IPStmt>();
                         bodyList.Add(whileStmt.Body);
                         List<IPStmt> replaceWhile = ReplaceReturn(bodyList, location); 
                         newBody.Add(new WhileStmt(whileStmt.SourceLocation, whileStmt.Condition, new CompoundStmt(whileStmt.Body.SourceLocation, replaceWhile)));
                         break;
                     default: 
                         newBody.Add(stmt);
                         break;
                 }
             }
             return newBody;
         }
 
         static private void InlineStmt(Function function, IPStmt stmt, List<IPStmt> body)
         {
             switch (stmt)
             {
                 case AssignStmt assign:
                     if (assign.Value is FunCallExpr)
                     {
                         InlineInFunction(((FunCallExpr) assign.Value).Function);
                         List<IPStmt> appendToBody = new List<IPStmt>();
                         GenerateInline(function, ((FunCallExpr) assign.Value).Function, ((FunCallExpr) assign.Value).Arguments, appendToBody, assign.SourceLocation);
                         appendToBody = ReplaceReturn(appendToBody, assign.Location);
                         foreach (var statement in appendToBody) body.Add(statement);
                     }
                     else
                     {
                         body.Add(assign);
                     }
                     break;
                 case CompoundStmt compound:
                     foreach (var statement in compound.Statements) InlineStmt(function, statement, body);
                     break;
                 case FunCallStmt call:
                     if (call.Function.CanReceive ?? true)
                     {
                         InlineInFunction(call.Function);
                         GenerateInline(function, call.Function, call.ArgsList, body, call.SourceLocation);
                     }
                     else
                     {
                         body.Add(call);
                     }
                     break;
                 case IfStmt ifStmt:
                     IPStmt thenStmt = null;
                     if (ifStmt.ThenBranch != null)
                     {
                       List<IPStmt> thenBranch = new List<IPStmt>();
                       InlineStmt(function, ifStmt.ThenBranch, thenBranch);
                       thenStmt = new CompoundStmt(ifStmt.ThenBranch.SourceLocation, thenBranch);
                     }
                     IPStmt elseStmt = null;
                     if (ifStmt.ElseBranch != null)
                     {
                       List<IPStmt> elseBranch = new List<IPStmt>();
                       InlineStmt(function, ifStmt.ElseBranch, elseBranch);
                       elseStmt = new CompoundStmt(ifStmt.ElseBranch.SourceLocation, elseBranch);
                     }
                     body.Add(new IfStmt(ifStmt.SourceLocation, ifStmt.Condition, thenStmt, elseStmt));
                     break;
                 case WhileStmt whileStmt:
                     List<IPStmt> bodyList = new List<IPStmt>();
                     InlineStmt(function, whileStmt.Body, bodyList);
                     body.Add(new WhileStmt(whileStmt.SourceLocation, whileStmt.Condition, new CompoundStmt(whileStmt.Body.SourceLocation, bodyList)));
                     break;
                 default:
                     body.Add(stmt);
                     break;
             }
         }

         static private void InlineInFunction(Function function)
         {
             List<IPStmt> body = new List<IPStmt>();
             foreach (var stmt in function.Body.Statements)
             {
                 InlineStmt(function, stmt, body);
             }
             function.Body = new CompoundStmt(function.Body.SourceLocation, body);
             return;
         }

         static private IPStmt ReplaceVars(IPStmt stmt, Dictionary<Variable,Variable> varMap)
         {
             if (stmt == null) return null;
             switch(stmt)
             {
                 case AddStmt addStmt:
                     return new AddStmt(addStmt.SourceLocation, ReplaceVars(addStmt.Variable, varMap), ReplaceVars(addStmt.Value, varMap));
                 case AnnounceStmt announceStmt:
                     return new AnnounceStmt(announceStmt.SourceLocation, ReplaceVars(announceStmt.PEvent, varMap), ReplaceVars(announceStmt.Payload, varMap));
                 case AssertStmt assertStmt:
                     return new AssertStmt(assertStmt.SourceLocation, ReplaceVars(assertStmt.Assertion, varMap), ReplaceVars(assertStmt.Message, varMap));
                 case AssignStmt assignStmt:
                     return new AssignStmt(assignStmt.SourceLocation, ReplaceVars(assignStmt.Location, varMap), ReplaceVars(assignStmt.Value, varMap));
                 case CompoundStmt compoundStmt:
                     List<IPStmt> statements = new List<IPStmt>();
                     foreach (var inner in compoundStmt.Statements) statements.Add(ReplaceVars(inner, varMap));
                     return new CompoundStmt(compoundStmt.SourceLocation, statements);
                 case CtorStmt ctorStmt:
                     List<IPExpr> arguments = new List<IPExpr>();
                     foreach (var arg in ctorStmt.Arguments) arguments.Add(ReplaceVars(arg, varMap));
                     return new CtorStmt(ctorStmt.SourceLocation, ctorStmt.Interface, arguments);
                 case FunCallStmt funCallStmt:
                     List<IPExpr> newArgs = new List<IPExpr>();
                     foreach (var arg in funCallStmt.ArgsList) newArgs.Add(ReplaceVars(arg, varMap));
                     return new FunCallStmt(funCallStmt.SourceLocation, funCallStmt.Function, new List<IPExpr>(newArgs));
                 case GotoStmt gotoStmt:
                     return new GotoStmt(gotoStmt.SourceLocation, gotoStmt.State, ReplaceVars(gotoStmt.Payload, varMap));
                 case IfStmt ifStmt:
                     return new IfStmt(ifStmt.SourceLocation, ReplaceVars(ifStmt.Condition, varMap), ReplaceVars(ifStmt.ThenBranch, varMap), ReplaceVars(ifStmt.ElseBranch, varMap));
                 case InsertStmt insertStmt:
                     return new InsertStmt(insertStmt.SourceLocation, ReplaceVars(insertStmt.Variable, varMap), ReplaceVars(insertStmt.Index, varMap), ReplaceVars(insertStmt.Value, varMap));
                 case MoveAssignStmt moveAssignStmt:
                     Variable fromVar = moveAssignStmt.FromVariable;
                     if (varMap.ContainsKey(moveAssignStmt.FromVariable)) fromVar = varMap[moveAssignStmt.FromVariable];
                     return new MoveAssignStmt(moveAssignStmt.SourceLocation, ReplaceVars(moveAssignStmt.ToLocation, varMap), fromVar);
                 case PrintStmt printStmt:
                     return new PrintStmt(printStmt.SourceLocation, ReplaceVars(printStmt.Message, varMap));
                 case RaiseStmt raiseStmt:
                     List<IPExpr> payload = new List<IPExpr>();
                     foreach(var p in raiseStmt.Payload) payload.Add(ReplaceVars(p, varMap));
                     return new RaiseStmt(raiseStmt.SourceLocation, raiseStmt.PEvent, payload);
                 case ReceiveStmt receiveStmt:
                     Dictionary<PEvent, Function> cases = new Dictionary<PEvent, Function>();
                     foreach(KeyValuePair<PEvent, Function> entry in receiveStmt.Cases)
                     {
                         Function replacement = new Function(entry.Value.Name, entry.Value.SourceLocation);
                         replacement.Owner = entry.Value.Owner;
                         replacement.ParentFunction = entry.Value.ParentFunction;
                         replacement.CanReceive = entry.Value.CanReceive;
                         replacement.Role = entry.Value.Role;
                         replacement.Scope = entry.Value.Scope;
                         foreach (var local in entry.Value.LocalVariables) replacement.AddLocalVariable(local);
                         foreach (var i in entry.Value.CreatesInterfaces) replacement.AddCreatesInterface(i);
                         foreach (var param in entry.Value.Signature.Parameters) replacement.Signature.Parameters.Add(param);
                         replacement.Signature.ReturnType = entry.Value.Signature.ReturnType;
                         foreach (var callee in entry.Value.Callees) replacement.AddCallee(callee);
                         replacement.Body = (CompoundStmt) ReplaceVars(entry.Value.Body, varMap);
                         cases.Add(entry.Key, replacement);
                     }
                     return new ReceiveStmt(receiveStmt.SourceLocation, cases);
                 case RemoveStmt removeStmt:
                     return new RemoveStmt(removeStmt.SourceLocation, ReplaceVars(removeStmt.Variable, varMap), ReplaceVars(removeStmt.Value, varMap));
                 case ReturnStmt returnStmt:
                     return new ReturnStmt(returnStmt.SourceLocation, ReplaceVars(returnStmt.ReturnValue, varMap));
                 case SendStmt sendStmt:
                     List<IPExpr> sendArgs = new List<IPExpr>();
                     foreach (var arg in sendStmt.Arguments) sendArgs.Add(ReplaceVars(arg, varMap));
                     return new SendStmt(sendStmt.SourceLocation, ReplaceVars(sendStmt.MachineExpr, varMap), ReplaceVars(sendStmt.Evt, varMap), sendArgs);
                 case WhileStmt whileStmt:
                     return new WhileStmt(whileStmt.SourceLocation, ReplaceVars(whileStmt.Condition, varMap), ReplaceVars(whileStmt.Body, varMap));
                 default:
                     return stmt;
             }
         }

         static private IPExpr ReplaceVars(IPExpr expr, Dictionary<Variable,Variable> varMap)
         {
             switch(expr)
             {
                 case BinOpExpr binOpExpr:
                     return new BinOpExpr(binOpExpr.SourceLocation, binOpExpr.Operation, ReplaceVars(binOpExpr.Lhs, varMap), ReplaceVars(binOpExpr.Rhs, varMap));
                 case CastExpr castExpr:
                     return new CastExpr(castExpr.SourceLocation, ReplaceVars(castExpr.SubExpr, varMap), castExpr.Type);
                 case ChooseExpr chooseExpr:
                     return new ChooseExpr(chooseExpr.SourceLocation, ReplaceVars(chooseExpr.SubExpr, varMap), chooseExpr.Type);
                 case CloneExpr cloneExpr:
                     return ReplaceVars(cloneExpr.Term, varMap);
                 case CoerceExpr coerceExpr:
                     return new CoerceExpr(coerceExpr.SourceLocation, ReplaceVars(coerceExpr.SubExpr, varMap), coerceExpr.NewType);
                 case ContainsExpr containsExpr:
                     return new ContainsExpr(containsExpr.SourceLocation, ReplaceVars(containsExpr.Item, varMap), ReplaceVars(containsExpr.Collection, varMap));
                 case CtorExpr ctorExpr:
                     List<IPExpr> newArguments = new List<IPExpr>();
                     foreach (var arg in ctorExpr.Arguments) newArguments.Add(ReplaceVars(arg, varMap));
                     return new CtorExpr(ctorExpr.SourceLocation, ctorExpr.Interface, new List<IPExpr>(newArguments));
                 case FunCallExpr funCallExpr:
                     List<IPExpr> newArgs = new List<IPExpr>();
                     foreach (var arg in funCallExpr.Arguments) newArgs.Add(ReplaceVars(arg, varMap));
                     return new FunCallExpr(funCallExpr.SourceLocation, funCallExpr.Function, new List<IPExpr>(newArgs));
                 case KeysExpr keysExpr:
                     return new KeysExpr(keysExpr.SourceLocation, ReplaceVars(keysExpr.Expr, varMap), keysExpr.Type);
                 case MapAccessExpr mapAccessExpr:
                     return new MapAccessExpr(mapAccessExpr.SourceLocation, ReplaceVars(mapAccessExpr.MapExpr, varMap), ReplaceVars(mapAccessExpr.IndexExpr, varMap), mapAccessExpr.Type);
                 case NamedTupleAccessExpr namedTupleAccessExpr:
                     return new NamedTupleAccessExpr(namedTupleAccessExpr.SourceLocation, ReplaceVars(namedTupleAccessExpr.SubExpr, varMap), namedTupleAccessExpr.Entry);
                 case NamedTupleExpr namedTupleExpr:
                     List<IPExpr> newFields = new List<IPExpr>();
                     foreach (var field in namedTupleExpr.TupleFields) newFields.Add(ReplaceVars(field, varMap));
                     return new NamedTupleExpr(namedTupleExpr.SourceLocation, new List<IPExpr>(newFields), namedTupleExpr.Type);
                 case SeqAccessExpr seqAccessExpr:
                     return new SeqAccessExpr(seqAccessExpr.SourceLocation, ReplaceVars(seqAccessExpr.SeqExpr, varMap), ReplaceVars(seqAccessExpr.IndexExpr, varMap), seqAccessExpr.Type);
                 case SetAccessExpr setAccessExpr:
                     return new SetAccessExpr(setAccessExpr.SourceLocation, ReplaceVars(setAccessExpr.SetExpr, varMap), ReplaceVars(setAccessExpr.IndexExpr, varMap), setAccessExpr.Type);
                 case SizeofExpr sizeofExpr:
                     return new SizeofExpr(sizeofExpr.SourceLocation, ReplaceVars(sizeofExpr.Expr, varMap));
                 case StringExpr stringExpr:
                     List<IPExpr> newListArgs = new List<IPExpr>();
                     foreach (var arg in stringExpr.Args) newListArgs.Add(ReplaceVars(arg, varMap));
                     return new StringExpr(stringExpr.SourceLocation, stringExpr.BaseString, newListArgs);
                 case TupleAccessExpr tupleAccessExpr:
                     return new TupleAccessExpr(tupleAccessExpr.SourceLocation, ReplaceVars(tupleAccessExpr.SubExpr, varMap), tupleAccessExpr.FieldNo, tupleAccessExpr.Type);
                 case UnaryOpExpr unaryOpExpr:
                     return new UnaryOpExpr(unaryOpExpr.SourceLocation, unaryOpExpr.Operation, ReplaceVars(unaryOpExpr.SubExpr, varMap));
                 case UnnamedTupleExpr unnamedTupleExpr:
                     List<IPExpr> newUnnamedFields = new List<IPExpr>();
                     foreach (var field in unnamedTupleExpr.TupleFields) newUnnamedFields.Add(ReplaceVars(field, varMap));
                     return new UnnamedTupleExpr(unnamedTupleExpr.SourceLocation, new List<IPExpr>(newUnnamedFields));
                 case ValuesExpr valuesExpr:
                     return new ValuesExpr(valuesExpr.SourceLocation, ReplaceVars(valuesExpr.Expr, varMap), valuesExpr.Type);
                 case VariableAccessExpr variableAccessExpr:
                     if (varMap.ContainsKey(variableAccessExpr.Variable))
                         return new VariableAccessExpr(variableAccessExpr.SourceLocation, varMap[variableAccessExpr.Variable]);
                     else return variableAccessExpr;
                 default:
                     return expr;
             }
         }

         static private Function TransformFunction(Function function, Machine machine)
         {
             if (function.CanReceive != true) {
                 return function;
             }
             if (machine == null)
                throw new NotImplementedException($"Async functions {function.Name} are not supported");
             Function transformedFunction = new Function(function.Name, function.SourceLocation);
             transformedFunction.Owner = function.Owner;
             transformedFunction.ParentFunction = function.ParentFunction;
             foreach (var local in function.LocalVariables) transformedFunction.AddLocalVariable(local);
             foreach (var i in function.CreatesInterfaces) transformedFunction.AddCreatesInterface(i);
             transformedFunction.Role = function.Role;
             transformedFunction.Body = (CompoundStmt) HandleReceives(function.Body, function, machine);
             transformedFunction.Scope = function.Scope;
             transformedFunction.CanChangeState = function.CanChangeState;
             transformedFunction.CanRaiseEvent = function.CanRaiseEvent;
             transformedFunction.CanReceive = function.CanReceive;
             transformedFunction.IsNondeterministic = function.IsNondeterministic;
             foreach (var param in function.Signature.Parameters) transformedFunction.Signature.Parameters.Add(param);
             transformedFunction.Signature.ReturnType = function.Signature.ReturnType;
             return transformedFunction;
         }

         static private IPStmt ReplaceBreaks(IPStmt stmt)
         {
             if (stmt == null) return null;
             switch(stmt)
             {
                 case CompoundStmt compoundStmt:
                     List<IPStmt> statements = new List<IPStmt>();
                     foreach (var inner in compoundStmt.Statements)
                     {
                         statements.Add(ReplaceBreaks(inner));
                     }
                     return new CompoundStmt(compoundStmt.SourceLocation, statements);
                 case IfStmt ifStmt:
                     return new IfStmt(ifStmt.SourceLocation, ifStmt.Condition, ReplaceBreaks(ifStmt.ThenBranch), ReplaceBreaks(ifStmt.ElseBranch));
                 case ReceiveStmt receiveStmt:
                     Dictionary<PEvent, Function> cases = new Dictionary<PEvent, Function>();
                     foreach(KeyValuePair<PEvent, Function> entry in receiveStmt.Cases)
                     {
                         Function replacement = new Function(entry.Value.Name, entry.Value.SourceLocation);
                         replacement.Owner = entry.Value.Owner;
                         replacement.ParentFunction = entry.Value.ParentFunction;
                         replacement.CanReceive = entry.Value.CanReceive;
                         replacement.Role = entry.Value.Role;
                         replacement.Scope = entry.Value.Scope;
                         foreach (var local in entry.Value.LocalVariables) replacement.AddLocalVariable(local);
                         foreach (var i in entry.Value.CreatesInterfaces) replacement.AddCreatesInterface(i);
                         foreach (var param in entry.Value.Signature.Parameters) replacement.Signature.Parameters.Add(param);
                         replacement.Signature.ReturnType = entry.Value.Signature.ReturnType;
                         foreach (var callee in entry.Value.Callees) replacement.AddCallee(callee);
                         replacement.Body = (CompoundStmt) ReplaceBreaks(entry.Value.Body);
                         cases.Add(entry.Key, replacement);
                     }
                     return new ReceiveStmt(receiveStmt.SourceLocation, cases);
                 case BreakStmt _:
                     return new ReturnStmt(stmt.SourceLocation, null);
                 default:
                     return stmt;
             }
         }

         static private IPStmt HandleReceives(IPStmt statement, Function function, Machine machine)
         {
             switch (statement)
             {
                 case CompoundStmt compound:
                     IEnumerator<IPStmt> enumerator = compound.Statements.GetEnumerator();
                     if (enumerator.MoveNext())
                     {
                         IPStmt first = enumerator.Current;
                         List<IPStmt> afterStmts = new List<IPStmt>();
                         while (enumerator.MoveNext())
                         {
                             afterStmts.Add(enumerator.Current);
                         } 
                         CompoundStmt after = null;
                         if (afterStmts.Count > 0)
                         {
                             after = new CompoundStmt(afterStmts[0].SourceLocation, afterStmts);
                             after = (CompoundStmt) HandleReceives(after, function, machine);
                         }
                         List<IPStmt> result = new List<IPStmt>();
                         switch (first)
                         {
                              case CompoundStmt nestedCompound:
                                  List<IPStmt> compoundStmts = new List<IPStmt>(nestedCompound.Statements);
                                  foreach (var stmt in afterStmts)
                                  {
                                      compoundStmts.Add(stmt);
                                  }
                                  result.Add(HandleReceives(new CompoundStmt(nestedCompound.SourceLocation, compoundStmts), function, machine));
                                  break;
                              case IfStmt cond:
                                  List<IPStmt> thenStmts = new List<IPStmt>();
                                  List<IPStmt> elseStmts = new List<IPStmt>();
                                  if (cond.ThenBranch != null)
                                      thenStmts = new List<IPStmt>(cond.ThenBranch.Statements);
                                  if (cond.ElseBranch != null)
                                      elseStmts = new List<IPStmt>(cond.ElseBranch.Statements);
                                  foreach (var stmt in afterStmts)
                                  {
                                      thenStmts.Add(stmt);
                                      elseStmts.Add(stmt);
                                  }
                                  CompoundStmt thenBody = new CompoundStmt(cond.SourceLocation, thenStmts);
                                  CompoundStmt elseBody = new CompoundStmt(cond.SourceLocation, elseStmts);
                        
                                  result.Add(new IfStmt(cond.SourceLocation, cond.Condition, HandleReceives(thenBody, function, machine), HandleReceives(elseBody, function, machine)));
                                  break;
                             case ReceiveStmt recv:
                                 IDictionary<PEvent, Function> cases = new Dictionary<PEvent, Function>();
                                 foreach (KeyValuePair<PEvent, Function> c in recv.Cases)
                                 {
                                     c.Value.AddLocalVariables(function.Signature.Parameters);
                                     c.Value.AddLocalVariables(function.LocalVariables);
                                     cases.Add(c.Key, TransformFunction(c.Value, machine));
                                 }
                                 Continuation continuation = GetContinuation(function, cases, after, recv.SourceLocation);
                                 if (machine != null) machine.AddMethod(continuation);
                                 foreach (Variable v in continuation.StoreParameters)
                                 {
                                     machine.AddField(v);
                                 }
                                 foreach (AssignStmt store in continuation.StoreStmts)
                                 {
                                     result.Add(store);
                                 }
                                 ReceiveSplitStmt split = new ReceiveSplitStmt(compound.SourceLocation, continuation);
                                 result.Add(split);
                                 break;
                             case WhileStmt loop:
                                 // turn the while statement into a recursive function
                                 WhileFunction rec = new WhileFunction(loop.SourceLocation);
                                 rec.Owner = function.Owner;
                                 rec.ParentFunction = function;
                                 foreach (var param in function.Signature.Parameters) rec.AddParameter(param);
                                 Dictionary<Variable,Variable> newVarMap = new Dictionary<Variable,Variable>();
                                 foreach (var local in function.LocalVariables)
                                 {
                                     Variable machineVar = new Variable($"while_{whileNumber}_{local.Name}", local.SourceLocation, local.Role);
                                     machineVar.Type = local.Type;
                                     machine.AddField(machineVar);
                                     newVarMap.Add(local, machineVar);
                                 }
                                 foreach (var i in function.CreatesInterfaces) rec.AddCreatesInterface(i);
                                 rec.CanChangeState = function.CanChangeState;
                                 rec.CanRaiseEvent = function.CanRaiseEvent;
                                 rec.CanReceive = function.CanReceive;
                                 rec.IsNondeterministic = function.IsNondeterministic;
                                 // make while loop body
                                 List<IPStmt> loopBody = new List<IPStmt>();
                                 IEnumerator<IPStmt> bodyEnumerator = loop.Body.Statements.GetEnumerator();
                                 while (bodyEnumerator.MoveNext())
                                 {   
                                     IPStmt stmt = bodyEnumerator.Current;
                                     IPStmt replaceBreak = ReplaceBreaks(stmt);
                                     if (replaceBreak != null) {
                                         loopBody.Add(ReplaceVars(replaceBreak, newVarMap));
                                     }
                                 }
                                 List<VariableAccessExpr> recArgs = new List<VariableAccessExpr>();
                                 foreach (var param in rec.Signature.Parameters)
                                 {
                                     recArgs.Add(new VariableAccessExpr(rec.SourceLocation, param));
                                 }
                                 // call the function
                                 FunCallStmt recCall = new FunCallStmt(loop.SourceLocation, rec, recArgs);
                                 loopBody = new List<IPStmt>(((CompoundStmt) HandleReceives(new CompoundStmt(rec.SourceLocation, loopBody), rec, machine)).Statements);
                                 rec.Body = new CompoundStmt(rec.SourceLocation, loopBody);
                                 if (machine != null) machine.AddMethod(rec);
                                 // assign local variables
                                 foreach (var local in function.LocalVariables)
                                 {
                                     result.Add(new AssignStmt(local.SourceLocation, new VariableAccessExpr(local.SourceLocation, newVarMap[local]), new VariableAccessExpr(local.SourceLocation, local)));
                                 }
                                 // replace the while statement with a function call
                                 result.Add(recCall);
                                 if (after != null)
                                 {
                                     foreach (var stmt in after.Statements)
                                     {
                                         result.Add(ReplaceVars(stmt, newVarMap));
                                     }
                                 }
                                 whileNumber++;
                                 break;
                             default:
                                 if (after == null) return compound;
                                 result.Add(first);
                                 foreach (IPStmt stmt in after.Statements) result.Add(HandleReceives(stmt, function, machine));
                                 break;
                         }
                         return new CompoundStmt(compound.SourceLocation, result);
                     }
                     return compound;
/*
                 case FunCallStmt call:
                     Function callee = TransformFunction(callee, machine);
                     FunCallStmt newCall = new FunCallStmt(SourceLocation, callee, call.ArgsList);
                     function.AddCallee(callee);
                     return newCall; 
*/
                 default:
                     return statement;
             }
         }
 
         static private Continuation GetContinuation(Function function, IDictionary<PEvent, Function> cases, IPStmt after, ParserRuleContext location)
         {
             Continuation continuation = new Continuation(new Dictionary<PEvent, Function>(cases), after, location);
             continuation.ParentFunction = function;
             function.AddCallee(continuation);
             function.Role = FunctionRole.Method;
             foreach (var v in function.Signature.Parameters) {
                 Variable local = new Variable(v.Name, v.SourceLocation, v.Role);
                 Variable store = new Variable($"continuation_{continuationNumber}_{v.Name}", v.SourceLocation, v.Role);
                 local.Type = v.Type;
                 store.Type = v.Type;
                 continuation.AddParameter(local, store);
             }
             foreach (var v in function.LocalVariables)
             {
                 Variable local = new Variable(v.Name, v.SourceLocation, v.Role);
                 Variable store = new Variable($"continuation_{continuationNumber}_{v.Name}", v.SourceLocation, v.Role);
                 local.Type = v.Type;
                 store.Type = v.Type;
                 continuation.AddParameter(local, store);
             }
             continuation.CanChangeState = function.CanChangeState;
             continuation.CanRaiseEvent = function.CanRaiseEvent;
             continuation.Signature.ReturnType = function.Signature.ReturnType;
             continuationNumber++;
             return continuation;
         }
    }
}
