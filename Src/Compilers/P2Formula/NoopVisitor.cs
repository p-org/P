using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PParser
{
    /// <summary>
    /// A convenience Visitor class, to subclass from. Allows us to write visitors that don't implement all the visit methods,
    /// without the compiler complaining.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class NoopVisitor<T> : BasePVisitor<T>
    {
        public NoopVisitor() : base() { }
        // Type Declarations
        public override T visit(TypeInt s) { return default(T);  }
        public override T visit(TypeBool s){ return default(T); }
        public override T visit(TypeMachineID s){ return default(T); }
        public override T visit(TypeModelMachineID s) { return default(T); }
        public override T visit(TypeEventID s){ return default(T); }
        public override T visit(TypeField s) { return default(T); }
        public override T visit(TypeNamedTuple s){ return default(T); }
        public override T visit(TypeTuple s) { return default(T); }
        public override T visit(TypeSeq s) { return default(T); }
        public override T visit(TypeMap s) { return default(T); }
        public override T visit(TypeAny s) { return default(T); }
        // P Declarations
        public override T visit(Program s){ return default(T); }
        public override T visit(EventDeclaration s){ return default(T); }
        public override T visit(MachineDeclaration s){ return default(T); }
        // Machine Declarations
        public override T visit(VarDeclaration s){ return default(T); }
        public override T visit(FunDeclaration s){ return default(T); }
        public override T visit(StateDeclaration s) { return default(T); }
        public override T visit(SubmachineDeclaration s) { return default(T); }
        public override T visit(ActionDeclaration s){ return default(T); }
        public override T visit(MaxQueueDeclaration s) { return default(T); }
        // State Declarations
        public override T visit(EntryFunction s){ return default(T); }
        public override T visit(ExitFunction s){ return default(T); }
        public override T visit(Defer s){ return default(T); }
        public override T visit(Ignore s){ return default(T); }
        public override T visit(Transition s){ return default(T); }
        public override T visit(CallTransition s){ return default(T); }
        public override T visit(Action s){ return default(T); }
        // DSL Statements
        public override T visit(DSLITE s){ return default(T); }
        public override T visit(DSLWhile s){ return default(T); }
        public override T visit(DSLAssign s){ return default(T); }
        public override T visit(DSLBlock s){ return default(T); }
        public override T visit(DSLFFCallStmt s){ return default(T); }
        public override T visit(DSLSend s){ return default(T); }
        public override T visit(DSLSCall s){ return default(T); }
        public override T visit(DSLRaise s){ return default(T); }
        public override T visit(DSLAssert s){ return default(T); }
        public override T visit(DSLReturn s){ return default(T); }
        public override T visit(DSLLeave s){ return default(T); }
        public override T visit(DSLSkip s){ return default(T); }
        public override T visit(DSLDelete s) { return default(T); }
        public override T visit(DSLMutation s) { return default(T); }
        // DSL Expressions
        public override T visit(DSLId e){ return default(T); }
        public override T visit(DSLMember e) { return default(T); }
        public override T visit(DSLIndex e) { return default(T); }
        public override T visit(DSLArg e) { return default(T); }
        public override T visit(DSLInt e){ return default(T); }
        public override T visit(DSLBool e){ return default(T); }
        public override T visit(DSLFFCall e){ return default(T); }
        public override T visit(DSLUnop e){ return default(T); }
        public override T visit(DSLBinop e){ return default(T); }
        public override T visit(DSLTuple e){ return default(T); }
        public override T visit(DSLNamedTuple e) { return default(T); }
        public override T visit(DSLKWArgs e){ return default(T); }
        public override T visit(DSLNew e){ return default(T); }
        public override T visit(DSLAttribute e) { return default(T); }
        public override T visit(DSLSizeof e) { return default(T); }
        public override T visit(DSLKeys e) { return default(T); }

        // Type Declarations
        public override T visit_pre(TypeInt s) { return default(T); }
        public override T visit_pre(TypeBool s) { return default(T); }
        public override T visit_pre(TypeMachineID s) { return default(T); }
        public override T visit_pre(TypeModelMachineID s) { return default(T); }
        public override T visit_pre(TypeEventID s) { return default(T); }
        public override T visit_pre(TypeField s) { return default(T); }
        public override T visit_pre(TypeNamedTuple s) { return default(T); }
        public override T visit_pre(TypeTuple s) { return default(T); }
        public override T visit_pre(TypeSeq s) { return default(T); }
        public override T visit_pre(TypeMap s) { return default(T); }
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
        public override T visit_pre(DSLMutation s) { return default(T); }
        // DSL Expressions
        public override T visit_pre(DSLId e) { return default(T); }
        public override T visit_pre(DSLMember e) { return default(T); }
        public override T visit_pre(DSLIndex e) { return default(T); }
        public override T visit_pre(DSLArg e) { return default(T); }
        public override T visit_pre(DSLInt e) { return default(T); }
        public override T visit_pre(DSLBool e) { return default(T); }
        public override T visit_pre(DSLFFCall e) { return default(T); }
        public override T visit_pre(DSLUnop e) { return default(T); }
        public override T visit_pre(DSLBinop e) { return default(T); }
        public override T visit_pre(DSLTuple e) { return default(T); }
        public override T visit_pre(DSLNamedTuple e) { return default(T); }
        public override T visit_pre(DSLKWArgs e) { return default(T); }
        public override T visit_pre(DSLNew e) { return default(T); }
        public override T visit_pre(DSLAttribute e) { return default(T); }
        public override T visit_pre(DSLSizeof e) { return default(T); }
        public override T visit_pre(DSLKeys e) { return default(T); }
    }
}
