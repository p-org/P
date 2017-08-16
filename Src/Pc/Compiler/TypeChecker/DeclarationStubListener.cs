using System.Collections.Generic;
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
        private DeclarationTable currentTable;

        public DeclarationStubListener(ParseTreeProperty<DeclarationTable> declarationTables, DeclarationTable topLevelTable)
        {
            this.declarationTables = declarationTables;
            this.currentTable = topLevelTable;
        }

        public override void EnterProgram(PParser.ProgramContext context)
        {
            // special case - we get the top level table externally,
            // which represents the global namespace, spanning all files
            declarationTables.Put(context, currentTable);
        }

        public override void EnterGroup(PParser.GroupContext context)
        {
            string symbolName = context.name.Text;
            currentTable.Put(symbolName, context);
            PushTable(context);
        }

        public override void ExitGroup(PParser.GroupContext context)
        {
            PopTable();
        }

        public override void EnterPTypeDef(PParser.PTypeDefContext context)
        {
            string symbolName = context.name.Text;
            currentTable.Put(symbolName, context);
        }

        public override void EnterEnumTypeDefDecl(PParser.EnumTypeDefDeclContext context)
        {
            string symbolName = context.name.Text;
            currentTable.Put(symbolName, context);
        }

        public override void EnterEnumElem(PParser.EnumElemContext context)
        {
            string symbolName = context.name.Text;
            currentTable.Put(symbolName, context);
        }

        public override void EnterNumberedEnumElem(PParser.NumberedEnumElemContext context)
        {
            string symbolName = context.name.Text;
            currentTable.Put(symbolName, context);
        }

        public override void EnterEventDecl(PParser.EventDeclContext context)
        {
            string symbolName = context.name.Text;
            currentTable.Put(symbolName, context);
        }

        public override void EnterEventSetDecl(PParser.EventSetDeclContext context)
        {
            string symbolName = context.name.Text;
            currentTable.Put(symbolName, context);
        }

        public override void EnterInterfaceDecl(PParser.InterfaceDeclContext context)
        {
            string symbolName = context.name.Text;
            currentTable.Put(symbolName, context);
        }

        public override void EnterImplMachineDecl(PParser.ImplMachineDeclContext context)
        {
            string symbolName = context.name.Text;
            currentTable.Put(symbolName, context);
        }

        public override void EnterImplMachineProtoDecl(PParser.ImplMachineProtoDeclContext context)
        {
            string symbolName = context.name.Text;
            currentTable.Put(symbolName, context);
        }

        public override void EnterSpecMachineDecl(PParser.SpecMachineDeclContext context)
        {
            string symbolName = context.name.Text;
            currentTable.Put(symbolName, context);
        }

        public override void EnterMachineBody(PParser.MachineBodyContext context)
        {
            PushTable(context);
        }

        public override void ExitMachineBody(PParser.MachineBodyContext context)
        {
            PopTable();
        }

        public override void EnterVarDecl(PParser.VarDeclContext context)
        {
            foreach (ITerminalNode varName in context.idenList().Iden())
            {
                currentTable.Put(varName.GetText(), context);
            }
        }

        public override void EnterFunDecl(PParser.FunDeclContext context)
        {
            string symbolName = context.name.Text;
            currentTable.Put(symbolName, context);
            PushTable(context);
        }

        public override void ExitFunDecl(PParser.FunDeclContext context)
        {
            PopTable();
        }

        public override void EnterFunProtoDecl(PParser.FunProtoDeclContext context)
        {
            string symbolName = context.name.Text;
            currentTable.Put(symbolName, context);
        }

        public override void EnterStateDecl(PParser.StateDeclContext context)
        {
            string symbolName = context.name.Text;
            currentTable.Put(symbolName, context);
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

        public override void EnterFunParam(PParser.FunParamContext context)
        {
            string symbolName = context.name.Text;
            currentTable.Put(symbolName, context);
        }

        private void PushTable(IParseTree context)
        {
            currentTable = new DeclarationTable { Parent = currentTable };
            declarationTables.Put(context, currentTable);
        }

        private void PopTable()
        {
            currentTable = currentTable.Parent;
        }
    }
}