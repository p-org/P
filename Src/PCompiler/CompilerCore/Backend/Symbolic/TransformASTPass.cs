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

         static public List<IPDecl> GetTransformedDecls(Scope globalScope)
         {
            continuationNumber = 0;
            List<IPDecl> decls = new List<IPDecl>();

            foreach (var decl in globalScope.AllDecls)
                decls.Add(TransformDecl(decl));

            continuationNumber = 0;
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
                        return TransformFunction(function, null);
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
                Function transformedFunction = TransformFunction(method, transformedMachine);
                functionMap.Add(method, transformedFunction);
                transformedMachine.AddMethod(transformedFunction);
            }
            transformedMachine.StartState = machine.StartState;
            transformedMachine.Observes = machine.Observes;
            transformedMachine.PayloadType = machine.PayloadType;
            transformedMachine.Scope = machine.Scope;
            transformedMachine.Semantics = machine.Semantics;
            foreach (var state in machine.States)
            {
                transformedMachine.AddState(TransformState(state, functionMap));
            }
            foreach (var group in machine.Groups) transformedMachine.AddGroup(group);
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

         static private IPStmt HandleReceives(IPStmt statement, Function function, Machine machine)
         {
             switch (statement)
             {
                 case IfStmt cond:
                     return new IfStmt(cond.SourceLocation, cond.Condition, HandleReceives(cond.ThenBranch, function, machine), HandleReceives(cond.ElseBranch, function, machine));
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
                             case ReceiveStmt recv:
                                 IDictionary<PEvent, Function> cases = new Dictionary<PEvent, Function>();
                                 foreach (KeyValuePair<PEvent, Function> c in recv.Cases)
                                 {
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
                                 foreach (var local in function.LocalVariables)
                                 {
                                     rec.AddParameter(local);
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
                                     switch (stmt)
                                     {
                                         case BreakStmt _:
                                             loopBody.Add(new ReturnStmt(rec.SourceLocation, null));
                                             break;
                                         default:
                                             loopBody.Add(stmt);
                                             break;
                                     }
                                 }
                                 List<VariableAccessExpr> recArgs = new List<VariableAccessExpr>();
                                 foreach (var param in rec.Signature.Parameters)
                                 {
                                     recArgs.Add(new VariableAccessExpr(rec.SourceLocation, param));
                                 }
                                 // call the function
                                 FunCallStmt recCall = new FunCallStmt(loop.SourceLocation, rec, recArgs);
                                 loopBody.Add(recCall);
                                 loopBody = new List<IPStmt>(((CompoundStmt) HandleReceives(new CompoundStmt(rec.SourceLocation, loopBody), rec, machine)).Statements);
                                 IfStmt ifStmt = new IfStmt(rec.SourceLocation, loop.Condition, new CompoundStmt(rec.SourceLocation, loopBody), new CompoundStmt(rec.SourceLocation, afterStmts));
                                 rec.Body = CompoundStmt.FromStatement(ifStmt);
                                 if (machine != null) machine.AddMethod(rec);
                                 // replace the while statement with a function call
                                 return CompoundStmt.FromStatement(recCall);
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
                 continuationNumber++;
             }
             foreach (var v in function.LocalVariables)
             {
                 Variable local = new Variable(v.Name, v.SourceLocation, v.Role);
                 Variable store = new Variable($"continuation_{continuationNumber}_{v.Name}", v.SourceLocation, v.Role);
                 local.Type = v.Type;
                 store.Type = v.Type;
                 continuation.AddParameter(local, store);
                 continuationNumber++;
             }
             continuation.CanChangeState = function.CanChangeState;
             continuation.CanRaiseEvent = function.CanRaiseEvent;
             return continuation;
         }
    }
}
