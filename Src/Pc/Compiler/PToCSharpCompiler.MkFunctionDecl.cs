using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Formula.API;
using Microsoft.Formula.API.Nodes;

namespace Microsoft.Pc
{
    internal partial class PToCSharpCompiler
    {
        internal class MkFunctionDecl
        {
            public string FunName;
            public FunInfo funInfo;
            public MkMachineClass Owner;  // null if global function
            private readonly PToCSharpCompiler pToCSharp;
            public Stack<bool> LhsStack = new Stack<bool>();
            private int labelCount; // labels are used for "continuations" in send, new, nondet, receive, function calls

            public MkFunctionDecl(string funName, FunInfo funInfo, MkMachineClass owner, PToCSharpCompiler pToCSharp)
            {
                FunName = funName;
                this.funInfo = funInfo;
                Owner = owner;
                this.pToCSharp = pToCSharp;
            }

            public string FunClassName => $"{FunName}_Class";

            public int GetFreshLabelId()
            {
                labelCount++;
                return labelCount;
            }

            public string GetLabelFromLabelId(int i)
            {
                return string.Format("{0}_{1}", FunName, i);
            }

            public SwitchStatementSyntax EmitLabelPrelude()
            {
                SyntaxList<SwitchSectionSyntax> caseList = new SyntaxList<SwitchSectionSyntax>();
                for (int i = 1; i <= labelCount; i++)
                {
                    SyntaxList<SwitchLabelSyntax> switchLabels = new SyntaxList<SwitchLabelSyntax>();
                    switchLabels = switchLabels.Add(SyntaxFactory.CaseSwitchLabel(CSharpHelper.MkCSharpNumericLiteralExpression(i)));
                    SyntaxList<StatementSyntax> switchStmts = new SyntaxList<StatementSyntax>();
                    switchStmts = switchStmts.Add(CSharpHelper.MkCSharpGoto(GetLabelFromLabelId(i)));
                    caseList = caseList.Add(SyntaxFactory.SwitchSection(switchLabels, switchStmts));
                }
                return SyntaxFactory.SwitchStatement(CSharpHelper.MkCSharpDot("currFun", "returnToLocation"), caseList);
            }

            private FuncTerm LookupType(Node node)
            {
                return funInfo.typeInfo[Factory.Instance.ToAST(node)];
            }

            #region FoldUnfold
            private IEnumerable<Node> Unfold(Node n)
            {
                if (n.NodeKind != NodeKind.FuncTerm)
                {
                    yield break;
                }

                var ft = (FuncTerm) n;
                string funName = ((Id) ft.Function).Name;
                if (funName == PData.Con_New.Node.Name)
                {
                    Debug.Assert(false, "New expr in ZingUnfold");
                }
                else if (funName == PData.Con_Print.Node.Name)
                {
                    foreach (Node a in Unfold(GetArgByIndex(ft, 2)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_Goto.Node.Name)
                {
                    foreach (Node a in Unfold(GetArgByIndex(ft, 1)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_Announce.Node.Name || funName == PData.Con_Raise.Node.Name)
                {
                    yield return GetArgByIndex(ft, 0);

                    foreach (Node a in Unfold(GetArgByIndex(ft, 1)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_Send.Node.Name)
                {
                    yield return GetArgByIndex(ft, 0);
                    yield return GetArgByIndex(ft, 1);

                    foreach (Node a in Unfold(GetArgByIndex(ft, 2)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_Receive.Node.Name) { }
                else if (funName == PData.Con_FunApp.Node.Name)
                {
                    foreach (Node a in Unfold(GetArgByIndex(ft, 1)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_FunStmt.Node.Name || funName == PData.Con_NewStmt.Node.Name)
                {
                    foreach (Node a in Unfold(GetArgByIndex(ft, 1)))
                    {
                        yield return a;
                    }

                    Node node = GetArgByIndex(ft, 2);
                    if (node.NodeKind != NodeKind.Id)
                    {
                        yield return node;
                    }
                }
                else if (funName == PData.Con_BinApp.Node.Name)
                {
                    string opName = ((Id) GetArgByIndex(ft, 0)).Name;
                    if (opName == PData.Cnst_Idx.Node.Name && LhsStack.Count > 0 && LhsStack.Peek())
                    {
                        LhsStack.Push(true);
                        yield return GetArgByIndex(ft, 1);

                        LhsStack.Pop();
                        LhsStack.Push(false);
                        yield return GetArgByIndex(ft, 2);

                        LhsStack.Pop();
                    }
                    else
                    {
                        yield return GetArgByIndex(ft, 1);
                        yield return GetArgByIndex(ft, 2);
                    }
                }
                else if (funName == PData.Con_Name.Node.Name || funName == PData.Con_NulApp.Node.Name
                         || funName == PData.Con_UnApp.Node.Name || funName == PData.Con_Default.Node.Name
                         || funName == PData.Con_NulStmt.Node.Name)
                {
                    var first = true;
                    foreach (Node t in ft.Args)
                    {
                        if (first)
                        {
                            first = false;
                            continue;
                        }

                        yield return t;
                    }
                }
                else if (funName == PData.Con_Assert.Node.Name)
                {
                    yield return GetArgByIndex(ft, 0);
                }
                else if (funName == PData.Con_BinStmt.Node.Name)
                {
                    yield return GetArgByIndex(ft, 3);

                    string op = ((Id) GetArgByIndex(ft, 0)).Name;
                    if (op == PData.Cnst_Assign.Node.Name)
                    {
                        var lhs = (FuncTerm) GetArgByIndex(ft, 1);
                        string lhsName = ((Id) lhs.Function).Name;
                        if (lhsName == PData.Con_BinApp.Node.Name && ((Id) GetArgByIndex(lhs, 0)).Name == PData.Cnst_Idx.Node.Name)
                        {
                            LhsStack.Push(true);
                            yield return GetArgByIndex(lhs, 1);

                            LhsStack.Pop();
                            yield return GetArgByIndex(lhs, 2);
                        }
                        else if (lhsName == PData.Con_Field.Node.Name)
                        {
                            LhsStack.Push(true);
                            yield return GetArgByIndex(lhs, 0);

                            LhsStack.Pop();
                        }
                        else
                        {
                            LhsStack.Push(true);
                            yield return lhs;

                            LhsStack.Pop();
                        }
                    }
                    else
                    {
                        LhsStack.Push(true);
                        yield return GetArgByIndex(ft, 1);

                        LhsStack.Pop();
                    }
                }
                else if (funName == PData.Con_Field.Node.Name || funName == PData.Con_Cast.Node.Name || funName == PData.Con_Convert.Node.Name)
                {
                    yield return ft.Args.First();
                }
                else if (funName == PData.Con_Tuple.Node.Name)
                {
                    foreach (Node a in Unfold(GetArgByIndex(ft, 0)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_NamedTuple.Node.Name)
                {
                    foreach (Node a in Unfold(GetArgByIndex(ft, 0)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_Exprs.Node.Name)
                {
                    do
                    {
                        yield return GetArgByIndex(ft, 1);

                        ft = GetArgByIndex(ft, 2) as FuncTerm;
                    } while (ft != null);
                }
                else if (funName == PData.Con_NamedExprs.Node.Name)
                {
                    do
                    {
                        yield return GetArgByIndex(ft, 1);

                        ft = GetArgByIndex(ft, 2) as FuncTerm;
                    } while (ft != null);
                }
                else if (funName == PData.Con_Seq.Node.Name)
                {
                    using (IEnumerator<Node> it = ft.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        yield return it.Current;

                        it.MoveNext();
                        yield return it.Current;
                    }
                }
                else if (funName == PData.Con_Ite.Node.Name)
                {
                    using (IEnumerator<Node> it = ft.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        yield return it.Current;

                        it.MoveNext();
                        yield return it.Current;

                        it.MoveNext();
                        yield return it.Current;
                    }
                }
                else if (funName == PData.Con_While.Node.Name)
                {
                    using (IEnumerator<Node> it = ft.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        yield return it.Current;

                        it.MoveNext();
                        yield return it.Current;
                    }
                }
                else
                {
                    foreach (Node t in ft.Args)
                    {
                        yield return t;
                    }
                }
            }

            private SyntaxNode Fold(Node n, List<SyntaxNode> children)
            {
                if (n.NodeKind == NodeKind.Id || n.NodeKind == NodeKind.Cnst)
                {
                    return null;
                }

                var ft = (FuncTerm)n;
                var funName = ((Id)ft.Function).Name;

                if (funName == PData.Con_Name.Node.Name)
                {
                    return FoldName(ft);
                }

                if (funName == PData.Con_Receive.Node.Name)
                {
                    return FoldReceive(ft);
                }

                if (funName == PData.Con_FunApp.Node.Name)
                {
                    return FoldFunApp(ft, children);
                }

                if (funName == PData.Con_NulApp.Node.Name)
                {
                    return FoldNulApp(ft);
                }

                if (funName == PData.Con_UnApp.Node.Name)
                {
                    return FoldUnApp(ft, children);
                }

                if (funName == PData.Con_BinApp.Node.Name)
                {
                    return FoldBinApp(ft, children);
                }

                if (funName == PData.Con_Field.Node.Name)
                {
                    return FoldField(ft, children);
                }

                if (funName == PData.Con_Default.Node.Name)
                {
                    return FoldDefault(ft);
                }

                if (funName == PData.Con_Cast.Node.Name)
                {
                    return FoldCast(ft, children);
                }

                if (funName == PData.Con_Convert.Node.Name)
                {
                    return FoldConvert(ft, children);
                }

                if (funName == PData.Con_Tuple.Node.Name)
                {
                    return FoldTuple(children);
                }

                if (funName == PData.Con_NamedTuple.Node.Name)
                {
                    return FoldNamedTuple(ft, children);
                }

                if (funName == PData.Con_NewStmt.Node.Name)
                {
                    return FoldNewStmt(ft, children);
                }

                if (funName == PData.Con_Goto.Node.Name)
                {
                    return FoldGoto(ft, children);
                }

                if (funName == PData.Con_Raise.Node.Name)
                {
                    return FoldRaise(ft, children);
                }

                if (funName == PData.Con_Send.Node.Name)
                {
                    return FoldSend(children);
                }

                if (funName == PData.Con_Announce.Node.Name)
                {
                    return FoldAnnounce(children);
                }

                if (funName == PData.Con_FunStmt.Node.Name)
                {
                    return FoldFunStmt(ft, children);
                }

                if (funName == PData.Con_NulStmt.Node.Name)
                {
                    return FoldNulStmt(ft);
                }

                if (funName == PData.Con_Assert.Node.Name)
                {
                    return FoldAssert(ft, children);
                }

                if (funName == PData.Con_Print.Node.Name)
                {
                    return FoldPrint(ft, children);
                }

                if (funName == PData.Con_BinStmt.Node.Name)
                {
                    return FoldBinStmt(ft, children);
                }

                if (funName == PData.Con_Return.Node.Name)
                {
                    return FoldReturn(children);
                }

                if (funName == PData.Con_While.Node.Name)
                {
                    return FoldWhile(children);
                }

                if (funName == PData.Con_Ite.Node.Name)
                {
                    return FoldIte(children);
                }

                if (funName == PData.Con_Seq.Node.Name)
                {
                    return FoldSeq(children);
                }

                if (funName == PData.Con_IdList.Node.Name)
                {
                    //return ZingData.Cnst_Nil;
                    return SyntaxFactory.IdentifierName("NIL");
                }

                Console.WriteLine("Unknown term name: " + funName);
                throw new NotImplementedException();
            }

            private List<StatementSyntax> CaseFunCallHelper(List<string> eventNames, List<string> funNames, string afterAfterLabel)
            {
                List<StatementSyntax> eventStmts = new List<StatementSyntax>();
                List<StatementSyntax> funStmts = new List<StatementSyntax>();

                for (int i = 0; i < eventNames.Count; i++)
                {
                    var beforeLabelId = GetFreshLabelId();
                    var beforeLabel = GetLabelFromLabelId(beforeLabelId);
                    var eventName = eventNames[i];
                    var funName = funNames[i];
                    var calleeInfo = pToCSharp.allGlobalFuns.ContainsKey(funName) ? pToCSharp.allGlobalFuns[funName] : pToCSharp.allMachines[Owner.machineName].funNameToFunInfo[funName];
                    Debug.Assert(calleeInfo.isAnonymous);
                    List<StatementSyntax> ifStmts = new List<StatementSyntax>();
                    ifStmts.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                                    CSharpHelper.MkCSharpElementAccessExpression(CSharpHelper.MkCSharpDot("currFun", "locals"), CSharpHelper.MkCSharpNumericLiteralExpression(calleeInfo.localNameToInfo[calleeInfo.PayloadVarName].index)),
                                    CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "currentPayload", "Clone"))));
                    foreach (var calleeLocal in calleeInfo.localNames)
                    {
                        var calleeLocalInfo = calleeInfo.localNameToInfo[calleeLocal];
                        ifStmts.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                                        CSharpHelper.MkCSharpElementAccessExpression(CSharpHelper.MkCSharpDot("currFun", "locals"), CSharpHelper.MkCSharpNumericLiteralExpression(calleeLocalInfo.index)),
                                        CSharpHelper.MkCSharpInvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("PrtValue"), SyntaxFactory.IdentifierName("PrtMkDefaultValue")), pToCSharp.typeContext.PTypeToCSharpExpr(calleeLocalInfo.type))));
                    }
                    ifStmts.Add(SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(
                                                        CSharpHelper.MkCSharpDot("parent", "PrtPushFunStackFrame"),
                                                        SyntaxFactory.IdentifierName(funName), CSharpHelper.MkCSharpDot("currFun", "locals"))));
                    ifStmts.Add(CSharpHelper.MkCSharpGoto(beforeLabel));
                    eventStmts.Add(SyntaxFactory.IfStatement(CSharpHelper.MkCSharpEquals(CSharpHelper.MkCSharpDot("parent", "currentTrigger"), pToCSharp.GetEventVar(eventName)), SyntaxFactory.Block(ifStmts)));
                    funStmts.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(beforeLabel));
                    funStmts.Add(SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(funName, "Execute"), SyntaxFactory.IdentifierName("application"), SyntaxFactory.IdentifierName("parent"))));
                    var elseStmt = SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "PrtPushFunStackFrame"), CSharpHelper.MkCSharpDot("currFun", "fun"), CSharpHelper.MkCSharpDot("currFun", "locals"), CSharpHelper.MkCSharpNumericLiteralExpression(beforeLabelId))),
                                         SyntaxFactory.ReturnStatement());
                    funStmts.Add(SyntaxFactory.IfStatement(
                                     CSharpHelper.MkCSharpEq(CSharpHelper.MkCSharpDot("parent", "continuation", "reason"), SyntaxFactory.IdentifierName("PrtContinuationReason.Return")),
                                     CSharpHelper.MkCSharpGoto(afterAfterLabel),
                                     SyntaxFactory.ElseClause(elseStmt)));
                }
                List<StatementSyntax> stmts = new List<StatementSyntax>();
                stmts.AddRange(eventStmts);
                stmts.Add(CSharpHelper.MkCSharpAssert(CSharpHelper.MkCSharpFalseLiteralExpression(), "Internal error"));
                stmts.AddRange(funStmts);
                return stmts;
            }

            private SyntaxNode FoldReceive(FuncTerm ft)
            {
                List<StatementSyntax> stmts = new List<StatementSyntax>();
                List<string> eventNames = new List<string>();
                List<string> funNames = new List<string>();
                var cases = GetArgByIndex(ft, 0) as FuncTerm;
                while (cases != null)
                {
                    Node evt = GetArgByIndex(cases, 0);
                    string eventName = null;
                    if (evt is Cnst)
                    {
                        eventName = (evt as Cnst).GetStringValue();
                    }
                    else if ((evt as Id).Name == "NULL")
                    {
                        eventName = NullEvent;
                    }
                    else
                    {
                        eventName = HaltEvent;
                    }
                    eventNames.Add(eventName);
                    stmts.Add(SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(
                                                      CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtImplMachine", SyntaxFactory.IdentifierName("parent")), "receiveSet", "Add"), 
                                                      pToCSharp.GetEventVar(eventName))));
                    var fun = GetArgByIndex(cases, 1);
                    string funName = pToCSharp.anonFunToName[Factory.Instance.ToAST(fun)];
                    funNames.Add(funName);
                    cases = GetArgByIndex(cases, 2) as FuncTerm;
                }
                var afterLabelId = GetFreshLabelId();
                var afterLabel = GetLabelFromLabelId(afterLabelId);
                stmts.Add(SyntaxFactory.ExpressionStatement(
                              CSharpHelper.MkCSharpInvocationExpression(
                                  CSharpHelper.MkCSharpDot("parent", "PrtFunContReceive"), 
                                  SyntaxFactory.ThisExpression(), 
                                  CSharpHelper.MkCSharpDot("currFun", "locals"), 
                                  CSharpHelper.MkCSharpNumericLiteralExpression(afterLabelId))));
                stmts.Add(SyntaxFactory.ReturnStatement());
                stmts.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(afterLabel));
                var afterAfterLabelId = GetFreshLabelId();
                var afterAfterLabel = GetLabelFromLabelId(afterAfterLabelId);
                stmts.AddRange(CaseFunCallHelper(eventNames, funNames, afterAfterLabel));
                stmts.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(afterAfterLabel));
                return SyntaxFactory.Block(stmts);
            }

            private SyntaxNode FoldName(FuncTerm ft)
            {
                var name = GetName(ft, 0);
                if (funInfo != null && funInfo.localNameToInfo.ContainsKey(name))
                {
                    //local var of a function:
                    return CSharpHelper.MkCSharpDot("currFun", VarName(name));
                }

                if (Owner != null && pToCSharp.allMachines[Owner.machineName].localVariableToVarInfo.ContainsKey(name))
                {
                    return CSharpHelper.MkCSharpDot("parent", VarName(name));
                }

                var type = LookupType(ft);
                if (PTypeEvent.Equals(Factory.Instance.ToAST(type)))
                {
                    return pToCSharp.GetEventVar(name);
                }
                // enum constant
                var enumTypeName = (GetArgByIndex(type, 0) as Cnst).GetStringValue();
                return CSharpHelper.MkCSharpObjectCreationExpression(
                    SyntaxFactory.IdentifierName("PrtEnumValue"),
                    CSharpHelper.MkCSharpStringLiteralExpression(name),
                    CSharpHelper.MkCSharpNumericLiteralExpression(pToCSharp.allEnums[enumTypeName][name]));
            }

            private SyntaxNode FoldNewStmt(FuncTerm ft, List<SyntaxNode> children)
            {
                SyntaxNode aout = null;
                if (GetArgByIndex(ft, 2).NodeKind != NodeKind.Id)
                {
                    aout = children.Last();
                    children.RemoveAt(children.Count - 1);
                }
                var createdIorM = GetName(ft, 0);
                var payloadVar = MkPayload(children);
                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                if (aout != null)
                {
                    stmtList.Add(
                        CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(aout,
                                                                                 CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("application", "CreateInterface"), SyntaxFactory.IdentifierName("parent"), CSharpHelper.MkCSharpStringLiteralExpression(createdIorM), payloadVar)));
                }
                else
                {
                    stmtList.Add(
                        SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("application", "CreateInterface"), SyntaxFactory.IdentifierName("parent"), CSharpHelper.MkCSharpStringLiteralExpression(createdIorM), payloadVar))
                    );
                }
                int afterLabelId = GetFreshLabelId();
                string afterLabel = GetLabelFromLabelId(afterLabelId);
                stmtList.Add(SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "PrtFunContNewMachine"), SyntaxFactory.ThisExpression(), CSharpHelper.MkCSharpDot("currFun", "locals"), CSharpHelper.MkCSharpNumericLiteralExpression(afterLabelId))));
                stmtList.Add(SyntaxFactory.ReturnStatement());
                stmtList.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(afterLabel));
                return SyntaxFactory.Block(stmtList);
            }

            private SyntaxNode FoldFunApp(FuncTerm ft, List<SyntaxNode> children)
            {
                string calleeName = (GetArgByIndex(ft, 0) as Cnst).GetStringValue();
                var paramList = new List<ExpressionSyntax>();
                paramList.Add(SyntaxFactory.IdentifierName("application"));
                paramList.Add(SyntaxFactory.IdentifierName("parent"));
                children.ForEach(x => paramList.Add((ExpressionSyntax)x));
                return CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(calleeName, "ExecuteToCompletion"), paramList.ToArray());
            }

            private SyntaxNode FoldNulApp(FuncTerm ft)
            {
                //No children
                var n = GetArgByIndex(ft, 0);

                if (n.NodeKind == NodeKind.Cnst)
                {
                    var val = ((Cnst)n).GetNumericValue();
                    
                        return CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtIntValue"),
                                                                         CSharpHelper.MkCSharpNumericLiteralExpression((Int64)val.Numerator));
                    
                        
                }

                //is float
                if(n is FuncTerm)
                {
                    if (((Id)((FuncTerm)n).Function).Name == PData.Cnst_Float.Node.Name)
                    {
                        var num = GetArgByIndex((FuncTerm)n, 0);
                        var val = ((Cnst)num).GetNumericValue();
                        return CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtFloatValue"),
                                                                             CSharpHelper.MkCSharpNumericLiteralExpression((double)val.Numerator / (double)val.Denominator));
                    }
                }

                // n.NodeKind == NodeKind.Id
                var op = ((Id)n).Name;
                if (op == PData.Cnst_True.Node.Name)
                {
                    return CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtBoolValue"),
                                                                         CSharpHelper.MkCSharpTrueLiteralExpression());
                }

                if (op == PData.Cnst_False.Node.Name)
                {
                    return CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtBoolValue"),
                                                                         CSharpHelper.MkCSharpFalseLiteralExpression());
                }

                if (op == PData.Cnst_This.Node.Name)
                {
                    return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("parent"), SyntaxFactory.IdentifierName("self"));
                }

                if (op == PData.Cnst_Nondet.Node.Name || op == PData.Cnst_FairNondet.Node.Name)
                {
                    return CSharpHelper.MkCSharpObjectCreationExpression(
                        SyntaxFactory.IdentifierName("PrtBoolValue"), 
                        CSharpHelper.MkCSharpInvocationExpression(
                            CSharpHelper.MkCSharpDot("application", "GetSelectedChoiceValue"),
                            CSharpHelper.MkCSharpCastExpression("PrtImplMachine", SyntaxFactory.IdentifierName("parent"))));
                }

                if (op == PData.Cnst_Null.Node.Name)
                {
                    return pToCSharp.GetEventVar(NullEvent);
                }
                //op == PData.Cnst_Halt.Node.Name
                return pToCSharp.GetEventVar(HaltEvent);
            }

            private SyntaxNode FoldUnApp(FuncTerm ft, List<SyntaxNode> children)
            {
                var op = ((Id)GetArgByIndex(ft, 0)).Name;
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var arg = it.Current;
                    if (op == PData.Cnst_Not.Node.Name)
                    {
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                            SyntaxFactory.IdentifierName("PrtBoolValue"),
                            SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtBoolValue", arg), "bl")));
                    }

                    if (op == PData.Cnst_Neg.Node.Name)
                    {
                        var negType = LookupType(ft);
                        var typeOp = ((Id)GetArgByIndex(negType, 0)).Name;
                        if (typeOp == PData.Cnst_Int.Node.Name)
                        {
                            return CSharpHelper.MkCSharpObjectCreationExpression(
                            SyntaxFactory.IdentifierName("PrtIntValue"),
                            SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg), "nt")));
                        }
                        else
                        {
                            return CSharpHelper.MkCSharpObjectCreationExpression(
                            SyntaxFactory.IdentifierName("PrtFloatValue"),
                            SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtFloatValue", arg), "ft")));
                        }
                    }
                    if (op == PData.Cnst_Keys.Node.Name)
                    {
                        return CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtMapValue", arg), "Keys"));
                    }

                    if (op == PData.Cnst_Values.Node.Name)
                    {
                        return CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtMapValue", arg), "Values"));
                    }
                    //  op == PData.Cnst_Sizeof.Node.Name
                    return CSharpHelper.MkCSharpObjectCreationExpression(
                        SyntaxFactory.IdentifierName("PrtIntValue"),
                        CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot((ExpressionSyntax)arg, "Size")));
                }
            }

            private SyntaxNode FoldBinApp(FuncTerm ft, List<SyntaxNode> children)
            {
                var op = ((Id)GetArgByIndex(ft, 0)).Name;
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var arg1 = (ExpressionSyntax)it.Current;
                    it.MoveNext();
                    var arg2 = (ExpressionSyntax)it.Current;

                    ExpressionSyntax arg1Val, arg2Val;
                    IdentifierNameSyntax returnType = null;
                    
                    Dictionary<string, SyntaxKind> IntOrFloatOpToSyntaxKind = new Dictionary<string, SyntaxKind>() {
                        { "ADD", SyntaxKind.AddExpression },
                        { "SUB", SyntaxKind.SubtractExpression },
                        { "MUL", SyntaxKind.MultiplyExpression },
                        { "DIV", SyntaxKind.DivideExpression }
                    };

                    Dictionary<string, SyntaxKind> BoolOpToSyntaxKind = new Dictionary<string, SyntaxKind>() {
                        { "AND", SyntaxKind.LogicalAndExpression },
                        { "OR", SyntaxKind.LogicalOrExpression }
                    };
                    Dictionary<string, SyntaxKind> CompOpToSyntaxKind = new Dictionary<string, SyntaxKind>() {
                        { "LT", SyntaxKind.LessThanExpression },
                        { "LE", SyntaxKind.LessThanOrEqualExpression },
                        { "GT", SyntaxKind.GreaterThanExpression },
                        { "GE", SyntaxKind.GreaterThanOrEqualExpression },
                        { "EQ", SyntaxKind.EqualsExpression },
                        { "NEQ", SyntaxKind.NotEqualsExpression }
                    };

                    if(IntOrFloatOpToSyntaxKind.ContainsKey(op))
                    {
                        var argType = LookupType(ft);
                        var typeRet = GetArgByIndex(argType, 0);
                        var typeRetName = typeRet is Id ? ((Id)typeRet).Name : /*NameType*/ PData.Cnst_Int.Node.Name;
                        if (typeRetName == PData.Cnst_Int.Node.Name)
                        {
                            arg1Val = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                            arg2Val = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                            returnType = SyntaxFactory.IdentifierName("PrtIntValue");
                        }
                        else
                        {
                            arg1Val = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtFloatValue", arg1), "ft");
                            arg2Val = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtFloatValue", arg2), "ft");
                            returnType = SyntaxFactory.IdentifierName("PrtFloatValue");
                        }

                        return CSharpHelper.MkCSharpObjectCreationExpression(
                            returnType,
                            SyntaxFactory.BinaryExpression(IntOrFloatOpToSyntaxKind[op], arg1Val, arg2Val));
                    }


                    //Boolean operations
                    if (BoolOpToSyntaxKind.ContainsKey(op))
                    {
                        var arg1Bool = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtBoolValue", arg1), "bl");
                        var arg2Bool = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtBoolValue", arg2), "bl");
     
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                            SyntaxFactory.IdentifierName("PrtBoolValue"),
                            SyntaxFactory.BinaryExpression(BoolOpToSyntaxKind[op], arg1Bool, arg2Bool));
                    }

                    //comparison operations
                    if(CompOpToSyntaxKind.ContainsKey(op))
                    {
                        var argType = LookupType(GetArgByIndex(ft, 1));
                        var typeRet = GetArgByIndex(argType, 0);
                        var typeRetName = typeRet is Id ? ((Id)typeRet).Name : /*NameType*/ PData.Cnst_Int.Node.Name;
                        if (typeRetName == PData.Cnst_Int.Node.Name)
                        {
                            arg1Val = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                            arg2Val = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                            returnType = SyntaxFactory.IdentifierName("PrtIntValue");
                        }
                        else
                        {
                            arg1Val = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtFloatValue", arg1), "ft");
                            arg2Val = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtFloatValue", arg2), "ft");
                            returnType = SyntaxFactory.IdentifierName("PrtFloatValue");
                        }

                        if (op == PData.Cnst_Eq.Node.Name)
                        {
                            return CSharpHelper.MkCSharpObjectCreationExpression(
                                SyntaxFactory.IdentifierName("PrtBoolValue"),
                                CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(arg1, "Equals"), arg2));
                        }

                        if (op == PData.Cnst_NEq.Node.Name)
                        {
                            return CSharpHelper.MkCSharpObjectCreationExpression(
                                SyntaxFactory.IdentifierName("PrtBoolValue"),
                                SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(arg1, "Equals"), arg2)));
                        }
                        
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                            SyntaxFactory.IdentifierName("PrtBoolValue"),
                            SyntaxFactory.BinaryExpression(CompOpToSyntaxKind[op], arg1Val, arg2Val));
                        
                    }


                    if (op == PData.Cnst_Idx.Node.Name)
                    {
                        var type = LookupType(GetArgByIndex(ft, 1));
                        var typeOp = ((Id)type.Function).Name;
                        if (typeOp == PData.Con_SeqType.Node.Name)
                        {
                            arg1 = CSharpHelper.MkCSharpCastExpression("PrtSeqValue", arg1);
                        }
                        else
                        {
                            // op == PData.Con_MapType.Node.Name
                            arg1 = CSharpHelper.MkCSharpCastExpression("PrtMapValue", arg1);
                        }
                        var lookupExpr = CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(arg1, "Lookup"), arg2);
                        if (LhsStack.Count > 0 && LhsStack.Peek())
                        {
                            return lookupExpr;
                        }

                        return CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(lookupExpr, "Clone"));
                    }
                    // op == PData.Cnst_In.Node.Name
                    return CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtBoolValue"),
                                                                         CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtMapValue", arg2), "Contains"), arg1));
                }
            }
            private int GetFieldIndex(string fieldName, FuncTerm nmdTupType)
            {
                int fieldIndex = 0;
                while (nmdTupType != null)
                {
                    var fieldInfo = (FuncTerm)GetArgByIndex(nmdTupType, 0);
                    var fieldNameInFieldInfo = (Cnst)GetArgByIndex(fieldInfo, 0);
                    if (fieldName == fieldNameInFieldInfo.GetStringValue())
                        return fieldIndex;
                    nmdTupType = GetArgByIndex(nmdTupType, 1) as FuncTerm;
                    fieldIndex++;
                }
                Debug.Assert(false);
                return 0;
            }

            private SyntaxNode FoldField(FuncTerm ft, List<SyntaxNode> children)
            {
                var expr = GetArgByIndex(ft, 0);
                var field = (Cnst)GetArgByIndex(ft, 1);
                int fieldIndex;
                if (field.CnstKind == CnstKind.Numeric)
                {
                    fieldIndex = (int)field.GetNumericValue().Numerator;
                }
                else
                {
                    fieldIndex = GetFieldIndex(field.GetStringValue(), LookupType(expr));
                }
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var arg = (ExpressionSyntax)it.Current;
                    var accessExpr = CSharpHelper.MkCSharpElementAccessExpression(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtTupleValue", arg), "fieldValues"), fieldIndex);
                    if (LhsStack.Count > 0 && LhsStack.Peek())
                    {
                        return accessExpr;
                    }

                    return CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(accessExpr, "Clone"));
                }
            }

            private SyntaxNode FoldDefault(FuncTerm ft)
            {
                var typeArg = (FuncTerm)GetArgByIndex(ft, 0);
                return CSharpHelper.MkCSharpInvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("PrtValue"),
                        SyntaxFactory.IdentifierName("PrtMkDefaultValue")),
                    pToCSharp.typeContext.PTypeToCSharpExpr(typeArg));
            }

            private SyntaxNode FoldCast(FuncTerm ft, List<SyntaxNode> children)
            {
                var typeArg = (FuncTerm)GetArgByIndex(ft, 1);
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var valueArg = it.Current;
                    return CSharpHelper.MkCSharpInvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("PrtValue"),
                            SyntaxFactory.IdentifierName("PrtCastValue")),
                        (ExpressionSyntax)valueArg,
                        pToCSharp.typeContext.PTypeToCSharpExpr(typeArg));
                }
            }

            private SyntaxNode FoldConvert(FuncTerm ft, List<SyntaxNode> children)
            {
                var typeArg = (FuncTerm)GetArgByIndex(ft, 1);
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var valueArg = it.Current;
                    return CSharpHelper.MkCSharpInvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("PrtValue"),
                            SyntaxFactory.IdentifierName("PrtConvertValue")),
                        (ExpressionSyntax)valueArg,
                        pToCSharp.typeContext.PTypeToCSharpExpr(typeArg));
                }
            }

            private SyntaxNode FoldTuple(List<SyntaxNode> children)
            {
                return CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtTupleValue"), children.ToArray());
            }

            private SyntaxNode FoldNamedTuple(FuncTerm ft, List<SyntaxNode> children)
            {
                var tupType = LookupType(ft);
                children.Insert(0, pToCSharp.typeContext.PTypeToCSharpExpr(tupType));
                return CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtNamedTupleValue"), children.ToArray());
            }

            private ExpressionSyntax MkPayload(List<SyntaxNode> args)
            {
                if (args.Count == 0)
                {
                    return pToCSharp.GetEventVar(NullEvent);
                }

                if (args.Count == 1)
                {
                    return (ExpressionSyntax)args[0];
                }

                return CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtTupleValue"), args.ToArray());
            }

            private SyntaxNode FoldGoto(FuncTerm ft, List<SyntaxNode> children)
            {
                var qualifiedStateName = (FuncTerm)GetArgByIndex(ft, 0);
                var stateName = GetNameFromQualifiedName(Owner.machineName, qualifiedStateName);
                var stateExpr = SyntaxFactory.IdentifierName(stateName);
                MachineInfo machineInfo = pToCSharp.allMachines[Owner.machineName];
                string stateEntryActionName = machineInfo.stateNameToStateInfo[stateName].entryActionName;
                FunInfo entryFunInfo = pToCSharp.allGlobalFuns.ContainsKey(stateEntryActionName)
                    ? pToCSharp.allGlobalFuns[stateEntryActionName]
                    : machineInfo.funNameToFunInfo[stateEntryActionName];
                var payloadVar = MkPayload(children);
                var traceStmt = CSharpHelper.MkCSharpTrace("<GotoLog> Machine {0}-{1} goes to {2}", 
                                                           CSharpHelper.MkCSharpDot("parent", "Name"), 
                                                           CSharpHelper.MkCSharpDot("parent", "instanceNumber"), 
                                                           CSharpHelper.MkCSharpDot(stateExpr, "name"));
                var assignStmt1 = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot("parent", "currentTrigger"), pToCSharp.GetEventVar(NullEvent));
                var assignStmt2 = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot("parent", "currentPayload"), payloadVar);
                var assignStmt3 = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot("parent", "destOfGoto"), stateExpr);
                var createRetCtxt = SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "PrtFunContGoto")));
                return SyntaxFactory.Block(traceStmt, assignStmt1, assignStmt2, assignStmt3, createRetCtxt, SyntaxFactory.ReturnStatement());
            }

            private SyntaxNode FoldRaise(FuncTerm ft, List<SyntaxNode> children)
            {
                var eventExpr = (ExpressionSyntax)children[0];
                children.RemoveAt(0);
                var payloadVar = MkPayload(children);
                var equalsExpr = CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(eventExpr, "Equals"), pToCSharp.GetEventVar(NullEvent));
                var assertStmt = CSharpHelper.MkCSharpAssert(CSharpHelper.MkCSharpNot(equalsExpr), pToCSharp.SpanToString(pToCSharp.LookupSpan(ft), "Raised event must be non-null"));
                var traceStmt = CSharpHelper.MkCSharpTrace("<RaiseLog> Machine {0}-{1} raised Event {2}", 
                                                           CSharpHelper.MkCSharpDot("parent", "Name"), 
                                                           CSharpHelper.MkCSharpDot("parent", "instanceNumber"), 
                                                           CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtEventValue", eventExpr), "evt", "name"));
                var assignStmt1 = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot("parent", "currentTrigger"), eventExpr);
                var assignStmt2 = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot("parent", "currentPayload"), payloadVar);
                var returnStmt = SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "PrtFunContRaise")));
                return SyntaxFactory.Block(assertStmt, traceStmt, assignStmt1, assignStmt2, returnStmt, SyntaxFactory.ReturnStatement());
            }

            private SyntaxNode FoldSend(List<SyntaxNode> args)
            {
                var targetExpr = CSharpHelper.MkCSharpCastExpression("PrtMachineValue", args[0]);
                ExpressionSyntax eventExpr = CSharpHelper.MkCSharpCastExpression("PrtEventValue", args[1]);
                args.RemoveRange(0, 2);
                ExpressionSyntax payloadExpr = MkPayload(args);
                StatementSyntax enqueueEventStmt = SyntaxFactory.ExpressionStatement(
                    CSharpHelper.MkCSharpInvocationExpression(
                        CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpDot(targetExpr, "mach"), "PrtEnqueueEvent"),
                        eventExpr, payloadExpr, SyntaxFactory.IdentifierName("parent"), targetExpr));
                var afterLabelId = GetFreshLabelId();
                var afterLabel = GetLabelFromLabelId(afterLabelId);
                StatementSyntax contStmt = SyntaxFactory.ExpressionStatement(
                    CSharpHelper.MkCSharpInvocationExpression(
                        CSharpHelper.MkCSharpDot("parent", "PrtFunContSend"), 
                        SyntaxFactory.ThisExpression(), CSharpHelper.MkCSharpDot("currFun", "locals"), CSharpHelper.MkCSharpNumericLiteralExpression(afterLabelId)));
                StatementSyntax afterStmt = CSharpHelper.MkCSharpEmptyLabeledStatement(afterLabel);
                return SyntaxFactory.Block(enqueueEventStmt, contStmt, SyntaxFactory.ReturnStatement(), afterStmt);
            }

            private SyntaxNode FoldAnnounce(List<SyntaxNode> args)
            {
                ExpressionSyntax eventExpr = CSharpHelper.MkCSharpCastExpression("PrtEventValue", args[0]);
                args.RemoveAt(0);
                ExpressionSyntax payloadExpr = MkPayload(args);
                var invocationArgs = new[]
                {
                    eventExpr, payloadExpr, SyntaxFactory.IdentifierName("parent")
                };
                StatementSyntax announceEventStmt = SyntaxFactory.ExpressionStatement(
                    CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("application", "Announce"), invocationArgs));
                return announceEventStmt;
            }

            private SyntaxNode FoldAssert(FuncTerm ft, List<SyntaxNode> children)
            {
                Cnst msgCnst = GetArgByIndex(ft, 1) as Cnst;
                using (var it = children.GetEnumerator())
                {
                    string errorMsg;
                    it.MoveNext();
                    if (msgCnst != null)
                    {
                        errorMsg = pToCSharp.SpanToString(pToCSharp.LookupSpan(ft), msgCnst.GetStringValue());
                    }
                    else
                    {
                        errorMsg = pToCSharp.SpanToString(pToCSharp.LookupSpan(ft), "Assert failed");
                    }
                    return CSharpHelper.MkCSharpAssert(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtBoolValue",it.Current), "bl"), errorMsg);
                }
            }

            private SyntaxNode FoldFunStmt(FuncTerm ft, List<SyntaxNode> children)
            {
                List<bool> isSwapParameter = new List<bool>();
                var exprs = GetArgByIndex(ft, 1) as FuncTerm;
                while (exprs != null)
                {
                    var qualifier = GetArgByIndex(exprs, 0) as Id;
                    isSwapParameter.Add(qualifier.Name == "SWAP");
                    exprs = GetArgByIndex(exprs, 2) as FuncTerm;
                }

                var calleeName = GetName(ft, 0);
                var calleeInfo = pToCSharp.allGlobalFuns.ContainsKey(calleeName) ? pToCSharp.allGlobalFuns[calleeName] : pToCSharp.allMachines[Owner.machineName].funNameToFunInfo[calleeName];

                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                List<ExpressionSyntax> paramList = new List<ExpressionSyntax>();
                int parameterCount = 0;
                List<StatementSyntax> processOutput = new List<StatementSyntax>();
                foreach (var child in children)
                {
                    if (parameterCount == calleeInfo.parameterNames.Count)
                    {
                        // output variable
                        processOutput.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(child, CSharpHelper.MkCSharpDot("parent", "continuation", "retVal")));
                        break;
                    }
                    var calleeArg = calleeInfo.parameterNames[parameterCount];
                    var calleeArgInfo = calleeInfo.localNameToInfo[calleeArg];
                    paramList.Add((ExpressionSyntax)child);
                    if (isSwapParameter[parameterCount])
                    {
                        processOutput.Add(
                            CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                                child,
                                CSharpHelper.MkCSharpElementAccessExpression(CSharpHelper.MkCSharpDot("parent", "continuation", "retLocals"), CSharpHelper.MkCSharpNumericLiteralExpression(calleeArgInfo.index))));
                    }
                    parameterCount++;
                }

                var beforeLabelId = GetFreshLabelId();
                var beforeLabel = GetLabelFromLabelId(beforeLabelId);
                stmtList.Add(SyntaxFactory.ExpressionStatement(
                                 CSharpHelper.MkCSharpInvocationExpression(
                                     CSharpHelper.MkCSharpDot("parent", "PrtPushFunStackFrame"), 
                                     SyntaxFactory.IdentifierName(calleeName), 
                                     CSharpHelper.MkCSharpInvocationExpression(
                                         CSharpHelper.MkCSharpDot(calleeName, "CreateLocals"), paramList.ToArray()))));
                stmtList.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(beforeLabel));
                stmtList.Add(SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(calleeName, "Execute"), SyntaxFactory.IdentifierName("application"), SyntaxFactory.IdentifierName("parent"))));
                var elseStmt = SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(
                                         CSharpHelper.MkCSharpInvocationExpression(
                                             CSharpHelper.MkCSharpDot("parent", "PrtPushFunStackFrame"), CSharpHelper.MkCSharpDot("currFun", "fun"), CSharpHelper.MkCSharpDot("currFun", "locals"), CSharpHelper.MkCSharpNumericLiteralExpression(beforeLabelId))),
                                     SyntaxFactory.ReturnStatement());
                stmtList.Add(SyntaxFactory.IfStatement(
                                 CSharpHelper.MkCSharpEq(CSharpHelper.MkCSharpDot("parent", "continuation", "reason"), SyntaxFactory.IdentifierName("PrtContinuationReason.Return")),
                                 SyntaxFactory.Block(processOutput),
                                 SyntaxFactory.ElseClause(elseStmt)));
                return SyntaxFactory.Block(stmtList);
            }

            private SyntaxNode FoldNulStmt(FuncTerm ft)
            {
                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                var op = ((Id)GetArgByIndex(ft, 0)).Name;
                if (op == PData.Cnst_Pop.Node.Name)
                {
                    stmtList.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot("parent", "currentTrigger"), pToCSharp.GetEventVar(NullEvent)));
                    stmtList.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot("parent", "currentPayload"), pToCSharp.GetEventVar(NullEvent)));
                    stmtList.Add(SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "PrtFunContPop"))));
                    stmtList.Add(SyntaxFactory.ReturnStatement());
                }
                return SyntaxFactory.Block(stmtList);
            }

            private SyntaxNode FoldPrint(FuncTerm ft, List<SyntaxNode> children)
            {
                string msg = (GetArgByIndex(ft, 0) as Cnst).GetStringValue();
                FuncTerm seg = GetArgByIndex(ft, 1) as FuncTerm;
                while (seg != null)
                {
                    int formatArg = (int)(GetArgByIndex(seg, 0) as Cnst).GetNumericValue().Numerator;
                    string str = (GetArgByIndex(seg, 1) as Cnst).GetStringValue();
                    seg = GetArgByIndex(seg, 2) as FuncTerm;
                    msg += string.Format("{{{0}}}", formatArg);
                    msg += str;
                }
                List<ExpressionSyntax> exprs = new List<ExpressionSyntax>();
                children.ForEach(x => exprs.Add((ExpressionSyntax)x));
                return CSharpHelper.MkCSharpPrint(msg, exprs);
            }

            private SyntaxNode FoldBinStmt(FuncTerm ft, List<SyntaxNode> children)
            {
                var op = ((Id)GetArgByIndex(ft, 0)).Name;
                var lhs = (FuncTerm)GetArgByIndex(ft, 1);
                var type = LookupType(lhs);
                var typeName = ((Id)type.Function).Name;
                var rhs = (FuncTerm)GetArgByIndex(ft, 3);
                var rhsTypeExpr = pToCSharp.typeContext.PTypeToCSharpExpr(LookupType(rhs));
                using (var it = children.GetEnumerator())
                {
                    ExpressionSyntax index = null;
                    it.MoveNext();
                    var src = (ExpressionSyntax)it.Current;
                    it.MoveNext();
                    var dest = (ExpressionSyntax)it.Current;
                    if (it.MoveNext())
                    {
                        index = (ExpressionSyntax)it.Current;
                    }

                    if (op == PData.Cnst_Assign.Node.Name)
                    {
                        string assignType = (GetArgByIndex(ft, 2) as Id).Name;
                        if (((Id)lhs.Function).Name == PData.Con_Field.Node.Name)
                        {
                            var field = (Cnst)GetArgByIndex(lhs, 1);
                            int fieldIndex;
                            if (field.CnstKind == CnstKind.Numeric)
                            {
                                fieldIndex = (int)field.GetNumericValue().Numerator;
                            }
                            else
                            {
                                fieldIndex = GetFieldIndex(field.GetStringValue(), LookupType(GetArgByIndex(lhs, 0)));
                            }
                            if (assignType == "NONE")
                            {
                                return SyntaxFactory.ExpressionStatement(
                                    CSharpHelper.MkCSharpInvocationExpression(
                                        CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtTupleValue", dest), "Update"),
                                        CSharpHelper.MkCSharpNumericLiteralExpression(fieldIndex),
                                        CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(src, "Clone"))));
                            }

                            if (assignType == "MOVE")
                            {
                                return SyntaxFactory.ExpressionStatement(
                                    CSharpHelper.MkCSharpInvocationExpression(
                                        CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtTupleValue", dest), "Update"),
                                        CSharpHelper.MkCSharpNumericLiteralExpression(fieldIndex),
                                        src));
                            }
                            // assignType = "SWAP" 
                            return CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                                src,
                                CSharpHelper.MkCSharpInvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression, 
                                        SyntaxFactory.IdentifierName("PrtValue"), 
                                        SyntaxFactory.IdentifierName("PrtCastValue")),
                                    CSharpHelper.MkCSharpInvocationExpression(
                                        CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtTupleValue", dest), "UpdateAndReturnOldValue"),
                                        CSharpHelper.MkCSharpNumericLiteralExpression(fieldIndex),
                                        src),
                                    rhsTypeExpr));
                        }

                        if (index == null)
                        {
                            if (assignType == "NONE")
                            {
                                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                                src = (ExpressionSyntax)TranslatePossibleNondet(src, stmtList);
                                stmtList.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(dest, CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(src, "Clone"))));
                                return SyntaxFactory.Block(stmtList);
                            }

                            if (assignType == "MOVE")
                            {
                                return CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(dest, src);
                            }
                            // assignType == "SWAP"
                            return SyntaxFactory.Block(
                                CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(SyntaxFactory.IdentifierName("swap"), dest),
                                CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(dest, src),
                                CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                                    src,
                                    CSharpHelper.MkCSharpInvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("PrtValue"),
                                            SyntaxFactory.IdentifierName("PrtCastValue")), 
                                        SyntaxFactory.IdentifierName("swap"),
                                        rhsTypeExpr)));
                        }

                        lhs = (FuncTerm)GetArgByIndex(lhs, 1);
                        type = LookupType(lhs);
                        typeName = ((Id)type.Function).Name;
                        if (typeName == PData.Con_SeqType.Node.Name)
                        {
                            if (assignType == "NONE")
                            {
                                return SyntaxFactory.ExpressionStatement(
                                    CSharpHelper.MkCSharpInvocationExpression(
                                        CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtSeqValue", dest), "Update"),
                                        index,
                                        CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(src, "Clone"))));
                            }

                            if (assignType == "MOVE")
                            {
                                return SyntaxFactory.ExpressionStatement(
                                    CSharpHelper.MkCSharpInvocationExpression(
                                        CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtSeqValue", dest), "Update"),
                                        index,
                                        src));
                            }
                            // assignType == "SWAP"
                            return CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                                src,
                                CSharpHelper.MkCSharpInvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("PrtValue"),
                                            SyntaxFactory.IdentifierName("PrtCastValue")), 
                                        CSharpHelper.MkCSharpInvocationExpression(
                                            CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtSeqValue", dest), "UpdateAndReturnOldValue"),
                                            index,
                                            src),
                                        rhsTypeExpr));
                        }
                        // type is PMapType
                        if (assignType == "NONE")
                        {
                            return SyntaxFactory.ExpressionStatement(
                                CSharpHelper.MkCSharpInvocationExpression(
                                    CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtMapValue", dest), "Update"),
                                    index,
                                    CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(src, "Clone"))));
                        }

                        if (assignType == "MOVE")
                        {
                            return SyntaxFactory.ExpressionStatement(
                                CSharpHelper.MkCSharpInvocationExpression(
                                    CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtMapValue", dest), "Update"),
                                    index,
                                    src));
                        }
                        // assignType == "SWAP"
                        return CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                            src,
                            CSharpHelper.MkCSharpInvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("PrtValue"),
                                            SyntaxFactory.IdentifierName("PrtCastValue")), 
                                        CSharpHelper.MkCSharpInvocationExpression(
                                            CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtMapValue", dest), "UpdateAndReturnOldValue"),
                                            index,
                                            src),
                                        rhsTypeExpr));
                    }

                    if (op == PData.Cnst_Remove.Node.Name)
                    {
                        if (typeName == PData.Con_SeqType.Node.Name)
                        {
                            return SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtSeqValue", dest), "Remove"), src));
                        }

                        return SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtMapValue", dest), "Remove"), src));
                    }
                    // op == PData.Cnst_Insert.Node.Name
                    return SyntaxFactory.ExpressionStatement(
                        CSharpHelper.MkCSharpInvocationExpression(
                            CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtSeqValue", dest), "Insert"),
                            CSharpHelper.MkCSharpElementAccessExpression(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtTupleValue", src), "fieldValues"), 0),
                            CSharpHelper.MkCSharpElementAccessExpression(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtTupleValue", src), "fieldValues"), 1)));
                }
            }

            private SyntaxNode FoldReturn(List<SyntaxNode> children)
            {
                AST<FuncTerm> returnType = PTypeNull;
                if (funInfo != null)
                {
                    returnType = funInfo.returnType;
                }
                using (var it = children.GetEnumerator())
                {
                    List<StatementSyntax> stmtList = new List<StatementSyntax>();
                    it.MoveNext();
                    if (returnType.Equals(PTypeNull))
                    {
                        stmtList.Add(SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "PrtFunContReturn"), CSharpHelper.MkCSharpDot("currFun", "locals"))));
                    }
                    else
                    {
                        var returnExpr = (ExpressionSyntax)TranslatePossibleNondet(it.Current, stmtList);
                        stmtList.Add(SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "PrtFunContReturnVal"), returnExpr, CSharpHelper.MkCSharpDot("currFun", "locals"))));
                    }
                    stmtList.Add(SyntaxFactory.ReturnStatement());
                    return SyntaxFactory.Block(stmtList);
                }
            }

            private SyntaxNode FoldWhile(List<SyntaxNode> children)
            {
                using (var it = children.GetEnumerator())
                {
                    List<StatementSyntax> stmtList = new List<StatementSyntax>();
                    it.MoveNext();
                    var condExpr = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtBoolValue", TranslatePossibleNondet(it.Current, stmtList)), "bl");
                    it.MoveNext();
                    var loopStart = pToCSharp.GetUnique(FunName + "_loop_start");
                    var loopEnd = pToCSharp.GetUnique(FunName + "_loop_end");
                    var body = it.Current;
                    stmtList.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(loopStart));
                    stmtList.Add(SyntaxFactory.IfStatement(SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, condExpr), CSharpHelper.MkCSharpGoto(loopEnd)));
                    stmtList.Add((StatementSyntax)body);
                    stmtList.Add(CSharpHelper.MkCSharpGoto(loopStart));
                    stmtList.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(loopEnd));
                    return SyntaxFactory.Block(stmtList);
                }
            }

            private SyntaxNode FoldIte(List<SyntaxNode> children)
            {
                using (var it = children.GetEnumerator())
                {
                    List<StatementSyntax> stmtList = new List<StatementSyntax>();
                    it.MoveNext();
                    var condExpr = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtBoolValue", TranslatePossibleNondet(it.Current, stmtList)), "bl");
                    it.MoveNext();
                    var thenStmt = it.Current;
                    it.MoveNext();
                    var elseStmt = it.Current;

                    var ifName = pToCSharp.GetUnique(FunName + "_if");
                    var elseLabel = ifName + "_else";
                    var afterLabel = ifName + "_end";
                    stmtList.Add(SyntaxFactory.IfStatement(SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, condExpr), CSharpHelper.MkCSharpGoto(elseLabel)));
                    stmtList.Add((StatementSyntax)thenStmt);
                    stmtList.Add(CSharpHelper.MkCSharpGoto(afterLabel));
                    stmtList.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(elseLabel));
                    stmtList.Add((StatementSyntax)elseStmt);
                    stmtList.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(afterLabel));
                    return SyntaxFactory.Block(stmtList);
                }
            }

            private SyntaxNode FoldSeq(List<SyntaxNode> children)
            {
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var first = it.Current;
                    it.MoveNext();
                    var second = it.Current;
                    return SyntaxFactory.Block((StatementSyntax)first, (StatementSyntax)second);
                }
            }

            private SyntaxNode TranslatePossibleNondet(SyntaxNode expr, List<StatementSyntax> stmtList)
            {
                var id = expr as IdentifierNameSyntax;
                if (id == null) return expr;
                var name = id.Identifier.ToString();
                if (name != "$" && name != "$$")
                {
                    return expr;
                }
                var afterLabelId = GetFreshLabelId();
                var afterLabel = GetLabelFromLabelId(afterLabelId);
                stmtList.Add(SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "PrtFunContNondet"), SyntaxFactory.ThisExpression(), CSharpHelper.MkCSharpDot("currFun", "locals"), CSharpHelper.MkCSharpNumericLiteralExpression(afterLabelId))));
                stmtList.Add(SyntaxFactory.ReturnStatement());
                stmtList.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(afterLabel));
                return CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtBoolValue"), CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "continuation", "ReturnAndResetNondet")));
            }
            #endregion

            public SyntaxNode MkFunStackFrameClass()
            {
                SyntaxList<MemberDeclarationSyntax> members = new SyntaxList<MemberDeclarationSyntax>();
                string frameClassName = StackFrameClassName(FunName);
                //public F1_Class_StackFrame(PrtFun fun, List<PrtValue> _locals) : base(fun, _locals) {}
                var pars = new List<SyntaxNode> { CSharpHelper.MkCSharpParameter(SyntaxFactory.Identifier("locals"), CSharpHelper.MkCSharpGenericListType(SyntaxFactory.IdentifierName("PrtValue"))),
                    CSharpHelper.MkCSharpParameter(SyntaxFactory.Identifier("retLoc"), SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))) };
                SyntaxTokenList modifiers = new SyntaxTokenList();
                modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
                members = members.Add(CSharpHelper.MkCSharpConstructor(SyntaxFactory.Identifier(frameClassName),
                                                                       modifiers,
                                                                       new List<SyntaxNode>
                                                                       {
                                                                           CSharpHelper.MkCSharpParameter(SyntaxFactory.Identifier("fun"), SyntaxFactory.IdentifierName("PrtFun")),
                                                                           CSharpHelper.MkCSharpParameter(SyntaxFactory.Identifier("_locals"), CSharpHelper.MkCSharpGenericListType(SyntaxFactory.IdentifierName("PrtValue"))) },
                                                                       CSharpHelper.MkCSharpConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                                                                                                                   CSharpHelper.MkCSharpArgumentList(SyntaxFactory.IdentifierName("fun"), SyntaxFactory.IdentifierName("_locals"))),
                                                                       new List<StatementSyntax>()));

                //public F2_Class_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base(fun, _locals, retLocation) {}
                members = members.Add(CSharpHelper.MkCSharpConstructor(SyntaxFactory.Identifier(frameClassName),
                                                                       modifiers,
                                                                       new List<SyntaxNode>
                                                                       {
                                                                           CSharpHelper.MkCSharpParameter(SyntaxFactory.Identifier("fun"), SyntaxFactory.IdentifierName("PrtFun")),
                                                                           CSharpHelper.MkCSharpParameter(SyntaxFactory.Identifier("_locals"), CSharpHelper.MkCSharpGenericListType(SyntaxFactory.IdentifierName("PrtValue"))),
                                                                           CSharpHelper.MkCSharpParameter(SyntaxFactory.Identifier("retLocation"), SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))) },
                                                                       CSharpHelper.MkCSharpConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                                                                                                                   CSharpHelper.MkCSharpArgumentList(SyntaxFactory.IdentifierName("fun"), SyntaxFactory.IdentifierName("_locals"), SyntaxFactory.IdentifierName("retLocation"))),
                                                                       new List<StatementSyntax>()));

                //public override PrtFunStackFrame Clone() {return this.Clone();}
                var body = SyntaxFactory.SingletonList<StatementSyntax>(
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.ThisExpression(),
                                SyntaxFactory.IdentifierName("Clone")))));
                var clonePars = new List<SyntaxNode>();
                members = members.Add((MemberDeclarationSyntax)CSharpHelper.MkCSharpMethodDeclaration(SyntaxFactory.IdentifierName("PrtFunStackFrame"),
                                                                                                      SyntaxFactory.Identifier("Clone"),
                                                                                                      new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword) },
                                                                                                      body,
                                                                                                      clonePars));

                //Getters/setters for locals variables of the function: parameters and locals
                foreach (var pair in funInfo.localNameToInfo)
                {
                    string varName = VarName(pair.Key);
                    //Debug:
                    //Console.WriteLine("Next local of function {0} is {1}", funName, varName);

                    int ind = pair.Value.index;
                    //Console.WriteLine("Index of the next local {0} is {1}", varName, ind);
                    //Example: public PrtValue Par1 {get {return locals[0];} {set {locals[0] = value;}}

                    modifiers = new SyntaxTokenList();
                    modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
                    var getBody = SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ReturnStatement(
                                                                     CSharpHelper.MkCSharpElementAccessExpression(
                                                                         SyntaxFactory.IdentifierName("locals"), ind)));
                    var setBody = SyntaxFactory.SingletonList((StatementSyntax)CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                                                                     CSharpHelper.MkCSharpElementAccessExpression(
                                                                         SyntaxFactory.IdentifierName("locals"), ind),
                                                                     SyntaxFactory.IdentifierName("value")));
                    AccessorDeclarationSyntax[] accessorList = { CSharpHelper.MkCSharpAccessor("get", getBody), CSharpHelper.MkCSharpAccessor("set", setBody)};
                    members = members.Add((MemberDeclarationSyntax)CSharpHelper.MkCSharpPropertyDecl("PrtValue", varName,
                                                                                                     modifiers,
                                                                                                     accessorList));
                }

                modifiers = new SyntaxTokenList();
                modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
                return CSharpHelper.MkCSharpClassDecl(frameClassName, modifiers,
                                                      SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(CSharpHelper.MkCSharpIdentifierNameType("PrtFunStackFrame")),
                                                      members);
            }

            private List<StatementSyntax> Flatten(StatementSyntax stmt)
            {
                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                BlockSyntax blockStmt = stmt as BlockSyntax;
                if (blockStmt == null)
                {
                    stmtList.Add(stmt);
                }
                else
                {
                    foreach (var x in blockStmt.Statements)
                    {
                        stmtList.AddRange(Flatten(x));
                    }
                }
                return stmtList;
            }

            public SyntaxNode MkExecuteMethod()
            {
                List<StatementSyntax> funStmts = new List<StatementSyntax>();
                //Line below is a template:
                //PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                if (Owner != null)
                {
                    funStmts.Add(
                        SyntaxFactory.LocalDeclarationStatement(
                                SyntaxFactory.VariableDeclaration(
                                        SyntaxFactory.IdentifierName(Owner.machineName))
                                    .WithVariables(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.VariableDeclarator(
                                                    SyntaxFactory.Identifier("parent"))
                                                .WithInitializer(
                                                    SyntaxFactory.EqualsValueClause(
                                                        CSharpHelper.MkCSharpCastExpression(Owner.machineName, SyntaxFactory.IdentifierName("_parent")))))))
                            .NormalizeWhitespace());
                }
                string stackFrameClassName = StackFrameClassName(FunName);
                
                funStmts.Add(
                    SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(
                                    SyntaxFactory.IdentifierName(stackFrameClassName))
                                .WithVariables(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.VariableDeclarator(
                                                SyntaxFactory.Identifier("currFun"))
                                            .WithInitializer(
                                                SyntaxFactory.EqualsValueClause(
                                                    CSharpHelper.MkCSharpCastExpression(
                                                        stackFrameClassName, 
                                                        SyntaxFactory.InvocationExpression(
                                                            SyntaxFactory.MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                SyntaxFactory.IdentifierName("parent"),
                                                                SyntaxFactory.IdentifierName("PrtPopFunStackFrame")))))))))
                        .NormalizeWhitespace());

                funStmts.Add(
                    SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(
                                    SyntaxFactory.IdentifierName("PrtValue"))
                                .WithVariables(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.VariableDeclarator(
                                            SyntaxFactory.Identifier("swap")))))
                        .NormalizeWhitespace());

                if(funInfo.body == null)
                {
                    var returnExpr = CSharpHelper.MkCSharpInvocationExpression(SyntaxFactory.IdentifierName($"Foreign_{FunName}"));
                    funStmts.Add(SyntaxFactory.ExpressionStatement(
                        CSharpHelper.MkCSharpInvocationExpression(
                            CSharpHelper.MkCSharpDot("parent", "PrtFunContReturnVal"), returnExpr, CSharpHelper.MkCSharpDot("currFun", "locals"))));
                }
                else
                {
                    // Compute the body before calculating the label prelude
                    SyntaxNode funBody = Factory.Instance.ToAST(funInfo.body).Compute<SyntaxNode>(
                        x => Unfold(x),
                        (x, ch) => Fold(x, ch.ToList()));

                    if (labelCount > 0)
                    {
                        funStmts.Add(EmitLabelPrelude());
                    }
                    funStmts.AddRange(Flatten((StatementSyntax)funBody));

                    funStmts.Add(
                        SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("parent"),
                                            SyntaxFactory.IdentifierName("PrtFunContReturn")))
                                    .WithArgumentList(
                                        SyntaxFactory.ArgumentList(
                                            SyntaxFactory.SingletonSeparatedList(
                                                SyntaxFactory.Argument(
                                                    CSharpHelper.MkCSharpDot("currFun", "locals"))))))
                            .NormalizeWhitespace());
                }

                var executeMethodDecl =
                        SyntaxFactory.MethodDeclaration(
                                SyntaxFactory.PredefinedType(
                                    SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                                SyntaxFactory.Identifier("Execute"))
                            .WithModifiers(
                                SyntaxFactory.TokenList(
                                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                    SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
                            .WithParameterList(
                                SyntaxFactory.ParameterList(
                                    SyntaxFactory.SeparatedList<ParameterSyntax>(
                                        new SyntaxNodeOrToken[]{
                                        SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier("application"))
                                            .WithType(
                                                SyntaxFactory.IdentifierName("StateImpl")),
                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                        SyntaxFactory.Parameter(
                                                Owner == null ? SyntaxFactory.Identifier("parent") : SyntaxFactory.Identifier("_parent"))
                                            .WithType(
                                                SyntaxFactory.IdentifierName("PrtMachine"))})))
                            .WithBody(
                                //Block(stmt1, stmt2, stmt3, stmt4))
                                SyntaxFactory.Block(funStmts))
                            .NormalizeWhitespace();

                return executeMethodDecl;


            }
            public SyntaxNode MkCreateLocalsMethod()
            {
                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                
                //var locals = new List<PrtValue>();
                stmtList.Add(
                    SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(
                                    SyntaxFactory.IdentifierName("var"))
                                .WithVariables(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.VariableDeclarator(
                                                SyntaxFactory.Identifier("locals"))
                                            .WithInitializer(
                                                SyntaxFactory.EqualsValueClause(
                                                    SyntaxFactory.ObjectCreationExpression(
                                                            SyntaxFactory.GenericName(
                                                                    SyntaxFactory.Identifier("List"))
                                                                .WithTypeArgumentList(
                                                                    SyntaxFactory.TypeArgumentList(
                                                                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                                                            SyntaxFactory.IdentifierName("PrtValue")))))
                                                        .WithArgumentList(
                                                            SyntaxFactory.ArgumentList()))))))
                        .NormalizeWhitespace());

                //foreach (var item in args)
                stmtList.Add(
                    SyntaxFactory.ForEachStatement(
                            SyntaxFactory.IdentifierName("var"),
                            SyntaxFactory.Identifier("item"),
                            SyntaxFactory.IdentifierName("args"),
                            SyntaxFactory.Block(
                                SyntaxFactory.SingletonList<StatementSyntax>(
                                    SyntaxFactory.ExpressionStatement(
                                        SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName("locals"),
                                                    SyntaxFactory.IdentifierName("Add")))
                                            .WithArgumentList(
                                                SyntaxFactory.ArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.InvocationExpression(
                                                                SyntaxFactory.MemberAccessExpression(
                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                    SyntaxFactory.IdentifierName("item"),
                                                                    SyntaxFactory.IdentifierName("Clone")))))))))))
                        .NormalizeWhitespace());

                foreach (var varName in funInfo.localNames)
                {
                    var varInfo = funInfo.localNameToInfo[varName];
                    var defaultValue = CSharpHelper.MkCSharpInvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("PrtValue"), SyntaxFactory.IdentifierName("PrtMkDefaultValue")),
                        pToCSharp.typeContext.PTypeToCSharpExpr(varInfo.type));
                    stmtList.Add(SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("locals", "Add"), defaultValue)));
                }

                for (int i = funInfo.parameterNames.Count + funInfo.localNames.Count; i < funInfo.maxNumLocals; i++)
                {
                    stmtList.Add(SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("locals", "Add"), SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("PrtValue"), SyntaxFactory.IdentifierName("@null")))));
                }

                //return locals;
                stmtList.Add(
                    SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("locals"))
                        .NormalizeWhitespace());

                //public override List<PrtValue> CreateLocals(params PrtValue[] args) { ... }
                var createLocalsMethodDecl =
                    SyntaxFactory.MethodDeclaration(
                            SyntaxFactory.GenericName(
                                    SyntaxFactory.Identifier("List"))
                                .WithTypeArgumentList(
                                    SyntaxFactory.TypeArgumentList(
                                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                            SyntaxFactory.IdentifierName("PrtValue")))),
                            SyntaxFactory.Identifier("CreateLocals"))
                        .WithModifiers(
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
                        .WithParameterList(
                            SyntaxFactory.ParameterList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Parameter(
                                            SyntaxFactory.Identifier("args"))
                                        .WithModifiers(
                                            SyntaxFactory.TokenList(
                                                SyntaxFactory.Token(SyntaxKind.ParamsKeyword)))
                                        .WithType(
                                            SyntaxFactory.ArrayType(
                                                    SyntaxFactory.IdentifierName("PrtValue"))
                                                .WithRankSpecifiers(
                                                    SyntaxFactory.SingletonList(
                                                        SyntaxFactory.ArrayRankSpecifier(
                                                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                                                SyntaxFactory.OmittedArraySizeExpression()))))))))
                        .WithBody(
                            SyntaxFactory.Block(stmtList))
                        .NormalizeWhitespace();

                return createLocalsMethodDecl;
            }
            public SyntaxNode MkFunToStringMethod()
            {
                var body = SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ReturnStatement(CSharpHelper.MkCSharpStringLiteralExpression(FunName)));
                var pars = new List<SyntaxNode>();
                return CSharpHelper.MkCSharpMethodDeclaration(SyntaxFactory.IdentifierName("string"), SyntaxFactory.Identifier("ToString"),
                                                              new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword) },
                                                              body,
                                                              pars);
            }
            public SyntaxNode MkCreateFunStackFrameMethod()
            {
                var body = SyntaxFactory.SingletonList<StatementSyntax>(
                    SyntaxFactory.ReturnStatement(CSharpHelper.MkCSharpObjectCreationExpression(
                                                      SyntaxFactory.IdentifierName(StackFrameClassName(FunName)),
                                                      SyntaxFactory.ThisExpression(),
                                                      SyntaxFactory.IdentifierName("locals"),
                                                      SyntaxFactory.IdentifierName("retLoc"))));
                var pars = new List<SyntaxNode> { CSharpHelper.MkCSharpParameter(SyntaxFactory.Identifier("locals"), CSharpHelper.MkCSharpGenericListType(SyntaxFactory.IdentifierName("PrtValue"))),
                    CSharpHelper.MkCSharpParameter(SyntaxFactory.Identifier("retLoc"), SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))) };
                return CSharpHelper.MkCSharpMethodDeclaration(SyntaxFactory.IdentifierName("PrtFunStackFrame"), SyntaxFactory.Identifier("CreateFunStackFrame"),
                                                              new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword) },
                                                              body,
                                                              pars);
            }
            public SyntaxNode MkFuncClass()
            {
                SyntaxList<MemberDeclarationSyntax> funMembers = new SyntaxList<MemberDeclarationSyntax>();

                //IsAnonFun property for anon functions only (getter only):
                PropertyDeclarationSyntax isAnonProperty;
                if (FunName.StartsWith("AnonFun"))
                {
                    isAnonProperty =
                        SyntaxFactory.PropertyDeclaration(
                                SyntaxFactory.PredefinedType(
                                    SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                                SyntaxFactory.Identifier("IsAnonFun"))
                            .WithModifiers(
                                SyntaxFactory.TokenList(
                                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                    SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
                            .WithAccessorList(
                                SyntaxFactory.AccessorList(
                                    SyntaxFactory.SingletonList(
                                        SyntaxFactory.AccessorDeclaration(
                                            SyntaxKind.GetAccessorDeclaration,
                                            SyntaxFactory.Block(
                                                SyntaxFactory.SingletonList<StatementSyntax>(
                                                    SyntaxFactory.ReturnStatement(
                                                        SyntaxFactory.LiteralExpression(
                                                            SyntaxKind.TrueLiteralExpression))))))))
                            .NormalizeWhitespace();
                }
                else
                {
                    isAnonProperty =
                        SyntaxFactory.PropertyDeclaration(
                                SyntaxFactory.PredefinedType(
                                    SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                                SyntaxFactory.Identifier("IsAnonFun"))
                            .WithModifiers(
                                SyntaxFactory.TokenList(
                                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                    SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
                            .WithAccessorList(
                                SyntaxFactory.AccessorList(
                                    SyntaxFactory.SingletonList(
                                        SyntaxFactory.AccessorDeclaration(
                                            SyntaxKind.GetAccessorDeclaration,
                                            SyntaxFactory.Block(
                                                SyntaxFactory.SingletonList<StatementSyntax>(
                                                    SyntaxFactory.ReturnStatement(
                                                        SyntaxFactory.LiteralExpression(
                                                            SyntaxKind.FalseLiteralExpression))))))))
                            .NormalizeWhitespace();
                }
                funMembers = funMembers.Add(isAnonProperty);
                funMembers = funMembers.Add((MemberDeclarationSyntax)MkFunStackFrameClass());
                funMembers = funMembers.Add((MemberDeclarationSyntax)MkExecuteMethod());
                funMembers = funMembers.Add((MemberDeclarationSyntax)MkCreateLocalsMethod());
                funMembers = funMembers.Add((MemberDeclarationSyntax)MkCreateFunStackFrameMethod());
                funMembers = funMembers.Add((MemberDeclarationSyntax)MkFunToStringMethod());
                var funClassDecl =
                    SyntaxFactory.ClassDeclaration(FunClassName)
                        .WithModifiers(
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                        .WithBaseList(
                            SyntaxFactory.BaseList(
                                SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                                    SyntaxFactory.SimpleBaseType(
                                        SyntaxFactory.IdentifierName("PrtFun")))))
                        .WithMembers(funMembers)
                        .NormalizeWhitespace();

                return funClassDecl;
            }

            public void AddFunClass()
            {
                //Function class declaration should be generated in two cases:
                //1. For all global static functions
                //2. For other functions: if this function name was not already encountered
                if (Owner == null || !(Owner == null) && !Owner.processedFuns.Contains(FunName))
                {
                    //Class declaration:
                    List<SyntaxNode> whereToAdd = (Owner == null) ? pToCSharp.members : Owner.machineMembers;

                    whereToAdd.Add(MkFuncClass());

                    //Variable declaration:
                    whereToAdd.Add(
                        SyntaxFactory.FieldDeclaration(
                                SyntaxFactory.VariableDeclaration(
                                        SyntaxFactory.IdentifierName(FunClassName))
                                    .WithVariables(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.VariableDeclarator(
                                                SyntaxFactory.Identifier(FunName)).WithInitializer(SyntaxFactory.EqualsValueClause(CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName(FunClassName)))))))
                            .WithModifiers(
                                SyntaxFactory.TokenList(
                                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                            .NormalizeWhitespace());

                    Owner?.processedFuns.Add(FunName);
                }
            }
        }
    }
}