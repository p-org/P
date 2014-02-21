using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PParser
{
    class SideEffectfulExpressionsPass : NoopVisitor<int>
    {
        List<SemanticError> errs;
        public List<SemanticError> errors { get { return errs; } }

        public SideEffectfulExpressionsPass(Program p)
            : base()
        {
            errs = new List<SemanticError>();
            walk(p);
        }


        public override int visit_pre(DSLNew n)
        {
            INode p = getParent();
            if (p is DSLAssign && n == (p as DSLAssign).rhs)
            {
                // do nothing
            }
            else if (p is DSLNewStmt)
            {
                // do nothing
            }
            else
            {
                errs.Add(new SemanticError(n.loc, "new expressions can only appear on the right hand side of assignment or as statement"));
            }
            return 0;
        }

        public override int visit_pre(DSLFFCall n)
        {
            INode p = getParent();

            if (n.isExternalCall && (p is DSLAssign && n == ((DSLAssign)p).rhs))
            {
                errs.Add(new SemanticError(n.loc, "External calls cannot appear on the right hand side of an assignment"));
            }
            if (!(p is DSLFFCallStmt || (p is DSLAssign && n == ((DSLAssign)p).rhs)))
            {
                errs.Add(new SemanticError(n.loc, "Foreign function calls can only appear on the right hand side of assignment or as statements."));
            }

            return 0;
        }
    }
}
