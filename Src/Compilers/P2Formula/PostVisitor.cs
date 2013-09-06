using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PParser
{
    /// <summary>
    /// Conveinece base Visitor class. Any visitors subclassing from this must implement ALL the visit methods.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PostVisitor<T> : BasePVisitor<T>
    {
        public PostVisitor() : base() { }
        // Type Declarations
        public override T visit_pre(TypeInt s) { return default(T); }
        public override T visit_pre(TypeBool s) { return default(T); }
        public override T visit_pre(TypeMachineID s) { return default(T); }
        public override T visit_pre(TypeEventID s) { return default(T); }
        public override T visit_pre(TypeField s) { return default(T); }
        public override T visit_pre(TypeTuple s) { return default(T); }
        public override T visit_pre(TypeNamedTuple s) { return default(T); }
        public override T visit_pre(TypeAny s) { return default(T); }
        // P Declarations
        public override T visit_pre(Program s) { return default(T); }
        public override T visit_pre(EventDeclaration s) { return default(T); }
        public override T visit_pre(MachineDeclaration s) { return default(T); }
        // Machine Declarations
        public override T visit_pre(VarDeclaration s) { return default(T); }
        public override T visit_pre(FunDeclaration s) { return default(T); }
        public override T visit_pre(StateDeclaration s) { return default(T); }
        public override T visit_pre(SubmachineDeclaration s) { return default(T); }
        public override T visit_pre(ActionDeclaration s) { return default(T); }
        public override T visit_pre(MaxQueueDeclaration s) { return default(T); }
        // State Declarations
        public override T visit_pre(EntryFunction s) { return default(T); }
        public override T visit_pre(ExitFunction s) { return default(T); }
        public override T visit_pre(Defer s) { return default(T); }
        public override T visit_pre(Ignore s) { return default(T); }
        public override T visit_pre(Transition s) { return default(T); }
        public override T visit_pre(CallTransition s) { return default(T); }
        public override T visit_pre(Action s) { return default(T); }
        // DSL Statements
        public override T visit_pre(DSLITE s) { return default(T); }
        public override T visit_pre(DSLWhile s) { return default(T); }
        public override T visit_pre(DSLAssign s) { return default(T); }
        public override T visit_pre(DSLBlock s) { return default(T); }
        public override T visit_pre(DSLFFCallStmt s) { return default(T); }
        public override T visit_pre(DSLSend s) { return default(T); }
        public override T visit_pre(DSLSCall s) { return default(T); }
        public override T visit_pre(DSLRaise s) { return default(T); }
        public override T visit_pre(DSLAssert s) { return default(T); }
        public override T visit_pre(DSLReturn s) { return default(T); }
        public override T visit_pre(DSLLeave s) { return default(T); }
        public override T visit_pre(DSLSkip s) { return default(T); }
        public override T visit_pre(DSLDelete s) { return default(T); }
        // DSL Expressions
        public override T visit_pre(DSLId e) { return default(T); }
        public override T visit_pre(DSLMember e) { return default(T); }
        public override T visit_pre(DSLIndex e) { return default(T); }
        public override T visit_pre(DSLInt e) { return default(T); }
        public override T visit_pre(DSLBool e) { return default(T); }
        public override T visit_pre(DSLArg e) { return default(T); }
        public override T visit_pre(DSLFFCall e) { return default(T); }
        public override T visit_pre(DSLUnop e) { return default(T); }
        public override T visit_pre(DSLBinop e) { return default(T); }
        public override T visit_pre(DSLTuple e) { return default(T); }
        public override T visit_pre(DSLNamedTuple e) { return default(T); }
        public override T visit_pre(DSLKWArgs e) { return default(T); }
        public override T visit_pre(DSLNew e) { return default(T); }
        public override T visit_pre(DSLAttribute e) { return default(T); }

        public override void walk(INode n)
        {
            foreach (INode child in n.children)
                walk(child);

            post_result[n] = visit(n);
        }
    }
}
