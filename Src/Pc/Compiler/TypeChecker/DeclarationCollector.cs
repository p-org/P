using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    /// <summary>
    /// The declaration collector walks the parse tree and associates names with parse tree fragments.
    /// These are grouped hierarchically in DeclarationTables, which are associated with nodes that introduce scope.
    /// </summary>
    public class DeclarationCollector : PParserBaseListener
    {
        private readonly Stack<DeclarationTable> declarations = new Stack<DeclarationTable>();
        private readonly ParseTreeProperty<DeclarationTable> declarationTables;

        private DeclarationCollector(ParseTreeProperty<DeclarationTable> declarationTables)
        {
            this.declarationTables = declarationTables;
        }

        /// <summary>
        /// Populate an association of parse tree nodes to the scope they introduce (if any)
        /// </summary>
        /// <param name="program">The program to analyze</param>
        /// <param name="declarationTables">The mapping to populate</param>
        public static void AnnotateWithDeclarations(PParser.ProgramContext program, ParseTreeProperty<DeclarationTable> declarationTables)
        {
            var listener = new DeclarationCollector(declarationTables);
            var walker = new ParseTreeWalker();
            walker.Walk(listener, program);
        }

        public override void EnterProgram(PParser.ProgramContext context)
        {
            PushTable(context);
        }

        public override void EnterGroup(PParser.GroupContext context)
        {
            string symbolName = context.name.Text;
            Table.Put(symbolName, context);
            PushTable(context);
        }

        public override void ExitGroup(PParser.GroupContext context)
        {
            PopTable();
        }

        public override void EnterEnumTypeDefDecl(PParser.EnumTypeDefDeclContext context)
        {
            string symbolName = context.name.Text;
            Table.Put(symbolName, context);
        }

        public override void EnterEnumElem(PParser.EnumElemContext context)
        {
            string symbolName = context.name.Text;
            Table.Put(symbolName, context);
        }

        public override void EnterNumberedEnumElem(PParser.NumberedEnumElemContext context)
        {
            string symbolName = context.name.Text;
            Table.Put(symbolName, context);
        }

        public override void EnterEventDecl(PParser.EventDeclContext context)
        {
            string symbolName = context.name.Text;
            Table.Put(symbolName, context);
        }

        public override void EnterEventSetDecl(PParser.EventSetDeclContext context)
        {
            string symbolName = context.name.Text;
            Table.Put(symbolName, context);
        }

        public override void EnterInterfaceDecl(PParser.InterfaceDeclContext context)
        {
            string symbolName = context.name.Text;
            Table.Put(symbolName, context);
        }

        public override void EnterImplMachineDecl(PParser.ImplMachineDeclContext context)
        {
            string symbolName = context.name.Text;
            Table.Put(symbolName, context);
        }

        public override void EnterImplMachineProtoDecl(PParser.ImplMachineProtoDeclContext context)
        {
            string symbolName = context.name.Text;
            Table.Put(symbolName, context);
        }

        public override void EnterSpecMachineDecl(PParser.SpecMachineDeclContext context)
        {
            string symbolName = context.name.Text;
            Table.Put(symbolName, context);
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
                Table.Put(varName.GetText(), context);
            }
        }

        public override void EnterFunDecl(PParser.FunDeclContext context)
        {
            string symbolName = context.name.Text;
            Table.Put(symbolName, context);
            PushTable(context);
        }

        public override void ExitFunDecl(PParser.FunDeclContext context)
        {
            PopTable();
        }

        public override void EnterFunProtoDecl(PParser.FunProtoDeclContext context)
        {
            string symbolName = context.name.Text;
            Table.Put(symbolName, context);
        }

        public override void EnterStateDecl(PParser.StateDeclContext context)
        {
            string symbolName = context.name.Text;
            Table.Put(symbolName, context);
        }

        public override void EnterStatementBlock(PParser.StatementBlockContext context)
        {
            PushTable(context);
        }

        public override void ExitStatementBlock(PParser.StatementBlockContext context)
        {
            PopTable();
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
            Table.Put(symbolName, context);
        }

        private DeclarationTable Table => declarations.Peek();

        private void PushTable(IParseTree context)
        {
            var bodyTable = new DeclarationTable { Parent = declarations.Count > 0 ? declarations.Peek() : null };
            declarations.Push(bodyTable);
            declarationTables.Put(context, bodyTable);
        }

        private void PopTable()
        {
            declarations.Pop();
        }
    }
}