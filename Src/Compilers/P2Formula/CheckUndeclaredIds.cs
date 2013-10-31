using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PParser
{
    class CheckUndeclaredIds : NoopVisitor<int>
    {
        private SemanticPass sem;
        List<SemanticError> errs;
        public List<SemanticError> errors { get { return errs; } }

        public CheckUndeclaredIds(Program p, SemanticPass semP) : base()
        {
            sem = semP;
            errs = new List<SemanticError>();
            walk(p);
        }

        void checkDefined(INode n, string name, string type)
        {
            if (!sem.defined(n, name, type))
            {
                errs.Add(new SemanticError(n.loc, "Undefined " + type + " " + name));
            }            
        }

        public override int visit_pre(Transition n)
        {
            checkDefined(n, n.targetState, SemanticPass.SYM_STATE);

            foreach (var evt in n.on)
                checkDefined(n, evt, SemanticPass.SYM_EVENT);

            return 0;
        }

        public override int visit_pre(CallTransition n)
        {
            checkDefined(n, n.targetState, SemanticPass.SYM_STATE);

            foreach (var evt in n.on)
                checkDefined(n, evt, SemanticPass.SYM_EVENT);

            return 0;
        }

        public override int visit_pre(Action n)
        {
            checkDefined(n, n.action, SemanticPass.SYM_ACTION);

            foreach (var evt in n.on)
                checkDefined(n, evt, SemanticPass.SYM_EVENT);

            return 0;
        }

        public override int visit_pre(DSLNew n)
        {
            checkDefined(n, n.machineName, SemanticPass.SYM_MACHINE);

            return 0;
        }

        public override int visit_pre(Ignore n)
        {
            foreach (var evt in n.events)
                checkDefined(n, evt, SemanticPass.SYM_EVENT);

            return 0;
        }

        public override int visit_pre(Defer n)
        {
            foreach (var evt in n.events)
                checkDefined(n, evt, SemanticPass.SYM_EVENT);

            return 0;
        }

        public override int visit(DSLFFCall e)
        {
            if (e.isExternalCall)
                return 0;
            checkDefined(e, e.fname, SemanticPass.SYM_FUN);
            return 0;
        }

        public override int visit_pre(DSLSCall n)
        {
            if (n.target is DSLMember)
            {
                var target = n.target as DSLMember;

                if (!(target.baseExp is DSLId))
                {
                    errs.Add(new SemanticError(n.loc, string.Format("Invalid expression. expect a state name or submachine name DOT state name.")));
                    return 0;
                }

                var submName = (target.baseExp as DSLId).id;
                var stateName = target.member;

                checkDefined(n, submName, SemanticPass.SYM_SUBMACHINE);
                var sym = sem.lookup(n, submName);

                if (sym != null)
                {
                    var submScope = sem.getScope(sym.n);
                    if (!submScope.defined(stateName, SemanticPass.SYM_STATE))
                    {
                        errs.Add(new SemanticError(n.loc, string.Format("Undefined state {0}.{1}", submName, stateName)));
                        return 0;
                    }
                }
                else
                {
                    errs.Add(new SemanticError(n.loc, string.Format("Undefined submachine {0}.", submName)));
                    return 0;
                }
            }
            return 0;
        }
    }
}
