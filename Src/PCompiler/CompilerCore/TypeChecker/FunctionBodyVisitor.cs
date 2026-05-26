using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Statements;

namespace Plang.Compiler.TypeChecker
{
    public class FunctionBodyVisitor : PParserBaseVisitor<object>
    {
        private readonly ICompilerConfiguration config;
        private readonly Machine machine;
        private readonly Function method;

        private FunctionBodyVisitor(ICompilerConfiguration config, Machine machine, Function method)
        {
            this.config = config;
            this.machine = machine;
            this.method = method;
        }

        public static void PopulateMethod(ICompilerConfiguration config, Function fun)
        {
            Contract.Requires(fun.Body == null);
            var visitor = new FunctionBodyVisitor(config, fun.Owner, fun);
            visitor.Visit(fun.SourceLocation);

            // Invariant: a non-foreign Function must have a non-null Body, since
            // Function.IsForeign is defined as `Body == null`. In collecting mode
            // an inner visit may report-and-recover without throwing, but if a
            // sub-step (e.g. TypeResolver.ResolveType on a bad var decl) bailed
            // without ever reaching `method.Body = ...` below at line VisitFunctionBody,
            // the function would be silently misclassified as foreign and
            // ControlFlowChecker / capability checks would skip it. Backfill an
            // empty CompoundStmt as a fail-safe — Compiler.cs's HasErrors gate
            // ensures this never reaches the IR transformer or backends.
            if (fun.Body == null && !fun.Role.HasFlag(FunctionRole.Foreign))
            {
                fun.Body = new CompoundStmt(fun.SourceLocation, new List<IPStmt>());
            }
        }

        public override object VisitAnonEventHandler(PParser.AnonEventHandlerContext context)
        {
            return Visit(context.functionBody());
        }

        public override object VisitNoParamAnonEventHandler(PParser.NoParamAnonEventHandlerContext context)
        {
            return Visit(context.functionBody());
        }

        public override object VisitPFunDecl(PParser.PFunDeclContext context)
        {
            return Visit(context.functionBody());
        }

        public override object VisitForeignFunDecl(PParser.ForeignFunDeclContext context)
        {
            return null;
        }

        public override object VisitFunctionBody(PParser.FunctionBodyContext context)
        {
            // TODO: check that parameters have been added to internal scope?

            // Add all local variables to scope.
            foreach (var varDeclContext in context.varDecl())
            {
                Visit(varDeclContext);
            }
            
            // Then we validate this scope doesn't redeclare the global params
            method.Scope.ValidateGlobalParamsUnique(config.Handler);

            // Build the statement trees
            var statementVisitor = new StatementVisitor(config, machine, method);
            method.Body = (CompoundStmt)statementVisitor.Visit(context);
            return null;
        }

        public override object VisitVarDecl(PParser.VarDeclContext context)
        {
            foreach (var varName in context.idenList()._names)
            {
                var variable = method.Scope.Put(varName.GetText(), varName, VariableRole.Local);
                variable.Type = TypeResolver.ResolveType(context.type(), method.Scope, config.Handler);
                method.AddLocalVariable(variable);
            }

            return null;
        }
    }
}