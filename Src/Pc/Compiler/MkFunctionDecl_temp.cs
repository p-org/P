using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Formula.API;
using Microsoft.Formula.API.ASTQueries;
using Microsoft.Formula.API.Nodes;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Pc
{
    partial class PToCSharp : PTranslation
    {
        internal partial class MkFunctionDecl
        {
            #region FoldUnfold

            private SyntaxNode MkPayload(SyntaxNode tupTypeExpr, List<SyntaxNode> args)
            {
                if (args.Count == 0)
                {
                    return MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("PrtValue"),
                                IdentifierName("NullValue"));
                }
                else if (args.Count == 1)
                {
                    return args[0];
                }
                else
                {
                    // return new PrtTupleValue(tupTypeExpr, args[0], args[1], args[args.Count - 1]);
                    return null;
                }
            }

            SyntaxNode FoldRaise(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }

            SyntaxNode FoldSend(FuncTerm ft, List<SyntaxNode> children)
            {
                //code to be generated:
                //Line 1 (template everything except event and <payload value>): 
                //parent.PrtEnqueueEvent(event, <payload value>, parent);
                //Example:parent.PrtEnqueueEvent(dummy, PrtValue.NullValue, parent);
                //public override void PrtEnqueueEvent(PrtValue e, PrtValue arg, PrtMachine source)
                //event: children[1]
                //<payload value>: compute from children[2-children.Count()]

                //Line 2 (template everything): 
                //parent.PrtFunContSend(this, currFun.locals, currFun.returnTolocation);
                //TODO(question):check that the last parameter is correct
                //Example: parent.PrtFunContSend(this, currFun.locals, 1);
                //public void PrtFunContSend(PrtFun fun, List<PrtValue> locals, int ret)

                //List<AST<Node>> args = new List<AST<Node>>(children.Select(x => x));                  
                SyntaxNode targetExpr; // (PrtMachineValue)children[0]
                ExpressionSyntax eventExpr; // (PrtEventValue)children[1]
                ExpressionSyntax payloadExpr; // MkPayload(eventExpr.payloadType, args[2..])
                var enqueueEvent =
                    ExpressionStatement(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("parent"),
                                IdentifierName("PrtEnqueueEvent")))
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList<ArgumentSyntax>(
                                    new SyntaxNodeOrToken[]{
                                        Argument(
                                            //TODO: replace with real expr
                                            IdentifierName("eventExpr")),
                                        Token(SyntaxKind.CommaToken),
                                        Argument(
                                            //TODO: replace with real expr
                                            IdentifierName("payloadExpr")),
                                        Token(SyntaxKind.CommaToken),
                                        Argument(
                                            IdentifierName("parent"))}))))
                    .NormalizeWhitespace();

                var contSend =
                    ExpressionStatement(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("parent"),
                                IdentifierName("PrtFunContSend")))
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList<ArgumentSyntax>(
                                    new SyntaxNodeOrToken[]{
                                        Argument(
                                            ThisExpression()),
                                        Token(SyntaxKind.CommaToken),
                                        Argument(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("currFun"),
                                                IdentifierName("locals"))),
                                        Token(SyntaxKind.CommaToken),
                                        Argument(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("currFun"),
                                                IdentifierName("returnTolocation")))}))))
                    .NormalizeWhitespace();


                throw new NotImplementedException();
            }
            
            #endregion
        }
    }
}
