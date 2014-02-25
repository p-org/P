using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Formula.API.ASTQueries;
using Microsoft.Formula.API.Nodes;
using Microsoft.Formula.API.Plugins;
using Microsoft.Formula.Compiler;
using Microsoft.Formula.API;

namespace PParser
{
    class BuilderError : BaseError
    {
        public BuilderError(DSLLoc l, string m) : base(l, m) { }
    }

    class BuilderException : Exception {
        public BuilderException(string msg) : base(msg) { }
    }

    class PFormulaBuilder : BasePVisitor<IEnumerable<AST<Node>>>
    {
        private string modelName;
        private string domainPath;
        private string domainName;

        private AST<Id> curMachine;
        private string curSubmachName;
        private int curMachMaxQSize;

        private AST<Id> curState;
        private string curStateName;
        private SemanticPass sem;

        private class AbortException : Exception {};

        public List<BuilderError> errors
        {
            get;
            private set;
        }

        public PFormulaBuilder(SemanticPass semP, string domName = "PData", string path = null)
            : base()
        {
            domainName = domName;
            domainPath = path;
            sem = semP;
            errors = new List<BuilderError>();
        }

        // ----------------- Entry function for consumers ----------
        public AST<Node> build(Program p, string name)
        {
            modelName = name;
            try
            {
                walk(p);
                return getPostResult(p).First();
            }
            catch (AbortException)
            {
                return null;
            }
        }
        // ------------------ Helpers ------------------------
        private void abort() { throw new AbortException(); }

        // ------------------ Naming Helpers -----------------
        // Use these couple of functions to enforce uniform naming of Machines/States across different visitors.
        private void assert(Boolean c, String msg, params object[] args)
        {
            if (!c) throw new BuilderException(String.Format(msg, args));
        }

        private IEnumerable<AST<Node>> wrap(AST<Node> n)
        {
            var l = new List<AST<Node>>();
            l.Add(n);
            return l;
        }

        private AST<Node> getOne(INode n)
        {
            var l = getPostResult(n);
            assert(l.Count() == 1, "Exptected one node, got " + l.Count() + " : " + l);
            return l.First();
        }

        private AST<Node> getOneOrNil(INode n)
        {
            if (n == null)
                return P_FormulaNodes.MkNilId(null);
            else
                return getOne(n);
        }

        private IEnumerable<AST<Node>> getAll(INode n)
        {
            return getPostResult(n);
        }

        private IEnumerable<AST<Node>> allChildTerms(INode n)
        {
            foreach (INode child in n.children) {
                var terms = getAll(child);

                if (terms == null) continue;

                foreach (AST<Node> term in terms)
                {
                    yield return term;
                }
            }
        }
        
        private AST<Node> sequence(AST<Id> seqNode, params AST<Node>[] nodes)
        {
            DSLLoc loc = null;
            if (nodes.Length > 0)
            {
                loc = nodes[nodes.Length - 1].Node.Span.ToLoc();
            }

            AST<Node> res = P_FormulaNodes.MkNilId(loc);

            foreach (AST<Node> n in nodes.Reverse<AST<Node>>())
                    res = fMkFuncTerm(seqNode, loc, n, res);

            return res;
        }

        private AST<Node> sequence(IEnumerable<AST<Node>> seq, AST<Id> seqNode)
        {
            var loc = seqNode.Node.Span.ToLoc();
            AST<Node> res = P_FormulaNodes.MkNilId(loc);
            foreach (AST<Node> n in seq.Reverse())
                    res = fMkFuncTerm(seqNode, loc, n, res);

            return res;
        }

        private AST<Node> sequenceCollapsing(IEnumerable<AST<Node>> seq, AST<Id> seqNode)
        {
            if (seq.Count() == 1)
                return seq.First();
            else
                return sequence(seq, seqNode);
        }

        public AST<Node> fMkExprs(DSLLoc loc, params AST<Node>[] nodes)
        {
            return sequence(P_FormulaNodes.MkExprsId(loc), nodes);
        }

        // These are just short hand methods for creating new Formula Nodes. Save typing.
        public AST<FuncTerm> fMkFuncTerm(AST<Id> name, DSLLoc loc, params AST<Node>[] args)
        {
            return Factory.Instance.MkFuncTerm(name, loc.ToSpan(), args);
        }

        public AST<ModelFact> fMkModelFact(AST<Node> term, DSLLoc loc)
        {
            return Factory.Instance.MkModelFact(null, term, loc.ToSpan());
        }

        public AST<ModelFact> fMkModelFact(AST<Node> term, string id, DSLLoc loc)
        {
            return Factory.Instance.MkModelFact(fMkId(id, loc), term, loc.ToSpan());
        }

        private AST<Cnst> fMkCnst(string s, DSLLoc loc)
        {
            return Factory.Instance.MkCnst(s, loc.ToSpan());
        }

        private AST<Cnst> fMkCnst(int i, DSLLoc loc)
        {
            return Factory.Instance.MkCnst(i, loc.ToSpan());
        }

        private AST<Id> fMkId(string s, DSLLoc loc)
        {
            return Factory.Instance.MkId(s, loc.ToSpan());
        }

        private AST<Id> fMkCnst(Boolean b, DSLLoc loc)
        {
            return fMkId(b ? "TRUE" : "FALSE", loc);
        }

        // ------------------ Actual Node Visitors ------------------------
        // Type Declarations
        public override IEnumerable<AST<Node>> visit(TypeInt s)
        {
            return wrap(P_FormulaNodes.MkTypeINTId(s.loc));
        }
        public override IEnumerable<AST<Node>> visit(TypeBool s)
        {
            return wrap(P_FormulaNodes.MkTypeBOOLId(s.loc));
        }
        public override IEnumerable<AST<Node>> visit(TypeMachineID s)
        {
            return wrap(P_FormulaNodes.MkTypeIDId(s.loc));
        }
        public override IEnumerable<AST<Node>> visit(TypeModelMachineID s)
        {
            return wrap(P_FormulaNodes.MkTypeMIDId(s.loc));
        }
        public override IEnumerable<AST<Node>> visit(TypeEventID s)
        {
            return wrap(P_FormulaNodes.MkTypeEVENTId(s.loc));
        }

        public override IEnumerable<AST<Node>> visit(TypeAny s)
        {
            return wrap(P_FormulaNodes.MkTypeAnyId(s.loc));
        }

        public override IEnumerable<AST<Node>> visit(TypeField s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkTypeFieldId(s.loc), s.loc, fMkCnst(s.label, s.loc), getOne(s.type)));
        }
    
        public override IEnumerable<AST<Node>> visit(TypeNamedTuple s)
        {
            return wrap(sequence(s.children.Select(c => getOne(c)), P_FormulaNodes.MkTypeNamedTupleId(s.loc)));
        }

        public override IEnumerable<AST<Node>> visit(TypeTuple s)
        {
            return wrap(sequence(s.children.Select(c => getOne(c)), P_FormulaNodes.MkTypeTupleId(s.loc)));
        }

        public override IEnumerable<AST<Node>> visit(TypeSeq s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkTypeSeqId(s.loc), s.loc, getOne(s.innerT)));
        }

        public override IEnumerable<AST<Node>> visit(TypeMap s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkTypeMapId(s.loc), s.loc, getOne(s.domain), getOne(s.range)));
        }

        // P Declarations
        public override IEnumerable<AST<Node>> visit(Program s)
        {
            var p = Factory.Instance.MkModel(modelName, false, Factory.Instance.MkModRef(domainName, null, domainPath), ComposeKind.None);

            foreach (AST<Node> term in allChildTerms(s))
            {
                p = Factory.Instance.AddFact(p, (AST<ModelFact>)term);
            }

            return wrap(p);
        }

        public override IEnumerable<AST<Node>> visit(EventDeclaration s)
        {
            AST<Node> payload = P_FormulaNodes.MkNilId(s.loc);
            AST<Node> annotation = P_FormulaNodes.MkNilId(s.loc);

            if (s.payloadType != null)
                payload = getOne(s.payloadType);

            if (s.assume != -1)
                annotation = fMkFuncTerm(P_FormulaNodes.MkAssumeMaxInstancesId(s.loc), s.loc, fMkCnst(s.assume, s.loc));
            else if (s.assert != -1)
                annotation = fMkFuncTerm(P_FormulaNodes.MkAssertMaxInstancesId(s.loc), s.loc, fMkCnst(s.assert, s.loc));


            var name = sem.resolve(s, s.id);
            return wrap(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkEventDeclId(s.loc), s.loc, fMkCnst(name, s.loc), annotation, payload), name, s.loc));
        }

        public override IEnumerable<AST<Node>> visit(MachineDeclaration s)
        {
            List<AST<Node>> mTerms = new List<AST<Node>>(allChildTerms(s));
            AST<Node> maxQSize = curMachMaxQSize == -1 ? (AST<Node>)P_FormulaNodes.MkNilId(s.loc) : (AST<Node>)fMkCnst(curMachMaxQSize, s.loc);
            var machineName = sem.resolve(s, s.id);

            var machineDecl = fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkMachineDeclId(s.loc), s.loc, fMkCnst(machineName, s.loc), fMkId(s.type, s.loc), maxQSize), machineName, s.loc);
            mTerms.Insert(0, machineDecl);
            if (s.isMain)
            {
                mTerms.Insert(0, fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkMainDeclId(s.loc),
                                                          s.loc,
                                                            fMkFuncTerm(
                                                                P_FormulaNodes.MkNewId(s.loc),
                                                                s.loc,
                                                                fMkCnst(s.id, s.loc),
                                                                P_FormulaNodes.MkNilId(s.loc))), s.loc));
            }
            if (s.isFair)
            {
                mTerms.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkFairId(s.loc), s.loc, fMkId(machineName, s.loc)), s.loc));
            }

            // Generate Machine Ignore Statement. It would be nice to do this optionally if the machine has any ignores.
            var machScope = sem.getScope(s);
            var actName = machScope.lookup(SemanticPass.VAR_IGNORE_ACTION).resolvedName();
            mTerms.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkActionDeclId(s.loc), s.loc, fMkCnst(actName, s.loc), curMachine, P_FormulaNodes.MkNilId(s.loc)), actName, s.loc));
            return mTerms;
        }

        public override IEnumerable<AST<Node>> visit(MonitorDeclaration s)
        {
            List<AST<Node>> mTerms = new List<AST<Node>>(allChildTerms(s));
            var machineName = sem.resolve(s, s.id);

            var machineDecl = fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkMachineDeclId(s.loc), s.loc, fMkCnst(machineName, s.loc), fMkId("SPEC", s.loc), P_FormulaNodes.MkNilId(s.loc)), machineName, s.loc);
            mTerms.Insert(0, machineDecl);
            // Generate Machine Ignore Statement. It would be nice to do this optionally if the machine has any ignores.
            var machScope = sem.getScope(s);
            var actName = machScope.lookup(SemanticPass.VAR_IGNORE_ACTION).resolvedName();
            mTerms.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkActionDeclId(s.loc), s.loc, fMkCnst(actName, s.loc), curMachine, P_FormulaNodes.MkNilId(s.loc)), actName, s.loc));
            return mTerms;
        }

        // Machine Declarations
        public override IEnumerable<AST<Node>> visit(VarDeclaration s)
        {
            var varName = sem.resolve(s, s.name);
            return wrap(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkVarDeclId(s.loc), s.loc, fMkCnst(varName, s.loc), curMachine, getOne(s.type), fMkCnst(s.isGhost, s.loc)), s.loc));
        }
        public override IEnumerable<AST<Node>> visit(ActionDeclaration s)
        {
            var actionName = sem.resolve(s, s.name);
            return wrap(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkActionDeclId(s.loc), s.loc, fMkCnst(actionName, s.loc), curMachine, getOne(s.body)), actionName, s.loc));
        }

        public override IEnumerable<AST<Node>> visit(FunDeclaration s)
        {
            var funName = sem.resolve(s, s.name);
            AST<Node> paramsN = P_FormulaNodes.MkNilId(s.loc);
            List<AST<Node>> res = new List<AST<Node>>();

            if (s.paramTypes != null)
            {
                foreach (INode n in s.paramTypes.children.Reverse())
                {
                    var field = (TypeField)n;
                    paramsN = fMkFuncTerm(P_FormulaNodes.MkParamsId(n.loc), n.loc, fMkCnst(field.label, n.loc), getOne(field.type), paramsN);
                }
            }

            res.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkFunDeclId(s.loc), s.loc, fMkCnst(funName, s.loc), curMachine, paramsN, getOneOrNil(s.returnType), fMkCnst(s.isModel, s.loc), getOne(s.body)), funName, s.loc));

            if (s.passiveAttr != null && s.passiveAttr.name == "passive")
            {
                res.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkFlagsId(s.loc), s.loc, fMkId(funName, s.loc), P_FormulaNodes.MkPassiveId(s.loc)), s.loc));
            }

            return res;
        }

        public override IEnumerable<AST<Node>> visit(MaxQueueDeclaration s)
        {
            curMachMaxQSize = s.size;
            return default(IEnumerable<AST<Node>>); 
        }

        public override IEnumerable<AST<Node>> visit(SubmachineDeclaration s)
        {
            List<AST<Node>> retFacts = new List<AST<Node>>();
            var name = sem.resolve(s, s.id);
            retFacts.AddRange(allChildTerms(s));
            retFacts.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkStateSetDeclId(s.loc), s.loc, fMkCnst(name, s.loc), curMachine), name, s.loc));
            retFacts.AddRange(s.children.Select(state => fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkInStateSetId(s.loc), s.loc,
                fMkId(name, s.loc), fMkId(sem.resolve(state, ((StateDeclaration)state).id), s.loc)), s.loc)));
            curSubmachName = null;
            return retFacts;
        }

        public override IEnumerable<AST<Node>> visit(StateDeclaration s)
        {
            AST<Node> entry = null;
            AST<Node> exit = null;
            List<AST<Node>> retFacts = new List<AST<Node>>();
            var stateName = sem.resolve(s, s.id);
            var deferSetName = stateName + "_Defer";
            
            // Find entry function and get its body
            foreach(INode child in s.children) {
                if (child is EntryFunction)
                    entry = getOne((child as EntryFunction).body);
            }
            entry = (entry == null) ? P_FormulaNodes.MkNilId(s.loc) : entry;

            // Find exit function and get its body
            foreach (INode child in s.children)
            {
                if (child is ExitFunction)
                    exit = getOne((child as ExitFunction).body);
            }
            exit = (exit == null) ? P_FormulaNodes.MkNilId(s.loc) : exit;
            foreach (INode child in s.children)
            {
                var loc = child.loc;
                Transition trans = child as Transition;
                if (trans == null || trans.block == null) continue;
                var transBlock = getOne(trans.block);
                AST<Node> condExpr = fMkCnst(false, loc);
                foreach (var e in trans.on)
                {
                    var useExpr = fMkFuncTerm(P_FormulaNodes.MkUseId(loc), loc, fMkCnst(e, loc), P_FormulaNodes.MkEventKindId(loc));
                    var eqExpr = fMkFuncTerm(P_FormulaNodes.MkApplyId(loc), loc, P_FormulaNodes.MkEqEqId(loc), fMkExprs(loc, P_FormulaNodes.MkTriggerId(loc), useExpr));
                    condExpr = fMkFuncTerm(P_FormulaNodes.MkApplyId(loc), loc, P_FormulaNodes.MkOrId(loc), fMkExprs(loc, eqExpr, condExpr));
                }
                exit = fMkFuncTerm(P_FormulaNodes.MkITEId(loc), loc, condExpr, transBlock, exit);
            }

            bool hasDeferred = false;
            // Find the Defer child
            foreach (INode child in s.children)
            {
                var loc = child.loc;
                if (child is Defer)
                {   // For each event in its event list add an InEventSet fact
                    foreach(string evt in (child as Defer).events) {
                        hasDeferred = true;
                        retFacts.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkInEventSetId(loc), loc, fMkId(deferSetName, loc), fMkId(evt, loc)), loc));
                    }
                }
            }
            
            // If we have a non-empty defered set, then add the StateSetDecl for it.
            if (hasDeferred)
                retFacts.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkEventSetDeclId(s.loc), s.loc, fMkCnst(deferSetName, s.loc), curMachine), deferSetName, s.loc));

            // If this is a start state, add the MachStart term.
            if (s.isStart)
                retFacts.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkMachStartId(s.loc), s.loc, curMachine, fMkId(stateName, s.loc)), s.loc));

            // If this is a stable state, add the Stable term.
            if (s.isStable)
                retFacts.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkStableId(s.loc), s.loc, fMkId(stateName, s.loc)), s.loc));

            // add the exit function
            retFacts.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkExitFunId(s.loc), s.loc, fMkId(stateName, s.loc), exit), s.loc));

            // add the StateDecl node itself
            retFacts.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkStateDeclId(s.loc), s.loc, fMkCnst(stateName, s.loc), curMachine, entry, (hasDeferred ? fMkId(deferSetName, s.loc) : P_FormulaNodes.MkNilId(s.loc))), stateName, s.loc));

            // Finally add all the node created by the children
            retFacts.AddRange(allChildTerms(s));
            return retFacts;
        }
        // State Declarations
        public override IEnumerable<AST<Node>> visit(EntryFunction s) { return default(IEnumerable<AST<Node>>); } // This one is folded in the StateDecl visitor
        public override IEnumerable<AST<Node>> visit(ExitFunction s) { return default(IEnumerable<AST<Node>>); } // This one is folded in the StateDecl visitor

        public override IEnumerable<AST<Node>> visit(Defer s) { return default(IEnumerable<AST<Node>>); }   // This is folded in StateDecl visitor
        public override IEnumerable<AST<Node>> visit(Ignore s) {
            var actName = sem.resolve(s, SemanticPass.VAR_IGNORE_ACTION);
            List<AST<Node>> retFacts = new List<AST<Node>>();

            foreach (string ev in s.events)
                retFacts.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkInstallId(s.loc), s.loc, curState, fMkId(ev, s.loc), fMkId(actName, s.loc)), s.loc));

            return retFacts;
        }

        private AST<Node> translateEvt(string ev, DSLLoc loc) {
            return ev == SemanticPass.VAR_DEFAULT ? P_FormulaNodes.MkDefaultId(loc) : fMkId(ev, loc);
        }

        public override IEnumerable<AST<Node>> visit(Transition s)
        {
            List<AST<Node>> ret = new List<AST<Node>>();
            foreach (var ev in s.on)
            {
                var ft = fMkFuncTerm(P_FormulaNodes.MkTransDeclId(s.loc), s.loc, curState, translateEvt(ev, s.loc), fMkId(sem.resolve(s, s.targetState), s.loc), fMkCnst(false, s.loc));
                ret.Add(fMkModelFact(ft, s.loc));
                if (s.isFair)
                {
                    ret.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkFairId(s.loc), s.loc, ft), s.loc));
                }
            }
            return ret;
            //return s.on.Select(ev => fMkModelFact(fMkFuncTerm(P_FormulaNodes.TransDecl_Iden, curState, translateEvt(ev), fMkId(sem.resolve(s, s.targetState)), fMkCnst(false))));
        }
        public override IEnumerable<AST<Node>> visit(CallTransition s)
        {
            return s.on.Select(ev => fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkTransDeclId(s.loc), s.loc, curState, translateEvt(ev, s.loc), fMkId(sem.resolve(s, s.targetState), s.loc), fMkCnst(true, s.loc)), s.loc));
        }
        public override IEnumerable<AST<Node>> visit(Action s) {
            return s.on.Select(ev => fMkModelFact(fMkFuncTerm(P_FormulaNodes.MkInstallId(s.loc), s.loc, curState, translateEvt(ev, s.loc), fMkId(sem.resolve(s, s.action), s.loc)), s.loc));
        }
        // DSL Statements
        public override IEnumerable<AST<Node>> visit(DSLITE s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkITEId(s.loc), s.loc, getOne(s.c), getOne(s.sTrue), getOne(s.sFalse)));
        }
        public override IEnumerable<AST<Node>> visit(DSLWhile s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkWhileId(s.loc), s.loc, getOne(s.c), getOne(s.body)));
        }
        public override IEnumerable<AST<Node>> visit(DSLAssign s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkAssignId(s.loc), s.loc, getOne(s.lhs), getOne(s.rhs)));
        }
        public override IEnumerable<AST<Node>> visit(DSLBlock s) {
            return wrap(sequenceCollapsing(s.children.Select(child => getOne(child)), P_FormulaNodes.MkSeqId(s.loc)));
        }

        public override IEnumerable<AST<Node>> visit(DSLFFCallStmt s)
        {
            return wrap(getOne(s.fcall));
        }
        public override IEnumerable<AST<Node>> visit(DSLSend s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkSendId(s.loc), s.loc, getOne(s.target), getOne(s.evt), getOneOrNil(s.payload)));
        }
        public override IEnumerable<AST<Node>> visit(DSLSCall s)
        {
            AST<Node> target;

            if (s.target is DSLId)
            {
                target = getOne(s.target);
            }
            else
            {
                var submNode = sem.lookup(s, ((s.target as DSLMember).baseExp as DSLId).id).n;
                var submScope = sem.getScope(submNode);
                var state = submScope.lookup((s.target as DSLMember).member);

                target = fMkFuncTerm(P_FormulaNodes.MkUseId(s.loc), s.loc, fMkCnst(state.resolvedName(), s.loc), P_FormulaNodes.MkStateKindId(s.loc));
            }

            return wrap(fMkFuncTerm(P_FormulaNodes.MkScallId(s.loc), s.loc, target));
        }
        public override IEnumerable<AST<Node>> visit(DSLMCall s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkMcallId(s.loc), s.loc, fMkCnst(s.monitorName, s.loc), getOne(s.evt), s.arg == null ? fMkId("NIL", s.loc) : getOne(s.arg)));
        }
        public override IEnumerable<AST<Node>> visit(DSLRaise s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkRaiseId(s.loc), s.loc, getOne(s.evt), getOneOrNil(s.payload)));
        }
        public override IEnumerable<AST<Node>> visit(DSLAssert s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkAssertId(s.loc), s.loc, getOne(s.cond)));
        }
        public override IEnumerable<AST<Node>> visit(DSLReturn s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkReturnId(s.loc), s.loc, getOneOrNil(s.rval)));
        }
        public override IEnumerable<AST<Node>> visit(DSLLeave s)
        {
            return wrap(P_FormulaNodes.MkLEAVEId(s.loc));
        }
        public override IEnumerable<AST<Node>> visit(DSLSkip s)
        {
            return wrap(P_FormulaNodes.MkNilId(s.loc));
        }
        public override IEnumerable<AST<Node>> visit(DSLDelete s)
        {
            return wrap(P_FormulaNodes.MkDELETEId(s.loc));
        }

        public override IEnumerable<AST<Node>> visit(DSLMutation s)
        {
            AST<Node> op = s.op == "insert" ? P_FormulaNodes.MkInsertId(s.loc) : s.op == "remove" ? P_FormulaNodes.MkRemoveId(s.loc) : P_FormulaNodes.MkUpdateId(s.loc);

            return wrap(fMkFuncTerm(P_FormulaNodes.MkDataOpId(s.loc), s.loc, op,
                fMkFuncTerm(P_FormulaNodes.MkExprsId(s.loc), s.loc, getOne(s.baseE), Factory.Instance.ToAST(((FuncTerm)getOne(s.args).Node).Args.ElementAt(0)))));
        }

        public override IEnumerable<AST<Node>> visit(DSLNewStmt s)
        {
            return wrap(getOne(s.newExp));
        }

        // DSL Expressions
        public override IEnumerable<AST<Node>> visit(DSLId e)
        {
            SemanticPass.BaseScope.Symbol sym = sem.lookup(e, e.id);
            AST<Node> kind = null;

            if (sym == null)
            {
                errors.Add(new BuilderError(e.loc, "Undefined symbol " + e.id));
                abort();
            }

            var name = e.id;

            if (sym.type == SemanticPass.SYM_EVENT)
            {
                kind = P_FormulaNodes.MkEventKindId(e.loc);
                if (name == SemanticPass.VAR_DEFAULT)
                {
                    name = P_FormulaNodes.MkDefaultId(e.loc).Node.Name;
                }
            }
            else if (sym.type == SemanticPass.SYM_STATE)
            {
                kind = P_FormulaNodes.MkStateKindId(e.loc);
                name = sem.resolve(e, name);
            }
            else if (sym.type == SemanticPass.SYM_VAR)
            {
                kind = P_FormulaNodes.MkVarKindId(e.loc);
                name = sem.resolve(e, name);
            }
            else if (sym.type == SemanticPass.SYM_PARAM)
                kind = P_FormulaNodes.MkVarKindId(e.loc);
            else if (sym.type == SemanticPass.SYM_BUILTIN_VAR)
            {
                // We have to handle this, arg, and nondet in a special way for backward compatibility
                kind = P_FormulaNodes.MkVarKindId(e.loc);
                if (e.id == SemanticPass.VAR_CHOICE)
                    return wrap(P_FormulaNodes.MkNondetId(e.loc));

                if (e.id == SemanticPass.VAR_THIS)
                    return wrap(P_FormulaNodes.MkThisId(e.loc));

                if (e.id == SemanticPass.VAR_TRIGGER)
                    return wrap(P_FormulaNodes.MkTriggerId(e.loc));

                if (e.id == SemanticPass.VAR_NULL)
                    return wrap(P_FormulaNodes.MkNullId(e.loc));

                name = e.id;
            }
            else if (sym.type == SemanticPass.SYM_SUBMACHINE)
            {   // This happens when we need to refer to a state in a submachine. (e.g. call(Subm.State);)
                return wrap(P_FormulaNodes.MkNilId(e.loc)); // This will be ignored.
            }
            else
            {
                errors.Add(new BuilderError(e.loc, "Unexpected symbol " + e.id + " of type " + sym.type));
                abort();
            }

            return wrap(fMkFuncTerm(P_FormulaNodes.MkUseId(e.loc), e.loc, fMkCnst(name, e.loc), kind));
        }

        public override IEnumerable<AST<Node>> visit(DSLMember e)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkApplyId(e.loc), e.loc, P_FormulaNodes.MkFldId(e.loc),
                fMkExprs(e.loc, getOne(e.baseExp), fMkFuncTerm(P_FormulaNodes.MkUseId(e.loc), e.loc, fMkCnst(e.member, e.loc), P_FormulaNodes.MkFieldKindId(e.loc)))));
        }

        private string dbgAst2Str(AST<Node> n)
        {
            System.IO.StringWriter sw = new System.IO.StringWriter();
            n.Print(sw);
            return sw.ToString();
        }

        public override IEnumerable<AST<Node>> visit(DSLIndex e)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkApplyId(e.loc), e.loc, P_FormulaNodes.MkIdxId(e.loc),
                fMkExprs(e.loc, getOne(e.baseExp), getOne(e.indexExp))));
        }
        public override IEnumerable<AST<Node>> visit(DSLArg e)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkPayloadId(e.loc), e.loc, e.type != null ? getOne(e.type) : P_FormulaNodes.MkNilId(e.loc)));
        }
        public override IEnumerable<AST<Node>> visit(DSLInt e)
        {
            return wrap(fMkCnst(e.v, e.loc));
        }
        public override IEnumerable<AST<Node>> visit(DSLBool e)
        {
            return wrap(fMkCnst(e.v, e.loc));
        }
        public override IEnumerable<AST<Node>> visit(DSLFFCall e)
        {
            var argsTuple = getOne(e.args);
            var argsExprs = ((FuncTerm)argsTuple.Node).Args.ElementAt(0);

            if (e.isExternalCall)
            {
                string[] names = e.fname.Split(new string[] { "__" }, StringSplitOptions.None);
                AST<Node> nameList = P_FormulaNodes.MkNilId(e.loc);
                for (int i = names.Length - 1; i > 0; i--)
                {
                    nameList = fMkFuncTerm(P_FormulaNodes.MkStringsId(e.loc), e.loc, fMkCnst(names[i], e.loc), nameList);
                }
                return wrap(fMkFuncTerm(P_FormulaNodes.MkEcallId(e.loc), e.loc, nameList, Factory.Instance.ToAST(argsExprs)));
            }
            else
            {
                return wrap(fMkFuncTerm(P_FormulaNodes.MkCallId(e.loc), e.loc, fMkCnst(sem.resolve(e, e.fname), e.loc), Factory.Instance.ToAST(argsExprs)));
            }
        }
        public override IEnumerable<AST<Node>> visit(DSLUnop e)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkApplyId(e.loc), e.loc, P_FormulaNodes.OperatorToId[e.op](e.loc), fMkExprs(e.loc, getOne(e.e))));
        }
        public override IEnumerable<AST<Node>> visit(DSLBinop e)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkApplyId(e.loc), e.loc, P_FormulaNodes.OperatorToId[e.op](e.loc), 
                fMkExprs(e.loc, getOne(e.e1), getOne(e.e2))));
        }
        public override IEnumerable<AST<Node>> visit(DSLTuple e)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkTupleId(e.loc), e.loc, sequence(e.children.Select(child => getOne(child)), P_FormulaNodes.MkExprsId(e.loc))));
        }

        public override IEnumerable<AST<Node>> visit(DSLNamedTuple e)
        {
            AST<Node> exprList = P_FormulaNodes.MkNilId(e.loc);

            foreach (var field in e.els.Reverse<Tuple<string, IDSLExp>>())
            {
                var loc = field.Item2.loc;
                exprList = fMkFuncTerm(P_FormulaNodes.MkNamedExprsId(loc), loc, fMkCnst(field.Item1, loc), getOne(field.Item2), exprList);
            }

            return wrap(fMkFuncTerm(P_FormulaNodes.MkNamedTupleId(e.loc), e.loc, exprList));
        }

        public override IEnumerable<AST<Node>> visit(DSLKWArgs e)
        {
            AST<Node> ntuple = P_FormulaNodes.MkNilId(e.loc);

            foreach (KeyValuePair<string, IDSLExp> kv in e.els)            
            {
                var loc = kv.Value.loc;
                ntuple = fMkFuncTerm(P_FormulaNodes.MkNamedTupleId(loc), loc, fMkCnst(kv.Key, loc), getOne(kv.Value), ntuple);
            }

            return wrap(ntuple);
        }

        public override IEnumerable<AST<Node>> visit(DSLNew e)
        {
            // In the P Ast we represent New args as a named tuple. Convert it to an Inits list for backward comp.
            // All the field labels in the Inits list have to be treated as variables names in the target machine scope, and thus be properly renamed.
            INode machineDecl = sem.lookup(e, e.machineName).n;
            var machineScope = sem.getScope(machineDecl);

            return wrap(fMkFuncTerm(P_FormulaNodes.MkNewId(e.loc), e.loc, fMkCnst(e.machineName, e.loc), 
                                    e.arg == null ? P_FormulaNodes.MkNilId(e.loc) : getOne(e.arg)));
        }

        public override IEnumerable<AST<Node>> visit(DSLAttribute e) { return default(IEnumerable<AST<Node>>); }

        public override IEnumerable<AST<Node>> visit(DSLSizeof e)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkApplyId(e.loc), e.loc, P_FormulaNodes.MkSizeofId(e.loc), sequence(P_FormulaNodes.MkExprsId(e.loc), getOne(e.of))));
        }

        public override IEnumerable<AST<Node>> visit(DSLKeys e)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.MkApplyId(e.loc), e.loc, P_FormulaNodes.MkKeysId(e.loc), sequence(P_FormulaNodes.MkExprsId(e.loc), getOne(e.of))));
        }
        
        // Preorder Visitors. Currently only used to set up Context information such as current Machine/State
        public override IEnumerable<AST<Node>> visit_pre(MachineDeclaration s)
        {
            curMachine = fMkId(sem.resolve(s, s.id), s.loc);
            curMachMaxQSize = -1;
            return default(IEnumerable<AST<Node>>);
        }

        public override IEnumerable<AST<Node>> visit_pre(MonitorDeclaration s)
        {
            curMachine = fMkId(sem.resolve(s, s.id), s.loc);
            return default(IEnumerable<AST<Node>>);
        }

        public override IEnumerable<AST<Node>> visit_pre(StateDeclaration s)
        {
            curStateName = sem.resolve(s, s.id);
            curState = fMkId(curStateName, s.loc);
            return default(IEnumerable<AST<Node>>);
        }

        public override IEnumerable<AST<Node>> visit_pre(SubmachineDeclaration s)
        {
            curSubmachName = s.id;
            return default(IEnumerable<AST<Node>>);
        }
        // Rest are dummies to shush the compiler..
        // Type Declarations
        public override IEnumerable<AST<Node>> visit_pre(TypeInt s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(TypeBool s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(TypeMachineID s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(TypeModelMachineID s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(TypeEventID s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(TypeAny s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(TypeField s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(TypeNamedTuple s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(TypeTuple s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(TypeSeq s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(TypeMap s) { return default(IEnumerable<AST<Node>>); }
        // P Declarations
        public override IEnumerable<AST<Node>> visit_pre(Program s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(EventDeclaration s) { return default(IEnumerable<AST<Node>>); }
        // Machine Declarations
        public override IEnumerable<AST<Node>> visit_pre(VarDeclaration s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(FunDeclaration s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(ActionDeclaration s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(MaxQueueDeclaration s) { return default(IEnumerable<AST<Node>>); }
        // State Declarations
        public override IEnumerable<AST<Node>> visit_pre(EntryFunction s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(ExitFunction s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(Defer s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(Ignore s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(Transition s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(CallTransition s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(Action s) { return default(IEnumerable<AST<Node>>); }
        // DSL Statements
        public override IEnumerable<AST<Node>> visit_pre(DSLITE s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLWhile s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLAssign s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLBlock s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLFFCallStmt s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLSend s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLSCall s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLMCall s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLRaise s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLAssert s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLReturn s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLLeave s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLSkip s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLDelete s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLMutation s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLNewStmt s) { return default(IEnumerable<AST<Node>>); }
        // DSL Expressions
        public override IEnumerable<AST<Node>> visit_pre(DSLId e) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLMember e) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLIndex e) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLArg e) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLInt e) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLBool e) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLFFCall e) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLUnop e) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLBinop e) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLTuple e) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLNamedTuple e) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLKWArgs e) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLNew e) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLAttribute e) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLSizeof e) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLKeys e) { return default(IEnumerable<AST<Node>>); }
    }
}
