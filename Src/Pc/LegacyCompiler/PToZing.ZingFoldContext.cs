using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Formula.API;
using Microsoft.Formula.API.Nodes;

namespace Microsoft.Pc
{
    partial class PToZing
    {
        private class ZingFoldContext
        {
            private PToZing pToZing;
            public string machineName;
            public string entityName;
            public FunInfo entityInfo;
            public Stack<List<AST<Node>>> sideEffectsStack;
            public List<Tuple<AST<Node>, string>> locals;
            public Stack<bool> lhsStack;
            private Dictionary<string, int> labels;

            public ZingFoldContext(PToZing comp, string machineName, string entityName, FunInfo entityInfo)
            {
                this.pToZing = comp;
                this.machineName = machineName;
                this.entityName = entityName;
                this.entityInfo = entityInfo;
                this.sideEffectsStack = new Stack<List<AST<Node>>>();
                PushSideEffectStack();
                this.locals = new List<Tuple<AST<Node>, string>>();
                this.labels = new Dictionary<string, int>();
                this.lhsStack = new Stack<bool>();
            }

            public int LabelToId(string l)
            {
                return labels[l];
            }

            public string GetFreshLabel()
            {
                var l = pToZing.GetUnique(entityName);
                labels[l] = labels.Count + 1;
                return l;
            }

            public AST<Node> EmitLabelPrelude()
            {
                var prelude = new List<AST<Node>>();
                var tmpVar = GetTmpVar(Factory.Instance.MkCnst("StackFrame"), "retTo");

                prelude.Add(PToZing.MkZingAssign(tmpVar, PToZing.MkZingCall(MkZingDot("entryCtxt", "PopReturnTo"))));
                prelude.Add(PToZing.MkZingAssign(MkZingIdentifier("locals"), MkZingDot(tmpVar, "locals")));
                prelude.Add(PToZing.MkZingIfThen(PToZing.MkZingEq(MkZingDot(tmpVar, "pc"), Factory.Instance.MkCnst(0)), MkZingGoto("start")));

                foreach (var l in labels.Keys)
                {
                    prelude.Add(PToZing.MkZingIfThen(PToZing.MkZingEq(MkZingDot(tmpVar, "pc"), Factory.Instance.MkCnst(labels[l])), MkZingGoto(l)));
                }

                prelude.Add(MkZingAssert(ZingData.Cnst_False, "Internal error"));

                return PToZing.MkZingSeq(prelude);
            }

            public void AddSideEffect(AST<Node> stmt)
            {
                this.sideEffectsStack.Peek().Add(stmt);
            }

            public void PushSideEffectStack()
            {
                this.sideEffectsStack.Push(new List<AST<Node>>());
            }

            // type must be Zing type
            public AST<Node> GetTmpVar(AST<Node> type, string baseName)
            {
                var tmpVarName = pToZing.GetUnique(baseName);
                var tmpVar = MkZingIdentifier(tmpVarName);
                this.locals.Add(new Tuple<AST<Node>, string>(type, tmpVarName));
                return tmpVar;
            }

            public AST<Node> EmitZingSideEffects(AST<Node> stmt)
            {
                Debug.Assert(sideEffectsStack.Count > 0);
                var sideEffects = sideEffectsStack.Pop();

                if (sideEffects.Count > 0)
                {
                    sideEffects.Add(stmt);
                    return PToZing.MkZingSeq(sideEffects);
                }
                else
                {
                    return stmt;
                }
            }

            public IEnumerable<AST<Node>> EmitLocals()
            {
                return locals.Select(loc => PToZing.MkZingVarDecl(loc.Item2, loc.Item1));
            }
        }
    }
}