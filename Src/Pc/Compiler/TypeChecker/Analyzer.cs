using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    public class DeclarationCollector : PParserBaseListener
    {
        private readonly Stack<DeclarationTable> declarations = new Stack<DeclarationTable>();
        private readonly ParseTreeProperty<DeclarationTable> declarationTables;

        private DeclarationCollector(ParseTreeProperty<DeclarationTable> declarationTables)
        {
            this.declarationTables = declarationTables;
        }

        public static ParseTreeProperty<DeclarationTable> AnnotateWithDeclarations(PParser.ProgramContext program)
        {
            var declarationTables = new ParseTreeProperty<DeclarationTable>();
            var listener = new DeclarationCollector(declarationTables);
            var walker = new ParseTreeWalker();
            walker.Walk(listener, program);
            return declarationTables;
        }

        public override void EnterProgram(PParser.ProgramContext context)
        {
            PushTable(context);
        }

        public override void EnterEnumTypeDefDecl(PParser.EnumTypeDefDeclContext context)
        {
            string symbolName = context.name.Text;
            RegisterDeclaration(symbolName, context);
        }

        private void RegisterDeclaration<T>(string symbolName, T context) where T : ParserRuleContext
        {
            DeclarationTable table = declarations.Peek();
            if (table.GetAny(symbolName, out var conflict))
            {
                throw new DuplicateDeclarationException(conflict, context);
            }

            table.Put(symbolName, context);
        }

        public override void EnterEventDecl(PParser.EventDeclContext context)
        {
            string symbolName = context.name.Text;
            RegisterDeclaration(symbolName, context);
        }

        public override void EnterEventSetDecl(PParser.EventSetDeclContext context)
        {
            string symbolName = context.name.Text;
            RegisterDeclaration(symbolName, context);
        }

        public override void EnterInterfaceDecl(PParser.InterfaceDeclContext context)
        {
            string symbolName = context.name.Text;
            RegisterDeclaration(symbolName, context);
        }

        public override void EnterImplMachineDecl(PParser.ImplMachineDeclContext context)
        {
            string symbolName = context.name.Text;
            RegisterDeclaration(symbolName, context);
        }

        public override void EnterImplMachineProtoDecl(PParser.ImplMachineProtoDeclContext context)
        {
            string symbolName = context.name.Text;
            RegisterDeclaration(symbolName, context);
        }

        public override void EnterSpecMachineDecl(PParser.SpecMachineDeclContext context)
        {
            string symbolName = context.name.Text;
            RegisterDeclaration(symbolName, context);
        }

        public override void EnterMachineBody(PParser.MachineBodyContext context)
        {
            PushTable(context);
        }

        private void PushTable(IParseTree context)
        {
            var bodyTable = new DeclarationTable {Parent = declarations.Count > 0 ? declarations.Peek() : null};
            declarations.Push(bodyTable);
            declarationTables.Put(context, bodyTable);
        }

        public override void ExitMachineBody(PParser.MachineBodyContext context)
        {
            declarations.Pop();
        }

        public override void EnterVarDecl(PParser.VarDeclContext context)
        {
            foreach (ITerminalNode varName in context.idenList().Iden())
            {
                RegisterDeclaration(varName.GetText(), context);
            }
        }

        public override void EnterFunDecl(PParser.FunDeclContext context)
        {
            string symbolName = context.name.Text;
            RegisterDeclaration(symbolName, context);
            PushTable(context);

            // Add function parameters to declaration
            PParser.IdenTypeListContext funParams = context.funParams().idenTypeList();
            foreach (PParser.IdenTypeContext idenTypePair in funParams?.idenType() ?? Enumerable.Empty<PParser.IdenTypeContext>())
            {
                string varName = idenTypePair.Iden().GetText();
                RegisterDeclaration(varName, idenTypePair);
            }
        }

        public override void ExitFunDecl(PParser.FunDeclContext context)
        {
            declarations.Pop();
        }

        public override void EnterFunProtoDecl(PParser.FunProtoDeclContext context)
        {
            string symbolName = context.name.Text;
            RegisterDeclaration(symbolName, context);
        }

        public override void EnterStateDecl(PParser.StateDeclContext context)
        {
            string symbolName = context.name.Text;
            RegisterDeclaration(symbolName, context);
        }

        public override void EnterStatementBlock(PParser.StatementBlockContext context)
        {
            PushTable(context);
        }

        public override void ExitStatementBlock(PParser.StatementBlockContext context)
        {
            declarations.Pop();
        }

        public override void EnterAnonEventHandler(PParser.AnonEventHandlerContext context)
        {
            PushTable(context);
        }

        public override void ExitAnonEventHandler(PParser.AnonEventHandlerContext context)
        {
            declarations.Pop();
        }

        public override void EnterPayloadVarDecl(PParser.PayloadVarDeclContext context)
        {
            string symbolName = context.Iden().GetText();
            RegisterDeclaration(symbolName, context);
        }
    }

    public class Analyzer
    {
        public static void Analyze(PParser parser, params PParser.ProgramContext[] programUnits)
        {
            foreach (PParser.ProgramContext programUnit in programUnits)
            {
                ParseTreeProperty<DeclarationTable> programDeclarations = DeclarationCollector.AnnotateWithDeclarations(programUnit);
                var walker = new ParseTreeWalker();
                walker.Walk(new DeclPrinter(programDeclarations), programUnit);
            }
        }

        private class DeclPrinter : IParseTreeListener
        {
            private readonly ParseTreeProperty<DeclarationTable> declarations;
            private int indentationLevel;

            public DeclPrinter(ParseTreeProperty<DeclarationTable> declarations)
            {
                this.declarations = declarations;
            }

            public void VisitTerminal(ITerminalNode node)
            {
            }

            public void VisitErrorNode(IErrorNode node)
            {
                throw new NotImplementedException();
            }

            public void EnterEveryRule(ParserRuleContext ctx)
            {
                Console.WriteLine("".PadLeft(indentationLevel, ' ') + $"{ctx.GetType().Name}: {PrintEnv(ctx)}");
                indentationLevel++;
            }

            public void ExitEveryRule(ParserRuleContext ctx)
            {
                indentationLevel--;
            }

            private string PrintEnv(ParserRuleContext ctx)
            {
                DeclarationTable declarationTable = declarations.Get(ctx);
                if (declarationTable == null)
                {
                    return "none";
                }

                string symbols = string.Join(", ", declarationTable.AllSymbols.Select(kv => $"{kv.Key}: {kv.Value.GetType().Name}"));
                return $"[{symbols}]";
            }
        }
    }
}