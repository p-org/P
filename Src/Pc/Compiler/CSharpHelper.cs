using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Pc
{
    class CSharpHelper
    {
        public static ExpressionStatementSyntax MkCSharpSimpleAssignmentExpressionStatement(SyntaxNode lhs, SyntaxNode rhs)
        {
            return SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        (ExpressionSyntax)lhs, (ExpressionSyntax)rhs))
                .NormalizeWhitespace();
        }
        public static LiteralExpressionSyntax MkCSharpStringLiteralExpression(string name)
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                                     SyntaxFactory.Literal(name));
        }
        public static LiteralExpressionSyntax MkCSharpFalseLiteralExpression()
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);

        }
        public static LiteralExpressionSyntax MkCSharpTrueLiteralExpression()
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);

        }
        public static LiteralExpressionSyntax MkCSharpNumericLiteralExpression(int arg)
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                     SyntaxFactory.Literal(arg));
        }
        public static GotoStatementSyntax MkCSharpGoto(string label)
        {
            return SyntaxFactory.GotoStatement(SyntaxKind.GotoStatement, SyntaxFactory.IdentifierName(label));
        }

        public static SyntaxNode MkCSharpParameter(SyntaxToken par, TypeSyntax type)
        {
            return SyntaxFactory.Parameter(par).WithType(type);
        }
        public static List<SyntaxNodeOrToken> MkCSharpParameterList(List<SyntaxNode> args)
        {
            List<SyntaxNodeOrToken> acc = new List<SyntaxNodeOrToken>();
            for (int i = 0; i < args.Count(); i++)
            {
                acc.Add(args[i]);
                if (i < args.Count() - 1)
                {
                    acc.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                }
            }
            return acc;
        }
        public static List<SyntaxNodeOrToken> MkCSharpArgumentList(params ExpressionSyntax[] args)
        {
            List<SyntaxNodeOrToken> acc = new List<SyntaxNodeOrToken>();
            for (int i = 0; i < args.Count(); i++)
            {
                acc.Add(SyntaxFactory.Argument(args[i]));
                if (i < args.Count() - 1)
                {
                    acc.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                }
            }
            return acc;
        }

        public static SimpleBaseTypeSyntax MkCSharpIdentifierNameType(string type)
        {
            return SyntaxFactory.SimpleBaseType((TypeSyntax)SyntaxFactory.IdentifierName(type));
        }
        public static TypeSyntax MkCSharpGenericListType(TypeSyntax type)
        {
            return SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("List"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            type)));
        }

        public static SyntaxNode MkCSharpFieldDeclaration(SyntaxNode type,
                                                          string name, SyntaxToken accessibility, SyntaxToken publicStatic)
        {
            var nameDecl = SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("" + name + ""));
            return SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration((TypeSyntax)type)
                        .WithVariables(
                            SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                                nameDecl)))
                .WithModifiers(
                    SyntaxFactory.TokenList(new[] { accessibility, publicStatic }))
                .NormalizeWhitespace();
        }

        public static SyntaxNode MkCSharpFieldDeclarationWithInit(SyntaxNode type,
                                                                  string name, SyntaxToken accessibility, SyntaxToken publicStatic, ExpressionSyntax init)
        {
            var nameDecl = SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("" + name + "")).WithInitializer(SyntaxFactory.EqualsValueClause(init));
            return SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration((TypeSyntax)type)
                        .WithVariables(
                            SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                                nameDecl)))
                .WithModifiers(
                    SyntaxFactory.TokenList(new[] { accessibility, publicStatic }))
                .NormalizeWhitespace();
        }

        public static ExpressionSyntax MkCSharpDot(string first, params string[] names)
        {
            return CSharpHelper.MkCSharpDot(SyntaxFactory.IdentifierName(first), names);
        }

        public static ExpressionSyntax MkCSharpDot(ExpressionSyntax first, params string[] names)
        {
            Debug.Assert(names.Length > 0);

            ExpressionSyntax lhs = first;
            for (int i = 0; i < names.Length; i++)
            {
                SimpleNameSyntax rhs = SyntaxFactory.IdentifierName(names[i]);
                lhs = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ParenthesizedExpression(lhs),
                    rhs);
            }
            return lhs.NormalizeWhitespace();
        }

        public static ElementAccessExpressionSyntax MkCSharpElementAccessExpression(SyntaxNode first, int index)
        {
            return SyntaxFactory.ElementAccessExpression(
                    (ExpressionSyntax)first)
                .WithArgumentList(
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(index))))));
        }
        public static ElementAccessExpressionSyntax MkCSharpElementAccessExpression(SyntaxNode first, SyntaxNode index)
        {
            return SyntaxFactory.ElementAccessExpression(
                    (ExpressionSyntax)first)
                .WithArgumentList(
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                            SyntaxFactory.Argument((ExpressionSyntax)index))));
        }
        public static CastExpressionSyntax MkCSharpCastExpression(string type, SyntaxNode expr)
        {
            return SyntaxFactory.CastExpression(
                SyntaxFactory.IdentifierName(type),
                SyntaxFactory.ParenthesizedExpression((ExpressionSyntax)expr));
        }
        //OmittedArraySizeExpression case only:
        public static SyntaxNode MkCSharpArrayCreationExpression(string type, SyntaxNodeOrToken[] initializer)
        {
            return SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.ArrayType(SyntaxFactory.IdentifierName(type))
                        .WithRankSpecifiers(
                            SyntaxFactory.SingletonList(
                                SyntaxFactory.ArrayRankSpecifier(
                                    SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                        SyntaxFactory.OmittedArraySizeExpression())))))
                .WithInitializer(
                    SyntaxFactory.InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>(initializer)));
        }
        public static ObjectCreationExpressionSyntax MkCSharpObjectCreationExpression(SyntaxNode type, params SyntaxNode[] names)
        {
            List<SyntaxNode> hd = new List<SyntaxNode>();
            if (names.Length == 0)
            {
                return SyntaxFactory.ObjectCreationExpression(
                        (TypeSyntax)type)
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList());
            }
            //TODO(improve): merge this case with the general case
            else if (names.Length == 1)
            {
                return SyntaxFactory.ObjectCreationExpression(
                        (TypeSyntax)type)
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                SyntaxFactory.Argument(((ExpressionSyntax)names[0])))))
                    //IdentifierName("p1")))))
                    .NormalizeWhitespace();
            }
            else
            {
                hd.Add(SyntaxFactory.Argument((ExpressionSyntax)names[0]));
                for (int i = 1; i < names.Length; i++)
                {
                    ArgumentSyntax tl = SyntaxFactory.Argument((ExpressionSyntax)names[i]);
                    hd.Add(tl);
                }
                //hd contains list of Argument(IdentifierName(names[i]))
                //Insert Token(SyntaxKind.CommaToken) after each Argument except the last one 
                //and create new SyntaxNodeOrToken[] out of the result:
                List<SyntaxNodeOrToken> hdWithCommas = CSharpHelper.MkCSharpParameterList(hd);
                //TODO(question): in Roslyn quoter, the initialization for List<SyntaxNodeOrToken>
                //looks different: "new SyntaxNodeOrToken[]{ el1, el2, ... }
                //Does that matter?
                return SyntaxFactory.ObjectCreationExpression(
                        (TypeSyntax)type)
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                new List<SyntaxNodeOrToken>(hdWithCommas))))
                    .NormalizeWhitespace();
            }
        }
        public static AccessorDeclarationSyntax MkCSharpAccessor(string getSet, SyntaxList<StatementSyntax> body)
        {
            if (getSet == "get")
            {
                return SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, SyntaxFactory.Block(body));
            }
            else
            {
                return SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, SyntaxFactory.Block(body));
            }
        }

        public static ExpressionSyntax MkCSharpEq(ExpressionSyntax expr1, ExpressionSyntax expr2)
        {
            return SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, expr1, expr2);
        }

        public static ExpressionSyntax MkCSharpEquals(ExpressionSyntax expr1, ExpressionSyntax expr2)
        {
            return MkCSharpInvocationExpression(MkCSharpDot(expr1, "Equals"), expr2);
        }
        public static ExpressionSyntax MkCSharpNeq(ExpressionSyntax expr1, ExpressionSyntax expr2)
        {
            return SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, expr1, expr2);
        }

        public static ExpressionSyntax MkCSharpNot(ExpressionSyntax expr)
        {
            return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, expr);
        }
        public static StatementSyntax MkCSharpAssert(ExpressionSyntax expr, string errorMsg)
        {
            return SyntaxFactory.IfStatement(
                CSharpHelper.MkCSharpNot(expr),
                SyntaxFactory.ThrowStatement(MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtAssertFailureException"), CSharpHelper.MkCSharpStringLiteralExpression(errorMsg))));
        }
        public static StatementSyntax MkCSharpPrint(string msg, List<ExpressionSyntax> pars)
        {
            msg = "<PrintLog>" + " " + msg;
            var allPars = new List<ExpressionSyntax>(pars);
            allPars.Insert(0, CSharpHelper.MkCSharpStringLiteralExpression(msg));
            return SyntaxFactory.ExpressionStatement(MkCSharpInvocationExpression(
                                           MkCSharpDot("application", "Trace"),
                                           allPars.ToArray()));
        }
        public static StatementSyntax MkCSharpTrace(string msg, params ExpressionSyntax[] pars)
        {
            var allPars = new List<ExpressionSyntax>(pars);
            allPars.Insert(0, CSharpHelper.MkCSharpStringLiteralExpression(msg));
            return SyntaxFactory.ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("application", "TraceLine"), allPars.ToArray()));
        }
        public static InvocationExpressionSyntax MkCSharpInvocationExpression(SyntaxNode first, params ExpressionSyntax[] pars)
        {
            var args = CSharpHelper.MkCSharpArgumentList(pars);
            return SyntaxFactory.InvocationExpression((ExpressionSyntax)first)
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList<ArgumentSyntax>(args)));
        }

        public static PrefixUnaryExpressionSyntax MkCSharpUnaryExpression(SyntaxKind op, SyntaxNode arg)
        {
            return SyntaxFactory.PrefixUnaryExpression(op, (ExpressionSyntax)arg);
        }
        public static SyntaxNode MkCSharpPropertyDecl(string type, string name,
                                                      SyntaxTokenList modifiers, params AccessorDeclarationSyntax[] accessorList)
        {
            return SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.IdentifierName(type),
                    SyntaxFactory.Identifier(name))
                .WithModifiers(modifiers)
                .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(accessorList)));
        }
        public static SyntaxNode MkCSharpMethodDeclaration(SyntaxNode type, SyntaxToken name, SyntaxToken[] attrs,
                                                           SyntaxList<StatementSyntax> body, List<SyntaxNode> methodPars)
        {
            List<SyntaxNodeOrToken> pars = CSharpHelper.MkCSharpParameterList(methodPars);
            return SyntaxFactory.MethodDeclaration((TypeSyntax)type, name)
                .WithModifiers(SyntaxFactory.TokenList(attrs))
                .WithParameterList(
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SeparatedList<ParameterSyntax>(pars)))
                .WithBody(SyntaxFactory.Block(body))
                .NormalizeWhitespace();
        }
        public static ConstructorInitializerSyntax MkCSharpConstructorInitializer(SyntaxKind constrInitializer,
                                                                                  List<SyntaxNodeOrToken> pars)
        {
            return SyntaxFactory.ConstructorInitializer(constrInitializer, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(pars)));
        }
        public static ConstructorDeclarationSyntax MkCSharpConstructor(SyntaxToken name, SyntaxTokenList modifiers,
                                                                       List<SyntaxNode> constrPars, ConstructorInitializerSyntax initializer, List<StatementSyntax> body)
        {
            List<SyntaxNodeOrToken> pars = CSharpHelper.MkCSharpParameterList(constrPars);
            return SyntaxFactory.ConstructorDeclaration(name)
                .WithModifiers(modifiers)
                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList<ParameterSyntax>(pars)))
                .WithInitializer(initializer)
                .WithBody(SyntaxFactory.Block(body));
        }
        public static SyntaxNode MkCSharpClassDecl(string name, SyntaxTokenList modifiers, SeparatedSyntaxList<BaseTypeSyntax> type,
                                                   SyntaxList<MemberDeclarationSyntax> members)
        {
            return SyntaxFactory.ClassDeclaration(name)
                .WithModifiers(modifiers)
                .WithBaseList(SyntaxFactory.BaseList(type))
                .WithMembers(members)
                .NormalizeWhitespace();
        }

        public static LabeledStatementSyntax MkCSharpEmptyLabeledStatement(string label)
        {
            return SyntaxFactory.LabeledStatement(
                SyntaxFactory.Identifier(label),
                SyntaxFactory.EmptyStatement());
        }
    }
}