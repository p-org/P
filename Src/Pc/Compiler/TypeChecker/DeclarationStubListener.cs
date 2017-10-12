using System;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker
{
    /// <summary>
    ///     The declaration stub collector walks the parse tree and associates names with parse tree fragments.
    ///     These are grouped hierarchically in Scopes, which are associated with nodes that introduce scope.
    /// </summary>
    internal class DeclarationStubListener : PParserBaseListener
    {
        private readonly ParseTreeProperty<Scope> scopes;
        private readonly ITranslationErrorHandler handler;
        private readonly ParseTreeProperty<IPDecl> nodesToDeclarations;
        private Scope currentScope;

        public DeclarationStubListener(
            Scope globalScope,
            ParseTreeProperty<Scope> scopes,
            ParseTreeProperty<IPDecl> nodesToDeclarations,
            ITranslationErrorHandler handler)
        {
            this.scopes = scopes;
            this.nodesToDeclarations = nodesToDeclarations;
            this.handler = handler;
            currentScope = globalScope;
        }

        public override void EnterProgram(PParser.ProgramContext context)
        {
            // special case - we get the top level table externally,
            // which represents the global namespace, spanning all files
            scopes.Put(context, currentScope);
        }

        public override void EnterEventDecl(PParser.EventDeclContext context)
        {
            string symbolName = context.name.GetText();
            PEvent decl = currentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
        }

        public override void EnterEventSetDecl(PParser.EventSetDeclContext context)
        {
            string symbolName = context.name.GetText();
            EventSet decl = currentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
        }

        public override void EnterInterfaceDecl(PParser.InterfaceDeclContext context)
        {
            string symbolName = context.name.GetText();
            Interface decl = currentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
        }

        public override void EnterImplMachineDecl(PParser.ImplMachineDeclContext context)
        {
            string symbolName = context.name.GetText();
            Machine decl = currentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
            PushTable(context);
        }

        public override void ExitImplMachineDecl(PParser.ImplMachineDeclContext context) { PopTable(); }

        public override void EnterStateDecl(PParser.StateDeclContext context)
        {
            string symbolName = context.name.GetText();
            State decl = currentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
        }

        public override void EnterGroup(PParser.GroupContext context)
        {
            string symbolName = context.name.GetText();
            StateGroup group = currentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, group);
            PushTable(context);
        }

        public override void ExitGroup(PParser.GroupContext context) { PopTable(); }

        public override void EnterVarDecl(PParser.VarDeclContext context)
        {
            foreach (PParser.IdenContext varName in context.idenList()._names)
            {
                Variable decl = currentScope.Put(varName.GetText(), context, varName);
                nodesToDeclarations.Put(varName, decl);
            }
        }
        
        public override void EnterSpecMachineDecl(PParser.SpecMachineDeclContext context)
        {
            string symbolName = context.name.GetText();
            Machine decl = currentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
            PushTable(context);
        }

        public override void ExitSpecMachineDecl(PParser.SpecMachineDeclContext context) { PopTable(); }

        public override void EnterPFunDecl(PParser.PFunDeclContext context)
        {
            string symbolName = context.name.GetText();
            Function decl = currentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
            PushTable(context);
        }

        public override void ExitPFunDecl(PParser.PFunDeclContext context) { PopTable(); }

        public override void EnterFunParam(PParser.FunParamContext context)
        {
            string symbolName = context.name.GetText();
            Variable decl = currentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
        }

        public override void EnterAnonEventHandler(PParser.AnonEventHandlerContext context) { PushTable(context); }

        public override void ExitAnonEventHandler(PParser.AnonEventHandlerContext context) { PopTable(); }

        public override void EnterNoParamAnonEventHandler(PParser.NoParamAnonEventHandlerContext context)
        {
            PushTable(context);
        }

        public override void ExitNoParamAnonEventHandler(PParser.NoParamAnonEventHandlerContext context) { PopTable(); }

        #region Typedef processing

        public override void EnterPTypeDef(PParser.PTypeDefContext context)
        {
            string symbolName = context.name.GetText();
            TypeDef typeDef = currentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, typeDef);
        }

        public override void EnterForeignTypeDef(PParser.ForeignTypeDefContext context)
        {
            string symbolName = context.name.GetText();
            TypeDef typeDef = currentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, typeDef);
        }

        #endregion

        #region Enum declaration processing

        public override void EnterEnumTypeDefDecl(PParser.EnumTypeDefDeclContext context)
        {
            string symbolName = context.name.GetText();
            PEnum pEnum = currentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, pEnum);
        }

        public override void EnterEnumElem(PParser.EnumElemContext context)
        {
            string symbolName = context.name.GetText();
            EnumElem elem = currentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, elem);
        }

        public override void EnterNumberedEnumElem(PParser.NumberedEnumElemContext context)
        {
            string symbolName = context.name.GetText();
            EnumElem elem = currentScope.Put(symbolName, context);
            nodesToDeclarations.Put(context, elem);
        }

        #endregion

        #region Table Management

        private void PushTable(IParseTree context)
        {
            currentScope = new Scope(handler) {Parent = currentScope};
            scopes.Put(context, currentScope);
        }

        private void PopTable() { currentScope = currentScope.Parent; }

        #endregion
    }
}
