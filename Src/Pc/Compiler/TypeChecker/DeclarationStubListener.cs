using System;
using System.Diagnostics;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    /// <summary>
    ///     The declaration collector walks the parse tree and associates names with parse tree fragments.
    ///     These are grouped hierarchically in DeclarationTables, which are associated with nodes that introduce scope.
    /// </summary>
    public class DeclarationStubListener : PParserBaseListener
    {
        private readonly ParseTreeProperty<DeclarationTable> declarationTables;
        private readonly ParseTreeProperty<IPDecl> nodesToDeclarations;
        private DeclarationTable currentTable;

        public DeclarationStubListener(DeclarationTable topLevelTable, ParseTreeProperty<DeclarationTable> declarationTables, ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            this.declarationTables = declarationTables;
            this.nodesToDeclarations = nodesToDeclarations;
            currentTable = topLevelTable;
        }

        public override void EnterProgram(PParser.ProgramContext context)
        {
            // special case - we get the top level table externally,
            // which represents the global namespace, spanning all files
            declarationTables.Put(context, currentTable);
        }

        #region Typedef processing
        public override void EnterPTypeDef(PParser.PTypeDefContext context)
        {
            string symbolName = context.name.Text;
            TypeDef typeDef = currentTable.Put(symbolName, context);
            nodesToDeclarations.Put(context, typeDef);
        }

        public override void EnterForeignTypeDef(PParser.ForeignTypeDefContext context)
        {
            throw new NotImplementedException("TODO: foreign types");
        }
        #endregion

        #region Enum declaration processing
        public override void EnterEnumTypeDefDecl(PParser.EnumTypeDefDeclContext context)
        {
            string symbolName = context.name.Text;
            PEnum pEnum = currentTable.Put(symbolName, context);
            nodesToDeclarations.Put(context, pEnum);
        }

        public override void EnterEnumElem(PParser.EnumElemContext context)
        {
            string symbolName = context.name.Text;
            EnumElem elem = currentTable.Put(symbolName, context);
            nodesToDeclarations.Put(context, elem);
        }

        public override void EnterNumberedEnumElem(PParser.NumberedEnumElemContext context)
        {
            string symbolName = context.name.Text;
            EnumElem elem = currentTable.Put(symbolName, context);
            nodesToDeclarations.Put(context, elem);
        }
        #endregion

        public override void EnterEventDecl(PParser.EventDeclContext context)
        {
            string symbolName = context.name.Text;
            PEvent decl = currentTable.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
        }

        public override void EnterEventSetDecl(PParser.EventSetDeclContext context)
        {
            string symbolName = context.name.Text;
            EventSet decl = currentTable.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
        }

        public override void EnterInterfaceDecl(PParser.InterfaceDeclContext context)
        {
            string symbolName = context.name.Text;
            Interface decl = currentTable.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
        }

        public override void EnterImplMachineDecl(PParser.ImplMachineDeclContext context)
        {
            string symbolName = context.name.Text;
            Machine decl = currentTable.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
            PushTable(context);
        }

        public override void ExitImplMachineDecl(PParser.ImplMachineDeclContext context)
        {
            PopTable();
        }

        public override void EnterStateDecl(PParser.StateDeclContext context)
        {
            string symbolName = context.name.Text;
            State decl = currentTable.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
        }

        public override void EnterGroup(PParser.GroupContext context)
        {
            string symbolName = context.name.Text;
            StateGroup group = currentTable.Put(symbolName, context);
            nodesToDeclarations.Put(context, group);
            PushTable(context);
        }

        public override void ExitGroup(PParser.GroupContext context)
        {
            PopTable();
        }

        public override void EnterVarDecl(PParser.VarDeclContext context)
        {
            foreach (PParser.IdenContext varName in context.idenList()._names)
            {
                Variable decl = currentTable.Put(varName.GetText(), context, varName);
                nodesToDeclarations.Put(varName, decl);
            }
        }

        public override void EnterImplMachineProtoDecl(PParser.ImplMachineProtoDeclContext context)
        {
            string symbolName = context.name.Text;
            MachineProto decl = currentTable.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
        }

        public override void EnterSpecMachineDecl(PParser.SpecMachineDeclContext context)
        {
            string symbolName = context.name.Text;
            Machine decl = currentTable.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
            PushTable(context);
        }

        public override void ExitSpecMachineDecl(PParser.SpecMachineDeclContext context)
        {
            PopTable();
        }

        public override void EnterFunDecl(PParser.FunDeclContext context)
        {
            string symbolName = context.name.Text;
            Function decl = currentTable.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
            PushTable(context);
        }

        public override void ExitFunDecl(PParser.FunDeclContext context)
        {
            PopTable();
        }

        public override void EnterFunParam(PParser.FunParamContext context)
        {
            string symbolName = context.name.Text;
            Variable decl = currentTable.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
        }

        public override void EnterFunProtoDecl(PParser.FunProtoDeclContext context)
        {
            string symbolName = context.name.Text;
            FunctionProto decl = currentTable.Put(symbolName, context);
            nodesToDeclarations.Put(context, decl);
        }

        public override void EnterAnonEventHandler(PParser.AnonEventHandlerContext context)
        {
            PushTable(context);
        }

        public override void ExitAnonEventHandler(PParser.AnonEventHandlerContext context)
        {
            PopTable();
        }

        public override void EnterNoParamAnonEventHandler(PParser.NoParamAnonEventHandlerContext context)
        {
            PushTable(context);
        }

        public override void ExitNoParamAnonEventHandler(PParser.NoParamAnonEventHandlerContext context)
        {
            PopTable();
        }

        #region Table Management
        private void PushTable(IParseTree context)
        {
            currentTable = new DeclarationTable {Parent = currentTable};
            declarationTables.Put(context, currentTable);
        }

        private void PopTable()
        {
            currentTable = currentTable.Parent;
        }
        #endregion
    }
}