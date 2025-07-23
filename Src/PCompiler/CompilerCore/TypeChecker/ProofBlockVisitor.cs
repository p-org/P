using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Plang.Compiler;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker
{
    public class ProofBlockVisitor : PParserBaseVisitor<object>
    {

        private ITranslationErrorHandler Handler;
        private Scope CurrentScope;
        private ParseTreeProperty<IPDecl> nodesToDeclarations;

        private ProofBlockVisitor(ITranslationErrorHandler handler, Scope scope, ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            Handler = handler;
            CurrentScope = scope;
            this.nodesToDeclarations = nodesToDeclarations;
        }

        public static void PopulateProofBlocks(ITranslationErrorHandler handler,
        Scope topLevelScope,
        ParserRuleContext context,
        ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            var visitor = new ProofBlockVisitor(handler, topLevelScope, nodesToDeclarations);
            visitor.Visit(context);
        }

        private List<Invariant> ToInvariant(IPExpr e, ParserRuleContext context)
        {
            if (e is InvariantGroupRefExpr invGroupRef) return invGroupRef.Invariants;
            if (e is InvariantRefExpr invRef) return [invRef.Invariant];
            if (!PrimitiveType.Bool.IsSameTypeAs(e.Type.Canonicalize()))
            {
                throw Handler.TypeMismatch(context, e.Type, PrimitiveType.Bool);
            }
            Invariant inv = new Invariant($"tmp_inv_{Guid.NewGuid()}", e, context);
            return [inv];
        }

        public override object VisitProofBlock(PParser.ProofBlockContext context)
        {
            var proofBlock = (ProofBlock)nodesToDeclarations.Get(context);
            proofBlock.Commands = context.proofBody().proofItem().Select(Visit).Cast<ProofCommand>().ToList();
            proofBlock.Commands.ForEach(x => x.ProofBlock = proofBlock.Name);
            return proofBlock;
        }

        public override object VisitProveUsingCmd(PParser.ProveUsingCmdContext context)
        {
            var proofCmd = (ProofCommand)nodesToDeclarations.Get(context);
            var temporaryFunction = new Function(proofCmd.Name, context);
            temporaryFunction.Scope = CurrentScope.MakeChildScope();
            var exprVisitor = new ExprVisitor(temporaryFunction, Handler);
            List<IPExpr> premises = [];
            List<IPExpr> goals = [];
            List<IPExpr> excepts = context._excludes.Select(exprVisitor.Visit).ToList();
            if (context.premisesAll == null)
            {
                premises = context._premises.Select(exprVisitor.Visit).ToList();
            }
            else
            {
                premises = CurrentScope.AllDecls.OfType<Invariant>().Select(x => (IPExpr)new InvariantRefExpr(x, context)).ToList();
            }

            if (context.goalsAll == null && context.goalsDefault == null)
            {
                goals = context._targets.Select(exprVisitor.Visit).ToList();
            }
            else if (context.goalsDefault != null)
            {
                goals = [new InvariantRefExpr(new Invariant(context), context)];
            }
            else
            {
                goals = CurrentScope.AllDecls.OfType<Invariant>().Select(x => (IPExpr)new InvariantRefExpr(x, context)).ToList();
            }

            if (premises.Count == context._premises.Count)
            {
                proofCmd.Premises = premises.Zip(context._premises, (x, y) => ToInvariant(x, y)).SelectMany(x => x).ToList();
            }
            else
            {
                proofCmd.Premises = premises.SelectMany(x => ToInvariant(x, context)).ToList();
            }

            if (goals.Count == context._targets.Count)
            {
                proofCmd.Goals = goals.Zip(context._targets, (x, y) => ToInvariant(x, y)).SelectMany(x => x).ToList();
            }
            else
            {
                proofCmd.Goals = goals.SelectMany(x => ToInvariant(x, context)).ToList();
            }

            proofCmd.Excepts = excepts.Zip(context._excludes, (x, y) => ToInvariant(x, y)).SelectMany(x => x).ToList();
            proofCmd.Premises = proofCmd.Premises.Except(proofCmd.Excepts).ToList();
            proofCmd.Goals = proofCmd.Goals.Except(proofCmd.Excepts).ToList();

            // prove A using B, ..., C means A -> B, ..., A -> C
            // If there is a cycle in the graph formed by all prove-using commands, then we should throw an error. 
            // We could do this incrementally but the number of prove-using commands will probably be very small anyway
            // so we are just going to do a topological sort every time (https://gist.github.com/Sup3rc4l1fr4g1l1571c3xp14l1d0c10u5/3341dba6a53d7171fe3397d13d00ee3f)
            // TODO: using _ to pick out sub invariants?
            var nodes = new HashSet<string>();
            var edges = new HashSet<(string, string)>();
            foreach (var cmd in CurrentScope.ProofCommands)
            {
                if (cmd.Goals is null) continue;
                foreach (var source in cmd.Goals.Select(inv => inv.Name))
                {
                    if (cmd.Premises is null) continue;
                    foreach (var target in cmd.Premises.Select(inv => inv.Name))
                    {
                        nodes.Add(source);
                        nodes.Add(target);
                        edges.Add((source, target));
                    }
                }
            }

            // Set of all nodes with no incoming edges
            var S = new System.Collections.Generic.HashSet<string>(nodes.Where(n => edges.All(e => e.Item2.Equals(n) == false)));

            // while S is non-empty do
            while (S.Any())
            {

                //  remove a node n from S
                var n = S.First();
                S.Remove(n);

                // for each node m with an edge e from n to m do
                foreach (var e in edges.Where(e => e.Item1.Equals(n)).ToList())
                {
                    var m = e.Item2;

                    // remove edge e from the graph
                    edges.Remove(e);

                    // if m has no other incoming edges then
                    if (edges.All(me => me.Item2.Equals(m) == false))
                    {
                        S.Add(m);
                    }
                }
            }

            // if graph has edges then
            if (edges.Any())
            {
                throw Handler.CyclicProof(proofCmd.SourceLocation, proofCmd);
            }

            return proofCmd;
        }
    }
}