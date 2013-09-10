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
            catch (AbortException e)
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
                return P_FormulaNodes.Nil_Iden;
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
            AST<Node> res = P_FormulaNodes.Nil_Iden;

            foreach (AST<Node> n in nodes.Reverse<AST<Node>>())
                    res = fMkFuncTerm(seqNode, n, res);

            return res;
        }

        private AST<Node> sequence(IEnumerable<AST<Node>> seq, AST<Id> seqNode)
        {
            AST<Node> res = P_FormulaNodes.Nil_Iden;

            foreach (AST<Node> n in seq.Reverse())
                    res = fMkFuncTerm(seqNode, n, res);

            return res;
        }

        private AST<Node> sequenceCollapsing(IEnumerable<AST<Node>> seq, AST<Id> seqNode)
        {
            if (seq.Count() == 1)
                return seq.First();
            else
                return sequence(seq, seqNode);
        }

        public AST<Node> fMkExprs(params AST<Node>[] nodes)
        {
            return sequence(P_FormulaNodes.Exprs_Iden, nodes);
        }

        // These are just short hand methods for creating new Formula Nodes. Save typing.
        public AST<FuncTerm> fMkFuncTerm(AST<Id> name, params AST<Node>[] args)
        {
            return Factory.Instance.MkFuncTerm(name, new Span(), args);
        }

        public AST<ModelFact> fMkModelFact(AST<Node> term)
        {
            return Factory.Instance.MkModelFact(null, term);
        }

        public AST<ModelFact> fMkModelFact(AST<Node> term, string id)
        {
            return Factory.Instance.MkModelFact(fMkId(id), term);
        }

        private AST<Cnst> fMkCnst(string s)
        {
            return Factory.Instance.MkCnst(s);
        }

        private AST<Cnst> fMkCnst(int i)
        {
            return Factory.Instance.MkCnst(i);
        }

        private AST<Id> fMkId(string s)
        {
            return Factory.Instance.MkId(s);
        }

        private AST<Id> fMkCnst(Boolean b)
        {
            return fMkId(b ? "TRUE" : "FALSE");
        }

        // ------------------ Actual Node Visitors ------------------------
        // Type Declarations
        public override IEnumerable<AST<Node>> visit(TypeInt s)
        {
            return wrap(P_FormulaNodes.TypeINT);
        }
        public override IEnumerable<AST<Node>> visit(TypeBool s)
        {
            return wrap(P_FormulaNodes.TypeBOOL);
        }
        public override IEnumerable<AST<Node>> visit(TypeMachineID s)
        {
            return wrap(P_FormulaNodes.TypeID);
        }
        public override IEnumerable<AST<Node>> visit(TypeEventID s)
        {
            return wrap(P_FormulaNodes.TypeEVENT);
        }

        public override IEnumerable<AST<Node>> visit(TypeAny s)
        {
            return wrap(P_FormulaNodes.TypeAny);
        }

        public override IEnumerable<AST<Node>> visit(TypeField s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.TypeField, fMkCnst(s.label), getOne(s.type)));
        }
    
        public override IEnumerable<AST<Node>> visit(TypeNamedTuple s)
        {
            return wrap(sequence(s.children.Select(c => getOne(c)), P_FormulaNodes.TypeNamedTuple));
        }

        public override IEnumerable<AST<Node>> visit(TypeTuple s)
        {
            return wrap(sequence(s.children.Select(c => getOne(c)), P_FormulaNodes.TypeTuple));
        }

        public override IEnumerable<AST<Node>> visit(TypeSeq s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.TypeSeq, getOne(s.innerT)));
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
            AST<Node> payload = P_FormulaNodes.Nil_Iden;
            AST<Node> annotation = P_FormulaNodes.Nil_Iden;

            if (s.payloadType != null)
                payload = getOne(s.payloadType);

            if (s.assume != -1)
                annotation = fMkFuncTerm(P_FormulaNodes.AssumeMaxInstances_Iden, fMkCnst(s.assume));
            else if (s.assert != -1)
                annotation = fMkFuncTerm(P_FormulaNodes.AssertMaxInstances_Iden, fMkCnst(s.assert));


            var name = sem.resolve(s, s.id);
            return wrap(fMkModelFact(fMkFuncTerm(P_FormulaNodes.EventDecl_Iden, fMkCnst(name), annotation, payload), name));
        }

        public override IEnumerable<AST<Node>> visit(MachineDeclaration s)
        {
            List<AST<Node>> mTerms = new List<AST<Node>>(allChildTerms(s));
            AST<Node> maxQSize = curMachMaxQSize == -1 ? (AST<Node>)P_FormulaNodes.Nil_Iden : (AST<Node>)fMkCnst(curMachMaxQSize);
            var machineName = sem.resolve(s, s.id);

            var machineDecl = fMkModelFact(fMkFuncTerm(P_FormulaNodes.MachineDecl_Iden, fMkCnst(machineName), fMkCnst(s.isGhost), maxQSize), machineName); // TODO: Support for max queue size
            mTerms.Insert(0, machineDecl);
            if (s.isMain)
                mTerms.Insert(0, fMkModelFact(fMkFuncTerm(P_FormulaNodes.MainDecl_Iden,
                                                            fMkFuncTerm(P_FormulaNodes.New_Iden,
                                                                fMkFuncTerm(P_FormulaNodes.MachType_Iden, fMkCnst(s.id)),
                                                                P_FormulaNodes.Nil_Iden))));

            // Generate Machine Ignore Statement. It would be nice to do this optionally if the machine has any ignores.
            var machScope = sem.getScope(s);
            var actName = machScope.lookup(SemanticPass.VAR_IGNORE_ACTION).resolvedName();
            mTerms.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.ActionDecl_Iden, fMkCnst(actName), curMachine, P_FormulaNodes.Nil_Iden), actName));
            return mTerms;
        }
        // Machine Declarations
        public override IEnumerable<AST<Node>> visit(VarDeclaration s)
        {
            var varName = sem.resolve(s, s.name);
            return wrap(fMkModelFact(fMkFuncTerm(P_FormulaNodes.VarDecl_Iden, fMkCnst(varName), curMachine, getOne(s.type), fMkCnst(s.isGhost))));
        }
        public override IEnumerable<AST<Node>> visit(ActionDeclaration s)
        {
            var actionName = sem.resolve(s, s.name);
            return wrap(fMkModelFact(fMkFuncTerm(P_FormulaNodes.ActionDecl_Iden, fMkCnst(actionName), curMachine, getOne(s.body)), actionName));
        }

        public override IEnumerable<AST<Node>> visit(FunDeclaration s)
        {
            var funName = sem.resolve(s, s.name);
            AST<Node> paramsN = P_FormulaNodes.Nil_Iden;
            List<AST<Node>> res = new List<AST<Node>>();

            if (s.paramTypes != null)
            {
                foreach (INode n in s.paramTypes.children.Reverse())
                {
                    var field = (TypeField)n;
                    paramsN = fMkFuncTerm(P_FormulaNodes.Params_Iden, fMkCnst(field.label), getOne(field.type), paramsN);
                }
            }

            res.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.FunDecl_Iden, fMkCnst(funName), curMachine, paramsN, getOneOrNil(s.returnType), fMkCnst(s.isForeign), getOne(s.body)), funName));

            if (s.passiveAttr != null && s.passiveAttr.name == "passive")
            {
                res.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.Flags_Iden, fMkId(funName), P_FormulaNodes.Passive_Iden)));
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
            retFacts.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.StateSetDecl_Iden, fMkCnst(name), curMachine), name));
            retFacts.AddRange(s.children.Select(state => fMkModelFact(fMkFuncTerm(P_FormulaNodes.InStateSet_Iden,
                fMkId(name), fMkId(sem.resolve(state, ((StateDeclaration)state).id))))));
            curSubmachName = null;
            return retFacts;
        }

        public override IEnumerable<AST<Node>> visit(StateDeclaration s)
        {
            AST<Node> entry = null;
            List<AST<Node>> retFacts = new List<AST<Node>>();
            var stateName = sem.resolve(s, s.id);
            var deferSetName = stateName + "_Defer";
            

            // Find entry function and get its body
            foreach(INode child in s.children) {
                if (child is EntryFunction)
                    entry = getOne((child as EntryFunction).body);
            }

            entry = (entry == null) ? P_FormulaNodes.Nil_Iden : entry;

            bool hasDefered = false;
            // Find the Defer child
            foreach (INode child in s.children)
            {
                if (child is Defer)
                {   // For each event in its event list add an InEventSet fact
                    foreach(string evt in (child as Defer).events) {
                        hasDefered = true;
                        retFacts.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.InEventSet_Iden, fMkId(deferSetName), fMkId(evt))));
                    }
                }

            }
            
            // If we have a non-empty defered set, then add the StateSetDecl for it.
            if (hasDefered)
                retFacts.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.EventSetDecl_Iden, fMkCnst(deferSetName), curMachine), deferSetName));

            // If this is a start state, add the MachStart term.
            if (s.isStart)
                retFacts.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.MachStart_Iden, curMachine, fMkId(stateName))));

            // add the StateDecl node itself
            retFacts.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.StateDecl_Iden, fMkCnst(stateName), curMachine, entry, (hasDefered ? fMkId(deferSetName) : P_FormulaNodes.Nil_Iden)), stateName));

            // Finally add all the node created by the children
            retFacts.AddRange(allChildTerms(s));
            return retFacts;
        }
        // State Declarations
        public override IEnumerable<AST<Node>> visit(EntryFunction s) { return default(IEnumerable<AST<Node>>); } // This one is folded in the StateDecl visitor
        public override IEnumerable<AST<Node>> visit(ExitFunction s)
        {
            return wrap(fMkModelFact(fMkFuncTerm(P_FormulaNodes.ExitFun_Iden, curState, getOne(s.body))));
        }

        public override IEnumerable<AST<Node>> visit(Defer s) { return default(IEnumerable<AST<Node>>); }   // This is folded in StateDecl visitor
        public override IEnumerable<AST<Node>> visit(Ignore s) {
            var actName = sem.resolve(s, SemanticPass.VAR_IGNORE_ACTION);
            List<AST<Node>> retFacts = new List<AST<Node>>();

            foreach (string ev in s.events)
                retFacts.Add(fMkModelFact(fMkFuncTerm(P_FormulaNodes.Install_Iden, curState, fMkId(ev), fMkId(actName))));

            return retFacts;
        }

        private AST<Node> translateEvt(string ev) {
            return ev == SemanticPass.VAR_DEFAULT ? P_FormulaNodes.Default_Iden : fMkId(ev);
        }

        public override IEnumerable<AST<Node>> visit(Transition s)
        {
            return s.on.Select(ev => fMkModelFact(fMkFuncTerm(P_FormulaNodes.TransDecl_Iden, curState, translateEvt(ev), fMkId(sem.resolve(s, s.targetState)), fMkCnst(false))));
        }
        public override IEnumerable<AST<Node>> visit(CallTransition s)
        {
            return s.on.Select(ev => fMkModelFact(fMkFuncTerm(P_FormulaNodes.TransDecl_Iden, curState, translateEvt(ev), fMkId(sem.resolve(s, s.targetState)), fMkCnst(true))));
        }
        public override IEnumerable<AST<Node>> visit(Action s) {
            return s.on.Select(ev => fMkModelFact(fMkFuncTerm(P_FormulaNodes.Install_Iden, curState, translateEvt(ev), fMkId(sem.resolve(s, s.action)))));
        }
        // DSL Statements
        public override IEnumerable<AST<Node>> visit(DSLITE s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.ITE_Iden, getOne(s.c), getOne(s.sTrue), getOne(s.sFalse)));
        }
        public override IEnumerable<AST<Node>> visit(DSLWhile s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.While_Iden, getOne(s.c), getOne(s.body)));
        }
        public override IEnumerable<AST<Node>> visit(DSLAssign s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.Assign_Iden, getOne(s.lhs), getOne(s.rhs)));
        }
        public override IEnumerable<AST<Node>> visit(DSLBlock s) {
            return wrap(sequenceCollapsing(s.children.Select(child => getOne(child)), P_FormulaNodes.Seq_Iden));
        }

        public override IEnumerable<AST<Node>> visit(DSLFFCallStmt s)
        {
            return wrap(getOne(s.fcall));
        }
        public override IEnumerable<AST<Node>> visit(DSLSend s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.Send_Iden, getOne(s.target), getOne(s.evt), getOneOrNil(s.payload)));
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

                target = fMkFuncTerm(P_FormulaNodes.Use_Iden, fMkCnst(state.resolvedName()), P_FormulaNodes.StateKind_Iden);
            }

            return wrap(fMkFuncTerm(P_FormulaNodes.Scall_Iden, target));
        }
        public override IEnumerable<AST<Node>> visit(DSLRaise s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.Raise_Iden, getOne(s.evt), getOneOrNil(s.payload)));
        }
        public override IEnumerable<AST<Node>> visit(DSLAssert s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.Assert_Iden, getOne(s.cond)));
        }
        public override IEnumerable<AST<Node>> visit(DSLReturn s)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.Return_Iden, getOneOrNil(s.rval)));
        }
        public override IEnumerable<AST<Node>> visit(DSLLeave s)
        {
            return wrap(P_FormulaNodes.LEAVE_Iden);
        }
        public override IEnumerable<AST<Node>> visit(DSLSkip s)
        {
            return wrap(P_FormulaNodes.Nil_Iden);
        }
        public override IEnumerable<AST<Node>> visit(DSLDelete s)
        {
            return wrap(P_FormulaNodes.DELETE_Iden);
        }

        public override IEnumerable<AST<Node>> visit(DSLMutation s)
        {
            AST<Node> op = s.op == "insert" ? P_FormulaNodes.Insert_Iden : P_FormulaNodes.Remove_Iden;

            return wrap(fMkFuncTerm(P_FormulaNodes.DataOp_Iden, op,
                fMkFuncTerm(P_FormulaNodes.Exprs_Iden, getOne(s.baseE), Factory.Instance.ToAST(((FuncTerm)getOne(s.args).Node).Args.ElementAt(0)))));
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
                kind = P_FormulaNodes.EventKind_Iden;
                if (name == SemanticPass.VAR_DEFAULT)
                {
                    name = P_FormulaNodes.Default_Iden.Node.Name;
                }
            }
            else if (sym.type == SemanticPass.SYM_STATE)
            {
                kind = P_FormulaNodes.StateKind_Iden;
                name = sem.resolve(e, name);
            }
            else if (sym.type == SemanticPass.SYM_VAR)
            {
                kind = P_FormulaNodes.VarKind_Iden;
                name = sem.resolve(e, name);
            }
            else if (sym.type == SemanticPass.SYM_PARAM)
                kind = P_FormulaNodes.VarKind_Iden;
            else if (sym.type == SemanticPass.SYM_BUILTIN_VAR)
            {
                // We have to handle this, arg, and nondet in a special way for backward compatibility
                kind = P_FormulaNodes.VarKind_Iden;
                if (e.id == SemanticPass.VAR_CHOICE)
                    return wrap(P_FormulaNodes.Nondet_Iden);

                if (e.id == SemanticPass.VAR_THIS)
                    return wrap(P_FormulaNodes.This_Iden);

                if (e.id == SemanticPass.VAR_TRIGGER)
                    return wrap(P_FormulaNodes.Trigger_Iden);

                if (e.id == SemanticPass.VAR_NULL)
                    return wrap(P_FormulaNodes.Null_Iden);

                name = e.id;
            }
            else if (sym.type == SemanticPass.SYM_SUBMACHINE)
            {   // This happens when we need to refer to a state in a submachine. (e.g. call(Subm.State);)
                return wrap(P_FormulaNodes.Nil_Iden); // This will be ignored.
            }
            else
            {
                errors.Add(new BuilderError(e.loc, "Unexpected symbol " + e.id + " of type " + sym.type));
                abort();
            }

            return wrap(fMkFuncTerm(P_FormulaNodes.Use_Iden, fMkCnst(name), kind));
        }



        public override IEnumerable<AST<Node>> visit(DSLMember e)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.Apply_Iden, P_FormulaNodes.Fld_Iden,
                fMkExprs(getOne(e.baseExp), fMkFuncTerm(P_FormulaNodes.Use_Iden, fMkCnst(e.member), P_FormulaNodes.FieldKind_Iden))));
        }

        private string dbgAst2Str(AST<Node> n)
        {
            System.IO.StringWriter sw = new System.IO.StringWriter();
            n.Print(sw);
            return sw.ToString();
        }

        public override IEnumerable<AST<Node>> visit(DSLIndex e)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.Apply_Iden, P_FormulaNodes.Idx_Iden,
                fMkExprs(getOne(e.baseExp), getOne(e.indexExp))));
        }
        public override IEnumerable<AST<Node>> visit(DSLArg e)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.Payload_Iden, e.type != null ? getOne(e.type) : P_FormulaNodes.Nil_Iden ));
        }
        public override IEnumerable<AST<Node>> visit(DSLInt e)
        {
            return wrap(fMkCnst(e.v));
        }
        public override IEnumerable<AST<Node>> visit(DSLBool e)
        {
            return wrap(fMkCnst(e.v));
        }
        public override IEnumerable<AST<Node>> visit(DSLFFCall e)
        {
            var argsTuple = getOne(e.args);
            var argsExprs = ((FuncTerm)argsTuple.Node).Args.ElementAt(0);

            return wrap(fMkFuncTerm(P_FormulaNodes.Call_Iden, fMkCnst(sem.resolve(e, e.fname)), Factory.Instance.ToAST(argsExprs)));
        }
        public override IEnumerable<AST<Node>> visit(DSLUnop e)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.Apply_Iden, P_FormulaNodes.OperatorToId[e.op], fMkExprs(getOne(e.e))));
        }
        public override IEnumerable<AST<Node>> visit(DSLBinop e)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.Apply_Iden, P_FormulaNodes.OperatorToId[e.op], 
                fMkExprs(getOne(e.e1), getOne(e.e2))));
        }
        public override IEnumerable<AST<Node>> visit(DSLTuple e)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.Tuple_Iden, sequence(e.children.Select(child => getOne(child)), P_FormulaNodes.Exprs_Iden)));
        }

        public override IEnumerable<AST<Node>> visit(DSLNamedTuple e)
        {
            AST<Node> exprList = P_FormulaNodes.Nil_Iden;

            foreach (var field in e.els.Reverse<Tuple<string, IDSLExp>>())
            {
                exprList = fMkFuncTerm(P_FormulaNodes.NamedExprs_Iden, fMkCnst(field.Item1), getOne(field.Item2), exprList);
            }

            return wrap(fMkFuncTerm(P_FormulaNodes.NamedTuple_Iden, exprList));
        }

        public override IEnumerable<AST<Node>> visit(DSLKWArgs e)
        {
            AST<Node> ntuple = P_FormulaNodes.Nil_Iden;

            foreach (KeyValuePair<string, IDSLExp> kv in e.els)
                ntuple = fMkFuncTerm(P_FormulaNodes.NamedTuple_Iden, fMkCnst(kv.Key), getOne(kv.Value), ntuple);

            return wrap(ntuple);
        }

        public override IEnumerable<AST<Node>> visit(DSLNew e)
        {
            // In the P Ast we represent New args as a named tuple. Convert it to an Inits list for backward comp.
            // All the field labels in the Inits list have to be treated as variables names in the target machine scope, and thus be properly renamed.
            AST<Node> inits = P_FormulaNodes.Nil_Iden;
            INode machineDecl = sem.lookup(e, e.machineName).n;
            var machineScope = sem.getScope(machineDecl);

            foreach (KeyValuePair<string, IDSLExp> c in e.args.els) {
                inits = fMkFuncTerm(P_FormulaNodes.NamedExprs_Iden, fMkCnst(machineScope.lookup(c.Key).resolvedName()), getOne(c.Value), inits);
            }

            return wrap(fMkFuncTerm(P_FormulaNodes.New_Iden, fMkFuncTerm(P_FormulaNodes.MachType_Iden, fMkCnst(e.machineName)), inits));
        }

        public override IEnumerable<AST<Node>> visit(DSLAttribute e) { return default(IEnumerable<AST<Node>>); }

        public override IEnumerable<AST<Node>> visit(DSLSizeof e)
        {
            return wrap(fMkFuncTerm(P_FormulaNodes.Apply_Iden, P_FormulaNodes.Sizeof_Iden, sequence(P_FormulaNodes.Exprs_Iden, getOne(e.of))));
        }

        // Preorder Visitors. Currently only used to set up Context information such as current Machine/State
        public override IEnumerable<AST<Node>> visit_pre(MachineDeclaration s)
        {
            curMachine = fMkId(sem.resolve(s, s.id));
            curMachMaxQSize = -1;
            return default(IEnumerable<AST<Node>>);
        }

        public override IEnumerable<AST<Node>> visit_pre(StateDeclaration s)
        {
            curStateName = sem.resolve(s, s.id);
            curState = fMkId(curStateName);
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
        public override IEnumerable<AST<Node>> visit_pre(TypeEventID s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(TypeAny s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(TypeField s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(TypeNamedTuple s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(TypeTuple s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(TypeSeq s) { return default(IEnumerable<AST<Node>>); }
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
        public override IEnumerable<AST<Node>> visit_pre(DSLRaise s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLAssert s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLReturn s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLLeave s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLSkip s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLDelete s) { return default(IEnumerable<AST<Node>>); }
        public override IEnumerable<AST<Node>> visit_pre(DSLMutation s) { return default(IEnumerable<AST<Node>>); }
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
    }
}
