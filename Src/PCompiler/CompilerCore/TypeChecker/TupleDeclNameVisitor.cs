using System;
using Antlr4.Runtime.Tree;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;
using Plang.Compiler.Util;

namespace Plang.Compiler.TypeChecker
{
    public class TupleDeclNameVisitor : PParserBaseVisitor<object>
    {
        private readonly StackProperty<Machine> _currentMachine = new StackProperty<Machine>();
        private readonly StackProperty<Scope> _currentScope;
        private readonly ParseTreeProperty<IPDecl> _nodesToDeclarations;

        private Scope CurrentScope => _currentScope.Value;
        private Machine CurrentMachine => _currentMachine.Value;
        private ITranslationErrorHandler Handler { get; }

        private TupleDeclNameVisitor(
            ITranslationErrorHandler handler,
            Scope topLevelScope,
            ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            Handler = handler;
            _currentScope = new StackProperty<Scope>(topLevelScope);
            _nodesToDeclarations = nodesToDeclarations;
        }

        /// <summary>
        /// Once all declarations have been resolved by the DeclarationVisitor pass, we patch up any
        /// tuple declarations that were declared in the context of a TypeDef[1] or an Event declaration[2].
        ///
        /// [1]: example: `type tTrans = (key: string, val: int, transId: int);`
        /// [2]: example: `event eWriteTransReq : tWriteTransReq;`
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="topLevelScope"></param>
        /// <param name="context"></param>
        /// <param name="nodesToDeclarations"></param>
        public static void HydrateTupleDecls(
            ITranslationErrorHandler handler,
            Scope topLevelScope,
            PParser.ProgramContext context,
            ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            var visitor = new TupleDeclNameVisitor(handler, topLevelScope, nodesToDeclarations);
            visitor.Visit(context);
        }

        public override object VisitEventDecl(PParser.EventDeclContext context)
        {
            // EVENT name=Iden
            var pEvent = (PEvent) _nodesToDeclarations.Get(context);

            if (pEvent.PayloadType == null)
            {
                throw new Exception("pEvent.PayloadType is null (did DeclarationVisitor not run already?)");
            }

            // For Tuple types, now that we have resolved the type of the typedef
            // we set the tuple's type's name.
            if (pEvent.PayloadType is TypeDefType td)
            {
                switch (resolveTypeDefs(pEvent.PayloadType))
                {
                case NamedTupleType nt when nt.TypeDefedName == null:
                    nt.TypeDefedName = td.TypeDefDecl.Name;
                    break;
                case TupleType tt when tt.TypeDefedName == null:
                    tt.TypeDefedName = td.TypeDefDecl.Name;
                    break;
                }
            }

            // SEMI
            return pEvent;
        }


        public override object VisitPTypeDef(PParser.PTypeDefContext context)
        {
            // TYPE name=iden
            var typedef = (TypeDef) _nodesToDeclarations.Get(context);

            // ASSIGN type
            if (typedef.Type == null)
            {
                throw new Exception("typedef.Type is null (did DeclarationVisitor not run already?)");
            }

            // For Tuple types, now that we have resolved the type of the typedef
            // we set the tuple's type's name.
            PLanguageType resolved = resolveTypeDefs(typedef.Type);
            switch (resolved)
            {
                case NamedTupleType nt when nt.TypeDefedName == null:
                    nt.TypeDefedName = typedef.Name;
                    break;
                case TupleType tt when tt.TypeDefedName == null:
                    tt.TypeDefedName = typedef.Name;
                    break;
            }

            // SEMI
            return typedef;
        }

        private PLanguageType resolveTypeDefs(PLanguageType decl)
        {
            PLanguageType curr = decl;
            while (curr is TypeDefType td)
            {
                curr = td.TypeDefDecl.Type;
            }

            return curr;
        }
    }
}
