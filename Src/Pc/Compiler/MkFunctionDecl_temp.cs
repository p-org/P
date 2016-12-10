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
                    return (ExpressionSyntax)args[0];
                }
                else
                {
                    //return new PrtTupleValue(tupTypeExpr, args);
                    // return new PrtTupleValue(tupTypeExpr, args[0], args[1], args[args.Count - 1]);
                    SyntaxNode[] pars = new SyntaxNode[args.Count];
                    pars[0] = tupTypeExpr;
                    for (int i = 1; i < pars.Length; i++)
                    {
                        pars[i] = args[i - 1];
                    }
   
                    return MkCSharpObjectCreationExpression(IdentifierName("PrtTupleValue"), pars);
                    //return ObjectCreationExpression(
                    //            IdentifierName("PrtTupleValue"))
                    //       .WithArgumentList(
                    //            ArgumentList(
                    //                SeparatedList<ArgumentSyntax>(
                    //                    new SyntaxNodeOrToken[]{
                    //                        Argument(
                    //                            IdentifierName("tupTypeExpr")),
                    //                        Token(SyntaxKind.CommaToken),
                    //                        Argument(
                    //                            IdentifierName("args"))})))
                    //       .NormalizeWhitespace();
                }
            }

            SyntaxNode FoldRaise(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }

            SyntaxNode FoldSend(FuncTerm ft, List<SyntaxNode> args)
            {
                //code to be generated:
                //Line 1 (template everything except event and <payload value>): 
                //target.PrtEnqueueEvent(event, <payload value>, parent);
                //Example:target.PrtEnqueueEvent(dummy, PrtValue.NullValue, parent);
                //public override void PrtEnqueueEvent(PrtValue e, PrtValue arg, PrtMachine source)
                //event: children[1]
                //<payload value>: compute from children[2-children.Count()]

                //Line 2 (template everything): 
                //parent.PrtFunContSend(this, currFun.locals, currFun.returnTolocation);
                //Example: parent.PrtFunContSend(this, currFun.locals, 1);
                //public void PrtFunContSend(PrtFun fun, List<PrtValue> locals, int ret)

                //List<SyntaxNode> args = new List<SyntaxNode>(children.Select(x => x));
                // (PrtMachineValue)args[0]                
                SyntaxNode targetExpr = MkCSharpCastExpression("PrtMachineValue", args[0]);
                // (PrtEventValue)args[1]
                ExpressionSyntax eventExpr = (ExpressionSyntax)MkCSharpCastExpression("PrtEventValue", args[1]);
                args.RemoveRange(0, 2);
                ExpressionSyntax tupleTypeExpr = (ExpressionSyntax)MkCSharpDot(eventExpr, "payloadType");
                ExpressionSyntax payloadExpr = (ExpressionSyntax)MkPayload(tupleTypeExpr, args);
                var invocationArgs = new ArgumentSyntax[]
                {
                    Argument(eventExpr), Argument(payloadExpr), Argument((ExpressionSyntax)MkCSharpIdentifierName("parent"))
                };
                StatementSyntax enqueueEventStmt = ExpressionStatement(
                    (ExpressionSyntax)MkCSharpInvocationExpression(
                    (ExpressionSyntax)MkCSharpDot((ExpressionSyntax)targetExpr, "PrtEnqueueEvent"),
                     invocationArgs));

                invocationArgs = new ArgumentSyntax[]
                {
                    Argument(ThisExpression()),
                    Argument((ExpressionSyntax)MkCSharpDot("currFun", "locals")),
                    Argument((ExpressionSyntax)MkCSharpDot("currFun", "returnTolocation"))
                };
                StatementSyntax contStmt = ExpressionStatement(
                    (ExpressionSyntax)MkCSharpInvocationExpression(
                    (ExpressionSyntax)MkCSharpDot("parent", "PrtFunContSend"),
                     invocationArgs));

                var afterLabel = GetFreshLabel();
                StatementSyntax afterStmt = MkCSharpEmptyLabeledStmt(afterLabel);

                return Block(enqueueEventStmt, contStmt, afterStmt);
            }
            
            #endregion
        }
    }
}
