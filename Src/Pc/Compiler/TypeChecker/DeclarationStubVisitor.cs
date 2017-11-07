using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.States;

namespace Microsoft.Pc.TypeChecker
{
    internal class DeclarationStubVisitor : PParserBaseVisitor<object>
    {
        private readonly ParseTreeProperty<IPDecl> nodesToDeclarations;
        private readonly StackProperty<Scope> scope;

        private Scope CurrentScope => scope.Value;

        private DeclarationStubVisitor(
            Scope globalScope,
            ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            this.nodesToDeclarations = nodesToDeclarations;
            scope = new StackProperty<Scope>(globalScope);
        }

        public static void PopulateStubs(
            Scope globalScope,
            PParser.ProgramContext context,
            ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            var visitor = new DeclarationStubVisitor(globalScope, nodesToDeclarations);
            visitor.Visit(context);
        }
        
        #region Typedefs

        public override object VisitPTypeDef(PParser.PTypeDefContext context)
        {
            string symbolName = context.name.GetText();
            TypeDef typeDef = CurrentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, typeDef);
            return null;
        }

        public override object VisitForeignTypeDef(PParser.ForeignTypeDefContext context)
        {
            string symbolName = context.name.GetText();
            TypeDef typeDef = CurrentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, typeDef);
            return null;
        }

        #endregion

        #region Enum typedef

        public override object VisitEnumTypeDefDecl(PParser.EnumTypeDefDeclContext context)
        {
            string symbolName = context.name.GetText();
            PEnum pEnum = CurrentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, pEnum);
            return VisitChildren(context);
        }

        public override object VisitEnumElem(PParser.EnumElemContext context)
        {
            string symbolName = context.name.GetText();
            EnumElem elem = CurrentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, elem);
            return null;
        }

        public override object VisitNumberedEnumElem(PParser.NumberedEnumElemContext context)
        {
            string symbolName = context.name.GetText();
            EnumElem elem = CurrentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, elem);
            return null;
        }

        #endregion

        #region Events

        public override object VisitEventDecl(PParser.EventDeclContext context)
        {
            string symbolName = context.name.GetText();
            PEvent decl = CurrentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
            return null;
        }

        #endregion

        #region Event sets

        public override object VisitEventSetDecl(PParser.EventSetDeclContext context)
        {
            string symbolName = context.name.GetText();
            EventSet decl = CurrentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
            return null;
        }

        #endregion

        #region Interfaces

        public override object VisitInterfaceDecl(PParser.InterfaceDeclContext context)
        {
            string symbolName = context.name.GetText();
            Interface decl = CurrentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
            return null;
        }

        #endregion

        #region Machines

        public override object VisitImplMachineDecl(PParser.ImplMachineDeclContext context)
        {
            string symbolName = context.name.GetText();
            Machine decl = CurrentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
            return VisitChildrenWithNewScope(decl, context);
        }

        public override object VisitSpecMachineDecl(PParser.SpecMachineDeclContext context)
        {
            string symbolName = context.name.GetText();
            Machine decl = CurrentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
            return VisitChildrenWithNewScope(decl, context);
        }

        public override object VisitVarDecl(PParser.VarDeclContext context)
        {
            foreach (PParser.IdenContext varName in context.idenList()._names)
            {
                Variable decl = CurrentScope.Put(varName.GetText(), context, varName);
                nodesToDeclarations.Put(varName, decl);
            }
            return null;
        }

        public override object VisitGroup(PParser.GroupContext context)
        {
            string symbolName = context.name.GetText();
            StateGroup group = CurrentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, group);
            return VisitChildrenWithNewScope(group, context);
        }

        public override object VisitStateDecl(PParser.StateDeclContext context)
        {
            string symbolName = context.name.GetText();
            State decl = CurrentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
            return null;
        }

        #endregion

        #region Functions

        public override object VisitPFunDecl(PParser.PFunDeclContext context)
        {
            string symbolName = context.name.GetText();
            Function decl = CurrentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
            return VisitChildrenWithNewScope(decl, context);
        }

        public override object VisitFunParam(PParser.FunParamContext context)
        {
            string symbolName = context.name.GetText();
            Variable decl = CurrentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
            return null;
        }

        public override object VisitFunctionBody(PParser.FunctionBodyContext context) { return null; }

        public override object VisitForeignFunDecl(PParser.ForeignFunDeclContext context)
        {
            string symbolName = context.name.GetText();
            Function decl = CurrentScope.Put(symbolName, context);
            decl.Scope = CurrentScope.MakeChildScope();
            nodesToDeclarations.Put(context, decl);
            return null;
        }

        #endregion

        private object VisitChildrenWithNewScope(IHasScope decl, IRuleNode context)
        {
            using (scope.NewContext(CurrentScope.MakeChildScope()))
            {
                decl.Scope = CurrentScope;
                return VisitChildren(context);
            }
        }

        #region Tree clipping expressions

        public override object VisitKeywordExpr(PParser.KeywordExprContext context) { return null; }

        public override object VisitSeqAccessExpr(PParser.SeqAccessExprContext context) { return null; }

        public override object VisitNamedTupleAccessExpr(PParser.NamedTupleAccessExprContext context) { return null; }

        public override object VisitPrimitiveExpr(PParser.PrimitiveExprContext context) { return null; }

        public override object VisitBinExpr(PParser.BinExprContext context) { return null; }

        public override object VisitUnaryExpr(PParser.UnaryExprContext context) { return null; }

        public override object VisitTupleAccessExpr(PParser.TupleAccessExprContext context) { return null; }

        public override object VisitUnnamedTupleExpr(PParser.UnnamedTupleExprContext context) { return null; }

        public override object VisitFunCallExpr(PParser.FunCallExprContext context) { return null; }

        public override object VisitCastExpr(PParser.CastExprContext context) { return null; }

        public override object VisitCtorExpr(PParser.CtorExprContext context) { return null; }

        public override object VisitParenExpr(PParser.ParenExprContext context) { return null; }

        public override object VisitNamedTupleExpr(PParser.NamedTupleExprContext context) { return null; }

        public override object VisitExpr(PParser.ExprContext context) { return null; }

        #endregion

        #region Tree clipping non-receive (containing) statements

        public override object VisitRemoveStmt(PParser.RemoveStmtContext context) { return null; }

        public override object VisitPrintStmt(PParser.PrintStmtContext context) { return null; }

        public override object VisitSendStmt(PParser.SendStmtContext context) { return null; }

        public override object VisitCtorStmt(PParser.CtorStmtContext context) { return null; }

        public override object VisitAssignStmt(PParser.AssignStmtContext context) { return null; }

        public override object VisitInsertStmt(PParser.InsertStmtContext context) { return null; }

        public override object VisitAnnounceStmt(PParser.AnnounceStmtContext context) { return null; }

        public override object VisitRaiseStmt(PParser.RaiseStmtContext context) { return null; }

        public override object VisitFunCallStmt(PParser.FunCallStmtContext context) { return null; }

        public override object VisitNoStmt(PParser.NoStmtContext context) { return null; }

        public override object VisitPopStmt(PParser.PopStmtContext context) { return null; }

        public override object VisitGotoStmt(PParser.GotoStmtContext context) { return null; }

        public override object VisitAssertStmt(PParser.AssertStmtContext context) { return null; }

        public override object VisitReturnStmt(PParser.ReturnStmtContext context) { return null; }

        #endregion

        #region Tree clipping types

        public override object VisitBoundedType(PParser.BoundedTypeContext context) { return null; }

        public override object VisitSeqType(PParser.SeqTypeContext context) { return null; }

        public override object VisitNamedType(PParser.NamedTypeContext context) { return null; }

        public override object VisitTupleType(PParser.TupleTypeContext context) { return null; }

        public override object VisitNamedTupleType(PParser.NamedTupleTypeContext context) { return null; }

        public override object VisitPrimitiveType(PParser.PrimitiveTypeContext context) { return null; }

        public override object VisitMapType(PParser.MapTypeContext context) { return null; }

        public override object VisitType(PParser.TypeContext context) { return null; }

        public override object VisitIdenTypeList(PParser.IdenTypeListContext context) { return null; }

        public override object VisitIdenType(PParser.IdenTypeContext context) { return null; }

        public override object VisitTypeDefDecl(PParser.TypeDefDeclContext context) { return null; }

        #endregion
    }
}
