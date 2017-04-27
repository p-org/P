using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.IO;

using Microsoft.Formula.API;
using Microsoft.Formula.API.ASTQueries;
using Microsoft.Formula.API.Nodes;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Pc
{
    class CSharpHelper
    {
        public static ExpressionStatementSyntax MkCSharpSimpleAssignmentExpressionStatement(SyntaxNode lhs, SyntaxNode rhs)
        {
            return ExpressionStatement(
                     AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        (ExpressionSyntax)lhs, (ExpressionSyntax)rhs))
                   .NormalizeWhitespace();
        }
        public static LiteralExpressionSyntax MkCSharpStringLiteralExpression(string name)
        {
            return LiteralExpression(SyntaxKind.StringLiteralExpression,
                Literal(name));
        }
        public static LiteralExpressionSyntax MkCSharpFalseLiteralExpression()
        {
            return LiteralExpression(SyntaxKind.FalseLiteralExpression);

        }
        public static LiteralExpressionSyntax MkCSharpTrueLiteralExpression()
        {
            return LiteralExpression(SyntaxKind.TrueLiteralExpression);

        }
        public static LiteralExpressionSyntax MkCSharpNumericLiteralExpression(int arg)
        {
            return LiteralExpression(SyntaxKind.NumericLiteralExpression,
                Literal(arg));
        }
        public static GotoStatementSyntax MkCSharpGoto(string label)
        {
            return GotoStatement(SyntaxKind.GotoStatement, IdentifierName(label));
        }

        public static SyntaxNode MkCSharpParameter(SyntaxToken par, TypeSyntax type)
        {
            return Parameter(par).WithType(type);
        }
        public static List<SyntaxNodeOrToken> MkCSharpParameterList(List<SyntaxNode> args)
        {
            List<SyntaxNodeOrToken> acc = new List<SyntaxNodeOrToken>();
            for (int i = 0; i < args.Count(); i++)
            {
                acc.Add(args[i]);
                if (i < args.Count() - 1)
                {
                    acc.Add(Token(SyntaxKind.CommaToken));
                }
            }
            return acc;
        }
        public static List<SyntaxNodeOrToken> MkCSharpArgumentList(params ExpressionSyntax[] args)
        {
            List<SyntaxNodeOrToken> acc = new List<SyntaxNodeOrToken>();
            for (int i = 0; i < args.Count(); i++)
            {
                acc.Add(Argument(args[i]));
                if (i < args.Count() - 1)
                {
                    acc.Add(Token(SyntaxKind.CommaToken));
                }
            }
            return acc;
        }

        public static SimpleBaseTypeSyntax MkCSharpIdentifierNameType(string type)
        {
            return SimpleBaseType((TypeSyntax)IdentifierName(type));
        }
        public static TypeSyntax MkCSharpGenericListType(TypeSyntax type)
        {
            return GenericName(
                            Identifier("List"))
                   .WithTypeArgumentList(
                            TypeArgumentList(
                                SingletonSeparatedList(
                                    type)));
        }

        public static SyntaxNode MkCSharpFieldDeclaration(SyntaxNode type,
                        string name, SyntaxToken accessibility, SyntaxToken publicStatic)
        {
            var nameDecl = VariableDeclarator(Identifier("" + name + ""));
            return FieldDeclaration(
                    VariableDeclaration((TypeSyntax)type)
                    .WithVariables(
                       SingletonSeparatedList<VariableDeclaratorSyntax>(
                          nameDecl)))
                     .WithModifiers(
                        TokenList(new[] { accessibility, publicStatic }))
                   .NormalizeWhitespace();
        }

        public static SyntaxNode MkCSharpFieldDeclarationWithInit(SyntaxNode type,
                        string name, SyntaxToken accessibility, SyntaxToken publicStatic, ExpressionSyntax init)
        {
            var nameDecl = VariableDeclarator(Identifier("" + name + "")).WithInitializer(EqualsValueClause(init));
            return FieldDeclaration(
                    VariableDeclaration((TypeSyntax)type)
                    .WithVariables(
                       SingletonSeparatedList<VariableDeclaratorSyntax>(
                          nameDecl)))
                     .WithModifiers(
                        TokenList(new[] { accessibility, publicStatic }))
                   .NormalizeWhitespace();
        }

        public static ExpressionSyntax MkCSharpDot(string first, params string[] names)
        {
            return CSharpHelper.MkCSharpDot(IdentifierName(first), names);
        }

        public static ExpressionSyntax MkCSharpDot(ExpressionSyntax first, params string[] names)
        {
            Debug.Assert(names.Length > 0);

            ExpressionSyntax lhs = first;
            for (int i = 0; i < names.Length; i++)
            {
                SimpleNameSyntax rhs = IdentifierName(names[i]);
                lhs = MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ParenthesizedExpression(lhs),
                            rhs);
            }
            return lhs.NormalizeWhitespace();
        }

        public static ElementAccessExpressionSyntax MkCSharpElementAccessExpression(SyntaxNode first, int index)
        {
            return ElementAccessExpression(
                      (ExpressionSyntax)first)
                   .WithArgumentList(
                          BracketedArgumentList(
                             SingletonSeparatedList<ArgumentSyntax>(
                                 Argument(
                                    LiteralExpression(
                                       SyntaxKind.NumericLiteralExpression,
                                          Literal(index))))));
        }
        public static ElementAccessExpressionSyntax MkCSharpElementAccessExpression(SyntaxNode first, SyntaxNode index)
        {
            return ElementAccessExpression(
                      (ExpressionSyntax)first)
                   .WithArgumentList(
                          BracketedArgumentList(
                             SingletonSeparatedList<ArgumentSyntax>(
                                 Argument((ExpressionSyntax)index))));
        }
        public static CastExpressionSyntax MkCSharpCastExpression(string type, SyntaxNode expr)
        {
            return CastExpression(
                        IdentifierName(type),
                        ParenthesizedExpression((ExpressionSyntax)expr));
        }
        //OmittedArraySizeExpression case only:
        public static SyntaxNode MkCSharpArrayCreationExpression(string type, SyntaxNodeOrToken[] initializer)
        {
            return ArrayCreationExpression(
                    ArrayType(IdentifierName(type))
                    .WithRankSpecifiers(
                         SingletonList(
                                    ArrayRankSpecifier(
                                        SingletonSeparatedList<ExpressionSyntax>(
                                            OmittedArraySizeExpression())))))
                        .WithInitializer(
                            InitializerExpression(
                                            SyntaxKind.ArrayInitializerExpression,
                                            SeparatedList<ExpressionSyntax>(initializer)));
        }
        public static ObjectCreationExpressionSyntax MkCSharpObjectCreationExpression(SyntaxNode type, params SyntaxNode[] names)
        {
            List<SyntaxNode> hd = new List<SyntaxNode>();
            if (names.Length == 0)
            {
                return ObjectCreationExpression(
                    (TypeSyntax)type)
                   .WithArgumentList(
                     ArgumentList());
            }
            //TODO(improve): merge this case with the general case
            else if (names.Length == 1)
            {
                return ObjectCreationExpression(
                        (TypeSyntax)type)
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList<ArgumentSyntax>(
                                Argument(((ExpressionSyntax)names[0])))))
                    //IdentifierName("p1")))))
                    .NormalizeWhitespace();
            }
            else
            {
                hd.Add(Argument((ExpressionSyntax)names[0]));
                for (int i = 1; i < names.Length; i++)
                {
                    ArgumentSyntax tl = Argument((ExpressionSyntax)names[i]);
                    hd.Add(tl);
                }
                //hd contains list of Argument(IdentifierName(names[i]))
                //Insert Token(SyntaxKind.CommaToken) after each Argument except the last one 
                //and create new SyntaxNodeOrToken[] out of the result:
                List<SyntaxNodeOrToken> hdWithCommas = CSharpHelper.MkCSharpParameterList(hd);
                //TODO(question): in Roslyn quoter, the initialization for List<SyntaxNodeOrToken>
                //looks different: "new SyntaxNodeOrToken[]{ el1, el2, ... }
                //Does that matter?
                return ObjectCreationExpression(
                            (TypeSyntax)type)
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList<ArgumentSyntax>(
                                    new List<SyntaxNodeOrToken>(hdWithCommas))))
                       .NormalizeWhitespace();
            }
        }
        public static AccessorDeclarationSyntax MkCSharpAccessor(string getSet, SyntaxList<StatementSyntax> body)
        {
            if (getSet == "get")
            {
                return AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, Block(body));
            }
            else
            {
                return AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, Block(body));
            }
        }

        public static ExpressionSyntax MkCSharpEq(ExpressionSyntax expr1, ExpressionSyntax expr2)
        {
            return BinaryExpression(SyntaxKind.EqualsExpression, expr1, expr2);
        }

        public static ExpressionSyntax MkCSharpNeq(ExpressionSyntax expr1, ExpressionSyntax expr2)
        {
            return BinaryExpression(SyntaxKind.NotEqualsExpression, expr1, expr2);
        }

        public static ExpressionSyntax MkCSharpNot(ExpressionSyntax expr)
        {
            return PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, expr);
        }
        public static StatementSyntax MkCSharpAssert(ExpressionSyntax expr, string errorMsg)
        {
            return IfStatement(
                CSharpHelper.MkCSharpNot(expr),
                ThrowStatement(MkCSharpObjectCreationExpression(IdentifierName("PrtAssertFailureException"), CSharpHelper.MkCSharpStringLiteralExpression(errorMsg))));
        }
        public static StatementSyntax MkCSharpPrint(string msg, params ExpressionSyntax[] pars)
        {
            msg = "<PrintLog>" + " " + msg;
            var allPars = new List<ExpressionSyntax>(pars);
            allPars.Insert(0, CSharpHelper.MkCSharpStringLiteralExpression(msg));
            return ExpressionStatement(MkCSharpInvocationExpression(
                                            MkCSharpDot("application", "Trace"),
                                            allPars.ToArray()));
        }
        public static StatementSyntax MkCSharpTrace(string msg, params ExpressionSyntax[] pars)
        {
            var allPars = new List<ExpressionSyntax>(pars);
            allPars.Insert(0, CSharpHelper.MkCSharpStringLiteralExpression(msg));
            return ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("application", "TraceLine"), allPars.ToArray()));
        }
        public static InvocationExpressionSyntax MkCSharpInvocationExpression(SyntaxNode first, params ExpressionSyntax[] pars)
        {
            var args = CSharpHelper.MkCSharpArgumentList(pars);
            return InvocationExpression((ExpressionSyntax)first)
                    .WithArgumentList(
                         ArgumentList(
                                SeparatedList<ArgumentSyntax>(args)));
        }

        public static PrefixUnaryExpressionSyntax MkCSharpUnaryExpression(SyntaxKind op, SyntaxNode arg)
        {
            return PrefixUnaryExpression(op, (ExpressionSyntax)arg);
        }
        public static SyntaxNode MkCSharpPropertyDecl(string type, string name,
                                     SyntaxTokenList modifiers, params AccessorDeclarationSyntax[] accessorList)
        {
            return PropertyDeclaration(
                      IdentifierName(type),
                      Identifier(name))
                   .WithModifiers(modifiers)
                   .WithAccessorList(AccessorList(List(accessorList)));
        }
        public static SyntaxNode MkCSharpMethodDeclaration(SyntaxNode type, SyntaxToken name, SyntaxToken[] attrs,
            SyntaxList<StatementSyntax> body, List<SyntaxNode> methodPars)
        {
            List<SyntaxNodeOrToken> pars = CSharpHelper.MkCSharpParameterList(methodPars);
            return MethodDeclaration((TypeSyntax)type, name)
                   .WithModifiers(TokenList(attrs))
                   .WithParameterList(
                        ParameterList(
                            SeparatedList<ParameterSyntax>(pars)))
                   .WithBody(Block(body))
                   .NormalizeWhitespace();
        }
        public static ConstructorInitializerSyntax MkCSharpConstructorInitializer(SyntaxKind constrInitializer,
            List<SyntaxNodeOrToken> pars)
        {
            return ConstructorInitializer(constrInitializer, ArgumentList(SeparatedList<ArgumentSyntax>(pars)));
        }
        public static ConstructorDeclarationSyntax MkCSharpConstructor(SyntaxToken name, SyntaxTokenList modifiers,
            List<SyntaxNode> constrPars, ConstructorInitializerSyntax initializer, List<StatementSyntax> body)
        {
            List<SyntaxNodeOrToken> pars = CSharpHelper.MkCSharpParameterList(constrPars);
            return ConstructorDeclaration(name)
                   .WithModifiers(modifiers)
                   .WithParameterList(ParameterList(SeparatedList<ParameterSyntax>(pars)))
                   .WithInitializer(initializer)
                   .WithBody(Block(body));
        }
        public static SyntaxNode MkCSharpClassDecl(string name, SyntaxTokenList modifiers, SeparatedSyntaxList<BaseTypeSyntax> type,
                                 SyntaxList<MemberDeclarationSyntax> members)
        {
            return ClassDeclaration(name)
                   .WithModifiers(modifiers)
                   .WithBaseList(BaseList(type))
                   .WithMembers(members)
                   .NormalizeWhitespace();
        }

        public static LabeledStatementSyntax MkCSharpEmptyLabeledStatement(string label)
        {
            return LabeledStatement(
                            Identifier(label),
                            EmptyStatement());
        }
    }
    class PToCSharpCompiler : PTranslation
    {
        public PToCSharpCompiler(Compiler compiler, AST<Model> modelWithTypes, Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo, string csharpFileName)
            : base(compiler, modelWithTypes, idToSourceInfo)
        {
            this.cSharpFileName = csharpFileName;
            this.typeContext = new TypeTranslationContext(this);
            GenerateTypeInfo(modelWithTypes);
        }

        #region CSharpCompiler
        string cSharpFileName;
        //for storing members of the Application class:
        List<SyntaxNode> members = new List<SyntaxNode>();
        //final C# program:
        SyntaxNode result = null;
        SyntaxGenerator generator;

        #region Generate P Types
        void GenerateTypeInfo(AST<Model> model)
        {
            var terms = GetBin(factBins, "TypeOf");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var typingContextAlias = Factory.Instance.ToAST(it.Current);
                    FuncTerm typingContext = aliasToTerm[typingContextAlias];
                    it.MoveNext();
                    var expr = Factory.Instance.ToAST(it.Current);
                    it.MoveNext();
                    var type = it.Current as FuncTerm;

                    string typingContextKind = ((Id)typingContext.Function).Name;
                    if (typingContextKind == "FunDecl")
                    {
                        string ownerName = GetOwnerName(typingContext, 1, 0);
                        string funName = GetName(typingContext, 0);
                        if (ownerName == null)
                        {
                            allGlobalFuns[funName].typeInfo[expr] = type;
                        }
                        else
                        {
                            allMachines[ownerName].funNameToFunInfo[funName].typeInfo[expr] = type;
                        }
                    }
                    else
                    {
                        // typingContextKind == "AnonFunDecl"
                        string ownerName = GetOwnerName(typingContext, 0, 0);
                        string funName = anonFunToName[typingContextAlias];
                        if (ownerName == null)
                        {
                            allGlobalFuns[funName].typeInfo[expr] = type;
                        }
                        else
                        {
                            allMachines[ownerName].funNameToFunInfo[funName].typeInfo[expr] = type;
                        }
                    }
                }
            }

            terms = GetBin(factBins, "ImportedType");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var typeName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    //ignore the duplicate imported types
                    if(!typeContext.importedTypes.ContainsKey(Factory.Instance.ToAST(it.Current)))
                    {
                        typeContext.importedTypes.Add(Factory.Instance.ToAST(it.Current), typeName);
                        typeContext.PTypeToCSharpExpr((FuncTerm)it.Current);
                    }
                    
                }
            }

            terms = GetBin(factBins, "ExportedType");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var typeName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    //if duplicate exported type then add it to duplicate and declare it separately in MkType()
                    if(typeContext.exportedTypes.ContainsKey(Factory.Instance.ToAST(it.Current)))
                    {
                        typeContext.duplicateExportedTypes.Add(typeName, it.Current as FuncTerm);
                    }
                    else
                    {
                        typeContext.exportedTypes.Add(Factory.Instance.ToAST(it.Current), typeName);
                        typeContext.PTypeToCSharpExpr((FuncTerm)it.Current);
                    }
                    
                }
            }

            terms = GetBin(factBins, "TranslatedTypeExpr");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var eType = (FuncTerm)it.Current;
                    typeContext.PTypeToCSharpExpr(eType);
                }
            }

            terms = GetBin(factBins, "TypeExpansion");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var type = (FuncTerm)it.Current;
                    it.MoveNext();
                    var eType = (FuncTerm)it.Current;
                    typeContext.AddOriginalType(type, eType);
                }
            }
        }

        TypeTranslationContext typeContext;

        internal class TypeTranslationContext
        {
            private int typeCount;
            //This field is for emitting types; order is important
            public List<StatementSyntax> typeInitialization;
            public List<FieldDeclarationSyntax> typeDeclaration;
            private Dictionary<AST<Node>, ExpressionSyntax> pTypeToCSharpExpr;
            public Dictionary<AST<Node>, string> exportedTypes;
            public Dictionary<string, FuncTerm> duplicateExportedTypes;
            public Dictionary<AST<Node>, string> importedTypes;

            private PToCSharpCompiler pToCSharp;

            public TypeTranslationContext(PToCSharpCompiler pToCSharp)
            {
                this.pToCSharp = pToCSharp;
                typeCount = 0;
                typeDeclaration = new List<FieldDeclarationSyntax>();
                typeInitialization = new List<StatementSyntax>();
                pTypeToCSharpExpr = new Dictionary<AST<Node>, ExpressionSyntax>();
                exportedTypes = new Dictionary<AST<Node>, string>();
                importedTypes = new Dictionary<AST<Node>, string>();
                duplicateExportedTypes = new Dictionary<string, FuncTerm>();
            }

            public ExpressionSyntax GetTypeExpr(string typeName)
            {
                var typeClass = "Types";
                var retVal = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(typeClass), IdentifierName(typeName));
                return retVal;
            }

            public string GetNextTypeName(string typeName = null)
            {
                
                typeName = typeName == null ?
                    String.Format("type_{0}_{1}", typeCount, Math.Abs(Path.GetFileNameWithoutExtension(pToCSharp.cSharpFileName).GetHashCode()).ToString())
                    : String.Format("type_{0}", typeName);
                typeCount++;
                return typeName;
            }

            public void AddTypeDeclaration(string typeName)
            {
                typeDeclaration.Add((FieldDeclarationSyntax)
                    CSharpHelper.MkCSharpFieldDeclaration(IdentifierName("PrtType"), typeName, Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)));
            }

            public void AddTypeInitialization(SyntaxNode lhs, SyntaxNode rhs)
            {
                typeInitialization.Add((StatementSyntax)(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(lhs, rhs)));
            }

            public ExpressionSyntax PTypeToCSharpExpr(FuncTerm pType)
            {
                
                var pTypeAST = Factory.Instance.ToAST(pType);
                if (!pTypeToCSharpExpr.ContainsKey(pTypeAST))
                {
                    pTypeToCSharpExpr[pTypeAST] = ConstructType(pType);
                }
                return pTypeToCSharpExpr[pTypeAST];
            }

            public void AddOriginalType(FuncTerm type, FuncTerm eType)
            {
                var typeAST = Factory.Instance.ToAST(type);
                var eTypeAST = Factory.Instance.ToAST(eType);
                if (pTypeToCSharpExpr.ContainsKey(eTypeAST) && !pTypeToCSharpExpr.ContainsKey(typeAST))
                {
                    pTypeToCSharpExpr[typeAST] = pTypeToCSharpExpr[eTypeAST];
                }
            }

            private ExpressionSyntax ConstructType(FuncTerm type)
            {
                string typeKind = ((Id)type.Function).Name;
                ExpressionSyntax typeExpr;
                string typeName;
                string originalName = "Interface";
                if (importedTypes.ContainsKey(Factory.Instance.ToAST(type)))
                {
                    originalName = importedTypes[Factory.Instance.ToAST(type)];
                    typeName = GetNextTypeName(importedTypes[Factory.Instance.ToAST(type)]);
                    return GetTypeExpr(typeName);
                }
                else
                {
                    if (exportedTypes.ContainsKey(Factory.Instance.ToAST(type)))
                    {
                        originalName = exportedTypes[Factory.Instance.ToAST(type)];
                        typeName = GetNextTypeName(exportedTypes[Factory.Instance.ToAST(type)]);
                        typeExpr = GetTypeExpr(typeName);
                    }
                    else
                    {
                        typeName = GetNextTypeName();
                        typeExpr = GetTypeExpr(typeName);
                    }

                    // add declaration and initialization
                    if (typeKind == "BaseType")
                    {
                        var primitiveType = ((Id)GetArgByIndex(type, 0)).Name;
                        if (primitiveType == "NULL")
                        {
                            AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtNullType")));
                            AddTypeDeclaration(typeName);
                        }
                        else if (primitiveType == "BOOL")
                        {
                            AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtBoolType")));
                            AddTypeDeclaration(typeName);
                        }
                        else if (primitiveType == "INT")
                        {
                            AddTypeDeclaration(typeName);
                            AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtIntType")));
                        }
                        else if (primitiveType == "EVENT")
                        {
                            AddTypeDeclaration(typeName);
                            AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtEventType")));
                        }
                        else if (primitiveType == "MACHINE")
                        {
                            AddTypeDeclaration(typeName);
                            AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtMachineType")));
                        }
                        else
                        {
                            Debug.Assert(primitiveType == "ANY", "Illegal BaseType");
                        }
                    }
                    else if (typeKind == "AnyType")
                    {
                        AddTypeDeclaration(typeName);
                        AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtAnyType")));
                    }
                    else if (typeKind == "NameType")
                    {
                        string enumTypeName = (GetArgByIndex(type, 0) as Cnst).GetStringValue();
                        List<ExpressionSyntax> args = new List<ExpressionSyntax>();
                        args.Add(CSharpHelper.MkCSharpStringLiteralExpression(enumTypeName));
                        foreach (var x in pToCSharp.allEnums[enumTypeName])
                        {
                            args.Add(CSharpHelper.MkCSharpStringLiteralExpression(x.Key));
                            args.Add(CSharpHelper.MkCSharpNumericLiteralExpression(x.Value));
                        }
                        AddTypeDeclaration(typeName);
                        AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtEnumType"), args.ToArray()));
                    }
                    else if (typeKind == "TupType")
                    {
                        List<SyntaxNode> memberTypes = new List<SyntaxNode>();
                        while (type != null)
                        {
                            memberTypes.Add(PTypeToCSharpExpr((FuncTerm)GetArgByIndex(type, 0)));
                            type = GetArgByIndex(type, 1) as FuncTerm;
                        }
                        //TODO(improve): create a generic method for inserting CommaToken into a generic list 
                        List<SyntaxNodeOrToken> initializer = new List<SyntaxNodeOrToken>();
                        foreach (var memberType in memberTypes)
                        {
                            initializer.Add(memberType);
                            initializer.Add(Token(SyntaxKind.CommaToken));
                        }
                        initializer.RemoveAt(initializer.Count() - 1);
                        AddTypeDeclaration(typeName);
                        AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtTupleType"), CSharpHelper.MkCSharpArrayCreationExpression("PrtType", initializer.ToArray())));

                    }
                    else if (typeKind == "NmdTupType")
                    {
                        List<SyntaxNode> memberNames = new List<SyntaxNode>();
                        List<SyntaxNode> memberTypes = new List<SyntaxNode>();

                        while (type != null)
                        {
                            var typeField = (FuncTerm)GetArgByIndex(type, 0);
                            string nameField = ((Cnst)GetArgByIndex(typeField, 0)).GetStringValue();
                            memberNames.Add(CSharpHelper.MkCSharpStringLiteralExpression(nameField));
                            memberTypes.Add(PTypeToCSharpExpr((FuncTerm)GetArgByIndex(typeField, 1)));
                            type = GetArgByIndex(type, 1) as FuncTerm;
                        }

                        List<SyntaxNodeOrToken> initializer = new List<SyntaxNodeOrToken>();
                        int ind = 0;
                        foreach (var memberName in memberNames)
                        {
                            initializer.Add(memberName);
                            initializer.Add(Token(SyntaxKind.CommaToken));
                            initializer.Add(memberTypes[ind++]);
                            initializer.Add(Token(SyntaxKind.CommaToken));
                        }
                        initializer.RemoveAt(initializer.Count() - 1);
                        AddTypeDeclaration(typeName);
                        AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtNamedTupleType"),
                            CSharpHelper.MkCSharpArrayCreationExpression("object", initializer.ToArray())));
                    }
                    else if (typeKind == "SeqType")
                    {
                        SyntaxNode innerType = PTypeToCSharpExpr((FuncTerm)GetArgByIndex(type, 0));
                        AddTypeDeclaration(typeName);
                        AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtSeqType"), innerType));
                    }
                    else if (typeKind == "MapType")
                    {
                        SyntaxNode keyType = PTypeToCSharpExpr((FuncTerm)GetArgByIndex(type, 0));
                        SyntaxNode valType = PTypeToCSharpExpr((FuncTerm)GetArgByIndex(type, 1));
                        AddTypeDeclaration(typeName);
                        AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtMapType"), keyType, valType));
                    }
                    else
                    {
                        // typekind == "InterfaceType"
                        var initializer = CSharpHelper.MkCSharpObjectCreationExpression(
                            IdentifierName("PrtInterfaceType"), CSharpHelper.MkCSharpStringLiteralExpression(originalName));
                        AddTypeDeclaration(typeName);
                        AddTypeInitialization(typeExpr, initializer);
                        
                    }
                }
                return typeExpr;
            }
        }

        #endregion

        public static string EventName(string rawName)
        {
            if (rawName == NullEvent)
            {
                return "@null";
            }
            else if (rawName == HaltEvent)
            {
                return rawName;
            }
            else
            {
                return String.Format("event_{0}", rawName);
            }
        }

        public static string StackFrameClassName(string rawName)
        {
            return String.Format("{0}_StackFrame", rawName);
        }

        public static string VarName(string rawName)
        {
            return String.Format("var_{0}", rawName);
        }

        public ExpressionSyntax GetEventVar(string eventName)
        {
            var eventClass = "Events";
            var retVal = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(eventClass), IdentifierName(EventName(eventName)));
            return retVal;
        }

        public void GenerateCSharp()
        {
            var workspace = new AdhocWorkspace();

            // Get the SyntaxGenerator for the specified language
            generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
            members = new List<SyntaxNode>();

            // Create using/Imports directives
            var usingDirectives = generator.NamespaceImportDeclaration("System");

            List<AST<Node>> elements = new List<AST<Node>>();
            MkEvents();
            MkTypes();
            MkStaticFunctions();
            MkCreateMachineMethods();
            MkMachineClasses();
            MkCSharpOutput();
            EmitCSharpOutput();
        }


        private void MkEvents()
        {
            List<SyntaxNode> evDeclarations = new List<SyntaxNode>();
            List<StatementSyntax> eventInitializationStmts = new List<StatementSyntax>();
            string eventsClassName = "Events";
            foreach (var evName in exportedEvents)
            {
                //add declaration
                evDeclarations.Add(
                CSharpHelper.MkCSharpFieldDeclaration(IdentifierName("PrtEventValue"),
                    EventName(evName),
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword)
                    )
                );

                //add initialization
                SyntaxNode payloadType = typeContext.PTypeToCSharpExpr(allEvents[evName].payloadType);
                SyntaxNode maxInstances;
                if (allEvents[evName].maxInstances == -1)
                {
                    String s = "PrtEvent.DefaultMaxInstances";
                    maxInstances = IdentifierName(s);
                }
                else
                {
                    maxInstances = CSharpHelper.MkCSharpNumericLiteralExpression(allEvents[evName].maxInstances);
                }

                SyntaxNode doAssume;
                if (allEvents[evName].maxInstancesAssumed)
                {
                    doAssume = CSharpHelper.MkCSharpTrueLiteralExpression();
                }
                else
                {
                    doAssume = CSharpHelper.MkCSharpFalseLiteralExpression();
                }

                SyntaxNode lhs = IdentifierName(EventName(evName));
                SyntaxNode rhs = CSharpHelper.MkCSharpObjectCreationExpression(
                    IdentifierName("PrtEventValue"),
                    CSharpHelper.MkCSharpObjectCreationExpression(
                        IdentifierName("PrtEvent"),
                        CSharpHelper.MkCSharpStringLiteralExpression(evName),
                        payloadType,
                        maxInstances,
                        doAssume
                    ));
                eventInitializationStmts.Add((StatementSyntax)CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(lhs, rhs));
            }

            var staticMethodName = "Events_" + Path.GetFileNameWithoutExtension(cSharpFileName.ToLower());
            var staticInitializer =
                    MethodDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.VoidKeyword)),
                        Identifier(staticMethodName))
                            .WithModifiers(
                                TokenList(new[] { Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)}))
                            .WithBody(
                                Block(eventInitializationStmts
                                    ))
                    .NormalizeWhitespace();

            evDeclarations.Add(staticInitializer);

            var eventsClass = generator.ClassDeclaration(
              eventsClassName, typeParameters: null,
              accessibility: Accessibility.Public,
              modifiers: DeclarationModifiers.Partial,
              members: evDeclarations);

            members.Add(eventsClass);
        }


        private void MkTypes()
        {
            //add type declaration and initialization for duplicate exported types
            foreach(var dt in typeContext.duplicateExportedTypes)
            {
                var typeName = typeContext.GetNextTypeName(dt.Key);
                var typeExpr = typeContext.GetTypeExpr(typeName);
                var declaredTypeExpr = typeContext.PTypeToCSharpExpr(dt.Value);
                typeContext.AddTypeDeclaration(typeName);
                typeContext.AddTypeInitialization(typeExpr, declaredTypeExpr);
            }

            string typesClassName = "Types";
            List<SyntaxNode> typeDeclarations = new List<SyntaxNode>();
            typeDeclarations.AddRange(typeContext.typeDeclaration);


            var staticMethodName = "Types_" + Path.GetFileNameWithoutExtension(cSharpFileName.ToLower());
            var staticInitializer =
                    MethodDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.VoidKeyword)),
                        Identifier(staticMethodName))
                            .WithModifiers(
                                TokenList(new[] { Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.PublicKeyword) }))
                            .WithBody(
                                Block(
                                    typeContext.typeInitialization
                                    ))
                    .NormalizeWhitespace();

            typeDeclarations.Add(staticInitializer);

            var typesClass = generator.ClassDeclaration(
              typesClassName, typeParameters: null,
              accessibility: Accessibility.Public,
              modifiers: DeclarationModifiers.Partial,
              members: typeDeclarations);

            members.Add(typesClass);
        }
        
        internal partial class MkFunctionDecl
        {
            public string funName;
            public FunInfo funInfo;
            public MkMachineClass owner;  // null if global function
            private PToCSharpCompiler pToCSharp;
            public Stack<bool> lhsStack;
            private int labelCount; // labels are used for "continuations" in send, new, nondet, receive, function calls

            public MkFunctionDecl(string funName, FunInfo funInfo, MkMachineClass owner, PToCSharpCompiler pToCSharp)
            {
                this.funName = funName;
                this.funInfo = funInfo;
                this.owner = owner;
                this.pToCSharp = pToCSharp;
                this.lhsStack = new Stack<bool>();
                this.labelCount = 0;
            }

            public string funClassName
            {
                get { return funName + "_Class"; }
            }

            public int GetFreshLabelId()
            {
                labelCount++;
                return labelCount;
            }

            public string GetLabelFromLabelId(int i)
            {
                return string.Format("{0}_{1}", funName, i);
            }

            public SwitchStatementSyntax EmitLabelPrelude()
            {
                SyntaxList<SwitchSectionSyntax> caseList = new SyntaxList<SwitchSectionSyntax>();
                for (int i = 1; i <= labelCount; i++)
                {
                    SyntaxList<SwitchLabelSyntax> switchLabels = new SyntaxList<SwitchLabelSyntax>();
                    switchLabels = switchLabels.Add(CaseSwitchLabel(CSharpHelper.MkCSharpNumericLiteralExpression(i)));
                    SyntaxList<StatementSyntax> switchStmts = new SyntaxList<StatementSyntax>();
                    switchStmts = switchStmts.Add(CSharpHelper.MkCSharpGoto(GetLabelFromLabelId(i)));
                    caseList = caseList.Add(SwitchSection(switchLabels, switchStmts));
                }
                return SwitchStatement(CSharpHelper.MkCSharpDot("currFun", "returnToLocation"), caseList);
            }

            private FuncTerm LookupType(Node node)
            {
                //return entityInfo.typeInfo[Factory.Instance.ToAST(node)];
                return funInfo.typeInfo[Factory.Instance.ToAST(node)];
            }
            #region FoldUnfold
            private IEnumerable<Node> Unfold(Node n)
            {
                if (n.NodeKind != NodeKind.FuncTerm)
                {
                    yield break;
                }

                var ft = (FuncTerm)n;
                var funName = ((Id)ft.Function).Name;
                if (funName == PData.Con_New.Node.Name)
                {
                    Debug.Assert(false, "New expr in ZingUnfold");
                    yield break;
                }
                else if (funName == PData.Con_Print.Node.Name)
                {
                    foreach (var a in Unfold(GetArgByIndex(ft, 2)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_Goto.Node.Name)
                {
                    foreach (var a in Unfold(GetArgByIndex(ft, 1)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_Announce.Node.Name || funName == PData.Con_Raise.Node.Name)
                {
                    yield return GetArgByIndex(ft, 0);
                    foreach (var a in Unfold(GetArgByIndex(ft, 1)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_Send.Node.Name)
                {
                    yield return GetArgByIndex(ft, 0);
                    yield return GetArgByIndex(ft, 1);
                    foreach (var a in Unfold(GetArgByIndex(ft, 2)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_Receive.Node.Name)
                {
                    yield break;
                }
                else if (funName == PData.Con_FunApp.Node.Name)
                {
                    foreach (var a in Unfold(GetArgByIndex(ft, 1)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_FunStmt.Node.Name || funName == PData.Con_NewStmt.Node.Name)
                {
                    foreach (var a in Unfold(GetArgByIndex(ft, 1)))
                    {
                        yield return a;
                    }
                    var node = GetArgByIndex(ft, 2);
                    if (node.NodeKind != NodeKind.Id)
                    {
                        yield return node;
                    }
                }
                else if (funName == PData.Con_BinApp.Node.Name)
                {
                    var opName = ((Id)GetArgByIndex(ft, 0)).Name;
                    if (opName == PData.Cnst_Idx.Node.Name && lhsStack.Count > 0 && lhsStack.Peek())
                    {
                        lhsStack.Push(true);
                        yield return GetArgByIndex(ft, 1);
                        lhsStack.Pop();
                        lhsStack.Push(false);
                        yield return GetArgByIndex(ft, 2);
                        lhsStack.Pop();
                    }
                    else
                    {
                        yield return GetArgByIndex(ft, 1);
                        yield return GetArgByIndex(ft, 2);
                    }
                }
                else if (funName == PData.Con_Name.Node.Name ||
                         funName == PData.Con_NulApp.Node.Name ||
                         funName == PData.Con_UnApp.Node.Name ||
                         funName == PData.Con_Default.Node.Name ||
                         funName == PData.Con_NulStmt.Node.Name)
                {
                    var first = true;
                    foreach (var t in ft.Args)
                    {
                        if (first)
                        {
                            first = false;
                            continue;
                        }
                        yield return t;
                    }
                }
                else if (funName == PData.Con_Assert.Node.Name)
                {
                    yield return GetArgByIndex(ft, 0);
                }
                else if (funName == PData.Con_BinStmt.Node.Name)
                {
                    yield return GetArgByIndex(ft, 3);
                    var op = ((Id)GetArgByIndex(ft, 0)).Name;
                    if (op == PData.Cnst_Assign.Node.Name)
                    {
                        var lhs = (FuncTerm)GetArgByIndex(ft, 1);
                        var lhsName = ((Id)lhs.Function).Name;
                        if (lhsName == PData.Con_BinApp.Node.Name && ((Id)GetArgByIndex(lhs, 0)).Name == PData.Cnst_Idx.Node.Name)
                        {
                            lhsStack.Push(true);
                            yield return GetArgByIndex(lhs, 1);
                            lhsStack.Pop();
                            yield return GetArgByIndex(lhs, 2);
                        }
                        else if (lhsName == PData.Con_Field.Node.Name)
                        {
                            lhsStack.Push(true);
                            yield return GetArgByIndex(lhs, 0);
                            lhsStack.Pop();
                        }
                        else
                        {
                            lhsStack.Push(true);
                            yield return lhs;
                            lhsStack.Pop();
                        }
                    }
                    else
                    {
                        lhsStack.Push(true);
                        yield return GetArgByIndex(ft, 1);
                        lhsStack.Pop();
                    }
                }
                else if (funName == PData.Con_Field.Node.Name || funName == PData.Con_Cast.Node.Name)
                {
                    yield return ft.Args.First<Node>();
                }
                else if (funName == PData.Con_Tuple.Node.Name)
                {
                    foreach (var a in Unfold(GetArgByIndex(ft, 0)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_NamedTuple.Node.Name)
                {
                    foreach (var a in Unfold(GetArgByIndex(ft, 0)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_Exprs.Node.Name)
                {
                    do
                    {
                        yield return GetArgByIndex(ft, 1);
                        ft = GetArgByIndex(ft, 2) as FuncTerm;
                    }
                    while (ft != null);
                }
                else if (funName == PData.Con_NamedExprs.Node.Name)
                {
                    do
                    {
                        yield return GetArgByIndex(ft, 1);
                        ft = GetArgByIndex(ft, 2) as FuncTerm;
                    }
                    while (ft != null);
                }
                else if (funName == PData.Con_Seq.Node.Name)
                {
                    using (var it = ft.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        yield return it.Current;
                        it.MoveNext();
                        yield return it.Current;
                    }
                }
                else if (funName == PData.Con_Ite.Node.Name)
                {
                    using (var it = ft.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        yield return it.Current;
                        it.MoveNext();
                        yield return it.Current;
                        it.MoveNext();
                        yield return it.Current;
                    }
                }
                else if (funName == PData.Con_While.Node.Name)
                {
                    using (var it = ft.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        yield return it.Current;
                        it.MoveNext();
                        yield return it.Current;
                    }
                }
                else
                {
                    foreach (var t in ft.Args)
                    {
                        yield return t;
                    }
                }
            }
            private SyntaxNode Fold(Node n, List<SyntaxNode> children)
            {
                if (n.NodeKind == NodeKind.Id || n.NodeKind == NodeKind.Cnst)
                {
                    return null;
                }

                var ft = (FuncTerm)n;
                var funName = ((Id)ft.Function).Name;

                if (funName == PData.Con_Name.Node.Name)
                {
                    return (SyntaxNode)FoldName(ft, children);
                }
                else if (funName == PData.Con_Receive.Node.Name)
                {
                    return FoldReceive(ft, children);
                }
                else if (funName == PData.Con_FunApp.Node.Name)
                {
                    return FoldFunApp(ft, children);
                }
                else if (funName == PData.Con_NulApp.Node.Name)
                {
                    return FoldNulApp(ft, children);
                }
                else if (funName == PData.Con_UnApp.Node.Name)
                {
                    return FoldUnApp(ft, children);
                }
                else if (funName == PData.Con_BinApp.Node.Name)
                {
                    return FoldBinApp(ft, children);
                }
                else if (funName == PData.Con_Field.Node.Name)
                {
                    return FoldField(ft, children);
                }
                else if (funName == PData.Con_Default.Node.Name)
                {
                    return FoldDefault(ft, children);
                }
                else if (funName == PData.Con_Cast.Node.Name)
                {
                    return FoldCast(ft, children);
                }
                else if (funName == PData.Con_Tuple.Node.Name)
                {
                    return FoldTuple(ft, children);
                }
                else if (funName == PData.Con_NamedTuple.Node.Name)
                {
                    return FoldNamedTuple(ft, children);
                }
                else if (funName == PData.Con_NewStmt.Node.Name)
                {
                    return FoldNewStmt(ft, children);
                }
                else if (funName == PData.Con_Goto.Node.Name)
                {
                    return FoldGoto(ft, children);
                }
                else if (funName == PData.Con_Raise.Node.Name)
                {
                    return FoldRaise(ft, children);
                }
                else if (funName == PData.Con_Send.Node.Name)
                {
                    return FoldSend(ft, children);
                }
                else if (funName == PData.Con_Announce.Node.Name)
                {
                    return FoldAnnounce(ft, children);
                }
                else if (funName == PData.Con_FunStmt.Node.Name)
                {
                    return FoldFunStmt(ft, children);
                }
                else if (funName == PData.Con_NulStmt.Node.Name)
                {
                    return FoldNulStmt(ft, children);
                }
                else if (funName == PData.Con_Assert.Node.Name)
                {
                    return FoldAssert(ft, children);
                }
                else if (funName == PData.Con_Print.Node.Name)
                {
                    return FoldPrint(ft, children);
                }
                else if (funName == PData.Con_BinStmt.Node.Name)
                {
                    return FoldBinStmt(ft, children);
                }
                else if (funName == PData.Con_Return.Node.Name)
                {
                    return FoldReturn(ft, children);
                }
                else if (funName == PData.Con_While.Node.Name)
                {
                    return FoldWhile(ft, children);
                }
                else if (funName == PData.Con_Ite.Node.Name)
                {
                    return FoldIte(ft, children);
                }
                else if (funName == PData.Con_Seq.Node.Name)
                {
                    return FoldSeq(ft, children);
                }
                else if (funName == PData.Con_IdList.Node.Name)
                {
                    //return ZingData.Cnst_Nil;
                    return IdentifierName("NIL");
                }
                else
                {
                    Console.WriteLine("Unknown term name: " + funName);
                    throw new NotImplementedException();
                }
            }

            private List<StatementSyntax> CaseFunCallHelper(List<string> eventNames, List<string> funNames, string afterAfterLabel)
            {
                List<StatementSyntax> eventStmts = new List<StatementSyntax>();
                List<StatementSyntax> funStmts = new List<StatementSyntax>();

                for (int i = 0; i < eventNames.Count; i++)
                {
                    var beforeLabelId = GetFreshLabelId();
                    var beforeLabel = GetLabelFromLabelId(beforeLabelId);
                    var eventName = eventNames[i];
                    var funName = funNames[i];
                    var calleeInfo = pToCSharp.allGlobalFuns.ContainsKey(funName) ? pToCSharp.allGlobalFuns[funName] : pToCSharp.allMachines[owner.machineName].funNameToFunInfo[funName];
                    Debug.Assert(calleeInfo.isAnonymous);
                    List<StatementSyntax> ifStmts = new List<StatementSyntax>();
                    ifStmts.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                        CSharpHelper.MkCSharpElementAccessExpression(CSharpHelper.MkCSharpDot("currFun", "locals"), CSharpHelper.MkCSharpNumericLiteralExpression(calleeInfo.localNameToInfo[calleeInfo.PayloadVarName].index)),
                        CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "currentPayload", "Clone"))));
                    foreach (var calleeLocal in calleeInfo.localNames)
                    {
                        var calleeLocalInfo = calleeInfo.localNameToInfo[calleeLocal];
                        ifStmts.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                            CSharpHelper.MkCSharpElementAccessExpression(CSharpHelper.MkCSharpDot("currFun", "locals"), CSharpHelper.MkCSharpNumericLiteralExpression(calleeLocalInfo.index)),
                            CSharpHelper.MkCSharpInvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("PrtValue"), IdentifierName("PrtMkDefaultValue")), pToCSharp.typeContext.PTypeToCSharpExpr(calleeLocalInfo.type))));
                    }
                    ifStmts.Add(ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(
                        CSharpHelper.MkCSharpDot("parent", "PrtPushFunStackFrame"),
                        IdentifierName(funName), CSharpHelper.MkCSharpDot("currFun", "locals"))));
                    ifStmts.Add(CSharpHelper.MkCSharpGoto(beforeLabel));
                    eventStmts.Add(IfStatement(CSharpHelper.MkCSharpEq(CSharpHelper.MkCSharpDot("parent", "currentTrigger"), pToCSharp.GetEventVar(eventName)), Block(ifStmts)));
                    funStmts.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(beforeLabel));
                    funStmts.Add(ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(funName, "Execute"), IdentifierName("application"), IdentifierName("parent"))));
                    var elseStmt = Block(ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "PrtPushFunStackFrame"), CSharpHelper.MkCSharpDot("currFun", "fun"), CSharpHelper.MkCSharpDot("currFun", "locals"), CSharpHelper.MkCSharpNumericLiteralExpression(beforeLabelId))),
                                         ReturnStatement());
                    funStmts.Add(IfStatement(
                                         CSharpHelper.MkCSharpEq(CSharpHelper.MkCSharpDot("parent", "continuation", "reason"), IdentifierName("PrtContinuationReason.Return")),
                                         CSharpHelper.MkCSharpGoto(afterAfterLabel),
                                         ElseClause(elseStmt)));
                }
                List<StatementSyntax> stmts = new List<StatementSyntax>();
                stmts.AddRange(eventStmts);
                stmts.Add(CSharpHelper.MkCSharpAssert(CSharpHelper.MkCSharpFalseLiteralExpression(), "Internal error"));
                stmts.AddRange(funStmts);
                return stmts;
            }

            SyntaxNode FoldReceive(FuncTerm ft, List<SyntaxNode> children)
            {
                List<StatementSyntax> stmts = new List<StatementSyntax>();
                List<string> eventNames = new List<string>();
                List<string> funNames = new List<string>();
                var cases = GetArgByIndex(ft, 0) as FuncTerm;
                while (cases != null)
                {
                    Node evt = GetArgByIndex(cases, 0);
                    string eventName = null;
                    if (evt is Cnst)
                    {
                        eventName = (evt as Cnst).GetStringValue();
                    }
                    else if ((evt as Id).Name == "NULL")
                    {
                        eventName = NullEvent;
                    }
                    else
                    {
                        eventName = HaltEvent;
                    }
                    eventNames.Add(eventName);
                    stmts.Add(ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(
                        CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtImplMachine", IdentifierName("parent")), "receiveSet", "Add"), 
                        pToCSharp.GetEventVar(eventName))));
                    var fun = GetArgByIndex(cases, 1);
                    string funName = pToCSharp.anonFunToName[Factory.Instance.ToAST(fun)];
                    funNames.Add(funName);
                    cases = GetArgByIndex(cases, 2) as FuncTerm;
                }
                var afterLabelId = GetFreshLabelId();
                var afterLabel = GetLabelFromLabelId(afterLabelId);
                stmts.Add(ExpressionStatement(
                    CSharpHelper.MkCSharpInvocationExpression(
                        CSharpHelper.MkCSharpDot("parent", "PrtFunContReceive"), 
                        ThisExpression(), 
                        CSharpHelper.MkCSharpDot("currFun", "locals"), 
                        CSharpHelper.MkCSharpNumericLiteralExpression(afterLabelId))));
                stmts.Add(ReturnStatement());
                stmts.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(afterLabel));
                var afterAfterLabelId = GetFreshLabelId();
                var afterAfterLabel = GetLabelFromLabelId(afterAfterLabelId);
                stmts.AddRange(CaseFunCallHelper(eventNames, funNames, afterAfterLabel));
                stmts.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(afterAfterLabel));
                return Block(stmts);
            }

            SyntaxNode FoldName(FuncTerm ft, List<SyntaxNode> children)
            {
                var name = GetName(ft, 0);
                if (funInfo != null && funInfo.localNameToInfo.ContainsKey(name))
                {
                    //local var of a function:
                    return CSharpHelper.MkCSharpDot("currFun", VarName(name));
                }
                else if (owner != null && pToCSharp.allMachines[owner.machineName].localVariableToVarInfo.ContainsKey(name))
                {
                    return CSharpHelper.MkCSharpDot("parent", VarName(name));
                }
                else
                {
                    var type = LookupType(ft);
                    if (PTypeEvent.Equals(Factory.Instance.ToAST(type)))
                    {
                        return pToCSharp.GetEventVar(name);
                    }
                    else
                    {
                        // enum constant
                        var enumTypeName = (GetArgByIndex(type, 0) as Cnst).GetStringValue();
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                            IdentifierName("PrtEnumValue"),
                            CSharpHelper.MkCSharpStringLiteralExpression(name),
                            CSharpHelper.MkCSharpNumericLiteralExpression(pToCSharp.allEnums[enumTypeName][name]));
                    }
                }
            }

            SyntaxNode FoldNewStmt(FuncTerm ft, List<SyntaxNode> children)
            {
                SyntaxNode aout = null;
                if (GetArgByIndex(ft, 2).NodeKind != NodeKind.Id)
                {
                    aout = children.Last();
                    children.RemoveAt(children.Count - 1);
                }
                var createdIorM = GetName(ft, 0);
                var payloadVar = MkPayload(children);
                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                if (aout != null)
                {
                    stmtList.Add(
                        CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(aout,
                        CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("application", "CreateInterfaceOrMachine"), CSharpHelper.MkCSharpDot("parent", "renamedName"), CSharpHelper.MkCSharpStringLiteralExpression(createdIorM), payloadVar)));
                }
                else
                {
                    stmtList.Add(
                        ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("application", "CreateInterfaceOrMachine"), CSharpHelper.MkCSharpDot("parent", "renamedName"), CSharpHelper.MkCSharpStringLiteralExpression(createdIorM), payloadVar))
                        );
                }
                int afterLabelId = GetFreshLabelId();
                string afterLabel = GetLabelFromLabelId(afterLabelId);
                stmtList.Add(ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "PrtFunContNewMachine"), ThisExpression(), CSharpHelper.MkCSharpDot("currFun", "locals"), CSharpHelper.MkCSharpNumericLiteralExpression(afterLabelId))));
                stmtList.Add(ReturnStatement());
                stmtList.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(afterLabel));
                return Block(stmtList);
            }

            SyntaxNode FoldFunApp(FuncTerm ft, List<SyntaxNode> children)
            {
                string calleeName = (GetArgByIndex(ft, 0) as Cnst).GetStringValue();
                var paramList = new List<ExpressionSyntax>();
                paramList.Add(IdentifierName("application"));
                paramList.Add(IdentifierName("parent"));
                children.ForEach(x => paramList.Add((ExpressionSyntax)x));
                return CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(calleeName, "ExecuteToCompletion"), paramList.ToArray());
            }

            SyntaxNode FoldNulApp(FuncTerm ft, List<SyntaxNode> children)
            {
                //No children
                var n = GetArgByIndex(ft, 0);

                if (n.NodeKind == NodeKind.Cnst)
                {
                    int val = (int)((Cnst)n).GetNumericValue().Numerator;
                    return CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtIntValue"),
                            CSharpHelper.MkCSharpNumericLiteralExpression(val));
                }
                // n.NodeKind == NodeKind.Id
                var op = ((Id)n).Name;
                if (op == PData.Cnst_True.Node.Name)
                {
                    return CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtBoolValue"),
                            CSharpHelper.MkCSharpTrueLiteralExpression());
                }
                else if (op == PData.Cnst_False.Node.Name)
                {
                    return CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtBoolValue"),
                            CSharpHelper.MkCSharpFalseLiteralExpression());
                }
                else if (op == PData.Cnst_This.Node.Name)
                {
                    return MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("parent"), IdentifierName("self"));
                }
                else if (op == PData.Cnst_Nondet.Node.Name || op == PData.Cnst_FairNondet.Node.Name)
                {
                    return CSharpHelper.MkCSharpObjectCreationExpression(
                        IdentifierName("PrtBoolValue"), 
                        CSharpHelper.MkCSharpInvocationExpression(
                            CSharpHelper.MkCSharpDot("application", "GetSelectedChoiceValue"),
                            CSharpHelper.MkCSharpCastExpression("PrtImplMachine", IdentifierName("parent"))));
                }
                else if (op == PData.Cnst_Null.Node.Name)
                {
                    return pToCSharp.GetEventVar(NullEvent);
                }
                else
                {
                    //op == PData.Cnst_Halt.Node.Name
                    return pToCSharp.GetEventVar(HaltEvent);
                }
            }

            SyntaxNode FoldUnApp(FuncTerm ft, List<SyntaxNode> children)
            {
                var op = ((Id)GetArgByIndex(ft, 0)).Name;
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var arg = it.Current;
                    if (op == PData.Cnst_Not.Node.Name)
                    {
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                            IdentifierName("PrtBoolValue"),
                            PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtBoolValue", arg), "bl")));
                    }
                    else if (op == PData.Cnst_Neg.Node.Name)
                    {
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                            IdentifierName("PrtIntValue"),
                            PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg), "nt")));
                    }
                    else if (op == PData.Cnst_Keys.Node.Name)
                    {
                        return CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtMapValue", arg), "Keys"));
                    }
                    else if (op == PData.Cnst_Values.Node.Name)
                    {
                        return CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtMapValue", arg), "Values"));
                    }
                    else
                    {
                        //  op == PData.Cnst_Sizeof.Node.Name
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                                        IdentifierName("PrtIntValue"),
                                        CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot((ExpressionSyntax)arg, "Size")));
                    }
                }
            }

            SyntaxNode FoldBinApp(FuncTerm ft, List<SyntaxNode> children)
            {
                var op = ((Id)GetArgByIndex(ft, 0)).Name;
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var arg1 = (ExpressionSyntax)it.Current;
                    it.MoveNext();
                    var arg2 = (ExpressionSyntax)it.Current;

                    if (op == PData.Cnst_Add.Node.Name)
                    {
                        var arg1Int = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                        var arg2Int = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                                IdentifierName("PrtIntValue"),
                                BinaryExpression(SyntaxKind.AddExpression, arg1Int, arg2Int));
                    }
                    else if (op == PData.Cnst_Sub.Node.Name)
                    {
                        var arg1Int = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                        var arg2Int = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                                IdentifierName("PrtIntValue"),
                                BinaryExpression(SyntaxKind.SubtractExpression, arg1Int, arg2Int));
                    }
                    else if (op == PData.Cnst_Mul.Node.Name)
                    {
                        var arg1Int = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                        var arg2Int = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                                IdentifierName("PrtIntValue"),
                                BinaryExpression(SyntaxKind.MultiplyExpression, arg1Int, arg2Int));
                    }
                    else if (op == PData.Cnst_IntDiv.Node.Name)
                    {
                        var arg1Int = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                        var arg2Int = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                                IdentifierName("PrtIntValue"),
                                BinaryExpression(SyntaxKind.DivideExpression, arg1Int, arg2Int));
                    }
                    else if (op == PData.Cnst_And.Node.Name)
                    {
                        var arg1Bool = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtBoolValue", arg1), "bl");
                        var arg2Bool = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtBoolValue", arg2), "bl");
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                                IdentifierName("PrtBoolValue"),
                                BinaryExpression(SyntaxKind.LogicalAndExpression, arg1Bool, arg2Bool));
                    }
                    else if (op == PData.Cnst_Or.Node.Name)
                    {
                        var arg1Bool = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtBoolValue", arg1), "bl");
                        var arg2Bool = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtBoolValue", arg2), "bl");
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                                IdentifierName("PrtBoolValue"),
                                BinaryExpression(SyntaxKind.LogicalOrExpression, arg1Bool, arg2Bool));
                    }
                    else if (op == PData.Cnst_Eq.Node.Name)
                    {
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                                IdentifierName("PrtBoolValue"),
                                CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(arg1, "Equals"), arg2));
                    }
                    else if (op == PData.Cnst_NEq.Node.Name)
                    {
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                                IdentifierName("PrtBoolValue"),
                                PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(arg1, "Equals"), arg2)));
                    }
                    else if (op == PData.Cnst_Lt.Node.Name)
                    {
                        var arg1Int = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                        var arg2Int = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                                IdentifierName("PrtBoolValue"),
                                BinaryExpression(SyntaxKind.LessThanExpression, arg1Int, arg2Int));
                    }
                    else if (op == PData.Cnst_Le.Node.Name)
                    {
                        var arg1Int = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                        var arg2Int = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                                IdentifierName("PrtBoolValue"),
                                BinaryExpression(SyntaxKind.LessThanOrEqualExpression, arg1Int, arg2Int));
                    }
                    else if (op == PData.Cnst_Gt.Node.Name)
                    {
                        var arg1Int = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                        var arg2Int = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                                IdentifierName("PrtBoolValue"),
                                BinaryExpression(SyntaxKind.GreaterThanExpression, arg1Int, arg2Int));
                    }
                    else if (op == PData.Cnst_Ge.Node.Name)
                    {
                        var arg1Int = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                        var arg2Int = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                        return CSharpHelper.MkCSharpObjectCreationExpression(
                                IdentifierName("PrtBoolValue"),
                                BinaryExpression(SyntaxKind.GreaterThanOrEqualExpression, arg1Int, arg2Int));
                    }
                    else if (op == PData.Cnst_Idx.Node.Name)
                    {
                        var type = LookupType(GetArgByIndex(ft, 1));
                        var typeOp = ((Id)type.Function).Name;
                        if (typeOp == PData.Con_SeqType.Node.Name)
                        {
                            arg1 = CSharpHelper.MkCSharpCastExpression("PrtSeqValue", arg1);
                        }
                        else
                        {
                            // op == PData.Con_MapType.Node.Name
                            arg1 = CSharpHelper.MkCSharpCastExpression("PrtMapValue", arg1);
                        }
                        var lookupExpr = CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(arg1, "Lookup"), arg2);
                        if (lhsStack.Count > 0 && lhsStack.Peek())
                        {
                            return lookupExpr;
                        }
                        else
                        {
                            return CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(lookupExpr, "Clone"));
                        }

                    }
                    else
                    {
                        // op == PData.Cnst_In.Node.Name
                        return CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtBoolValue"),
                                                                CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtMapValue", arg2), "Contains"), arg1));
                    }
                }
            }
            private int GetFieldIndex(string fieldName, FuncTerm nmdTupType)
            {
                int fieldIndex = 0;
                while (nmdTupType != null)
                {
                    var fieldInfo = (FuncTerm)GetArgByIndex(nmdTupType, 0);
                    var fieldNameInFieldInfo = (Cnst)GetArgByIndex(fieldInfo, 0);
                    if (fieldName == fieldNameInFieldInfo.GetStringValue())
                        return fieldIndex;
                    nmdTupType = GetArgByIndex(nmdTupType, 1) as FuncTerm;
                    fieldIndex++;
                }
                Debug.Assert(false);
                return 0;
            }

            SyntaxNode FoldField(FuncTerm ft, List<SyntaxNode> children)
            {
                var expr = GetArgByIndex(ft, 0);
                var field = (Cnst)GetArgByIndex(ft, 1);
                int fieldIndex;
                if (field.CnstKind == CnstKind.Numeric)
                {
                    fieldIndex = (int)field.GetNumericValue().Numerator;
                }
                else
                {
                    fieldIndex = GetFieldIndex(field.GetStringValue(), LookupType(expr));
                }
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var arg = (ExpressionSyntax)it.Current;
                    var accessExpr = CSharpHelper.MkCSharpElementAccessExpression(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtTupleValue", arg), "fieldValues"), fieldIndex);
                    if (lhsStack.Count > 0 && lhsStack.Peek())
                    {
                        return accessExpr;
                    }
                    else
                    {
                        return CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(accessExpr, "Clone"));
                    }
                }
            }

            SyntaxNode FoldDefault(FuncTerm ft, List<SyntaxNode> children)
            {
                var typeArg = (FuncTerm)GetArgByIndex(ft, 0);
                return CSharpHelper.MkCSharpInvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("PrtValue"),
                                IdentifierName("PrtMkDefaultValue")),
                            pToCSharp.typeContext.PTypeToCSharpExpr(typeArg));
            }

            SyntaxNode FoldCast(FuncTerm ft, List<SyntaxNode> children)
            {
                var typeArg = (FuncTerm)GetArgByIndex(ft, 1);
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var valueArg = it.Current;
                    return CSharpHelper.MkCSharpInvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("PrtValue"),
                                    IdentifierName("PrtCastValue")),
                                (ExpressionSyntax)valueArg,
                                pToCSharp.typeContext.PTypeToCSharpExpr(typeArg));
                }
            }

            SyntaxNode FoldTuple(FuncTerm ft, List<SyntaxNode> children)
            {
                return CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtTupleValue"), children.ToArray());
            }

            SyntaxNode FoldNamedTuple(FuncTerm ft, List<SyntaxNode> children)
            {
                var tupType = LookupType(ft);
                children.Insert(0, pToCSharp.typeContext.PTypeToCSharpExpr(tupType));
                return CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtNamedTupleValue"), children.ToArray());
            }

            private ExpressionSyntax MkPayload(List<SyntaxNode> args)
            {
                if (args.Count == 0)
                {
                    return pToCSharp.GetEventVar(NullEvent);
                }
                else if (args.Count == 1)
                {
                    return (ExpressionSyntax)args[0];
                }
                else
                {
                    return CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtTupleValue"), args.ToArray());
                }
            }

            SyntaxNode FoldGoto(FuncTerm ft, List<SyntaxNode> children)
            {
                var qualifiedStateName = (FuncTerm)GetArgByIndex(ft, 0);
                var stateName = GetNameFromQualifiedName(owner.machineName, qualifiedStateName);
                var stateExpr = IdentifierName(stateName);
                MachineInfo machineInfo = pToCSharp.allMachines[owner.machineName];
                string stateEntryActionName = machineInfo.stateNameToStateInfo[stateName].entryActionName;
                FunInfo entryFunInfo = pToCSharp.allGlobalFuns.ContainsKey(stateEntryActionName)
                                        ? pToCSharp.allGlobalFuns[stateEntryActionName]
                                        : machineInfo.funNameToFunInfo[stateEntryActionName];
                var payloadVar = MkPayload(children);
                var traceStmt = CSharpHelper.MkCSharpTrace(string.Format("<GotoLog> Machine {{0}}-{{1}} goes to {{2}}"), 
                    CSharpHelper.MkCSharpDot("parent", "Name"), 
                    CSharpHelper.MkCSharpDot("parent", "instanceNumber"), 
                    CSharpHelper.MkCSharpDot(stateExpr, "name"));
                var assignStmt1 = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot("parent", "currentTrigger"), pToCSharp.GetEventVar(NullEvent));
                var assignStmt2 = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot("parent", "currentPayload"), payloadVar);
                var assignStmt3 = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot("parent", "destOfGoto"), stateExpr);
                var createRetCtxt = ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "PrtFunContGoto")));
                return Block(traceStmt, assignStmt1, assignStmt2, assignStmt3, createRetCtxt, ReturnStatement());
            }

            SyntaxNode FoldRaise(FuncTerm ft, List<SyntaxNode> children)
            {
                var eventExpr = (ExpressionSyntax)children[0];
                children.RemoveAt(0);
                var payloadVar = MkPayload(children);
                var equalsExpr = CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(eventExpr, "Equals"), pToCSharp.GetEventVar(NullEvent));
                var assertStmt = CSharpHelper.MkCSharpAssert(CSharpHelper.MkCSharpNot(equalsExpr), pToCSharp.SpanToString(pToCSharp.LookupSpan(ft), "Raised event must be non-null"));
                var traceStmt = CSharpHelper.MkCSharpTrace(string.Format("<RaiseLog> Machine {{0}}-{{1}} raised Event {{2}}"), 
                    CSharpHelper.MkCSharpDot("parent", "Name"), 
                    CSharpHelper.MkCSharpDot("parent", "instanceNumber"), 
                    CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtEventValue", eventExpr), "evt", "name"));
                var assignStmt1 = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot("parent", "currentTrigger"), eventExpr);
                var assignStmt2 = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot("parent", "currentPayload"), payloadVar);
                var returnStmt = ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "PrtFunContRaise")));
                return Block(assertStmt, traceStmt, assignStmt1, assignStmt2, returnStmt, ReturnStatement());
            }

            SyntaxNode FoldSend(FuncTerm ft, List<SyntaxNode> args)
            {
                var targetExpr = CSharpHelper.MkCSharpCastExpression("PrtMachineValue", args[0]);
                ExpressionSyntax eventExpr = CSharpHelper.MkCSharpCastExpression("PrtEventValue", args[1]);
                args.RemoveRange(0, 2);
                ExpressionSyntax payloadExpr = MkPayload(args);
                StatementSyntax enqueueEventStmt = ExpressionStatement(
                    CSharpHelper.MkCSharpInvocationExpression(
                        CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpDot(targetExpr, "mach"), "PrtEnqueueEvent"),
                        eventExpr, payloadExpr, IdentifierName("parent"), targetExpr));
                var afterLabelId = GetFreshLabelId();
                var afterLabel = GetLabelFromLabelId(afterLabelId);
                StatementSyntax contStmt = ExpressionStatement(
                    CSharpHelper.MkCSharpInvocationExpression(
                        CSharpHelper.MkCSharpDot("parent", "PrtFunContSend"), 
                        ThisExpression(), CSharpHelper.MkCSharpDot("currFun", "locals"), CSharpHelper.MkCSharpNumericLiteralExpression(afterLabelId)));
                StatementSyntax afterStmt = CSharpHelper.MkCSharpEmptyLabeledStatement(afterLabel);
                return Block(enqueueEventStmt, contStmt, ReturnStatement(), afterStmt);
            }

            SyntaxNode FoldAnnounce(FuncTerm ft, List<SyntaxNode> args)
            {
                ExpressionSyntax eventExpr = (ExpressionSyntax)CSharpHelper.MkCSharpCastExpression("PrtEventValue", args[0]);
                args.RemoveAt(0);
                ExpressionSyntax payloadExpr = (ExpressionSyntax)MkPayload(args);
                var invocationArgs = new ExpressionSyntax[]
                {
                    eventExpr, payloadExpr, (ExpressionSyntax)IdentifierName("parent")
                };
                StatementSyntax announceEventStmt = ExpressionStatement(
                    CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("application", "Announce"), invocationArgs));
                return announceEventStmt;
            }

            SyntaxNode FoldAssert(FuncTerm ft, List<SyntaxNode> children)
            {
                Cnst msgCnst = GetArgByIndex(ft, 1) as Cnst;
                using (var it = children.GetEnumerator())
                {
                    string errorMsg;
                    it.MoveNext();
                    if (msgCnst != null)
                    {
                        errorMsg = pToCSharp.SpanToString(pToCSharp.LookupSpan(ft), msgCnst.GetStringValue());
                    }
                    else
                    {
                        errorMsg = pToCSharp.SpanToString(pToCSharp.LookupSpan(ft), "Assert failed");
                    }
                    return CSharpHelper.MkCSharpAssert(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtBoolValue",it.Current), "bl"), errorMsg);
                }
            }

            SyntaxNode FoldFunStmt(FuncTerm ft, List<SyntaxNode> children)
            {
                List<bool> isSwapParameter = new List<bool>();
                var exprs = GetArgByIndex(ft, 1) as FuncTerm;
                while (exprs != null)
                {
                    var qualifier = GetArgByIndex(exprs, 0) as Id;
                    isSwapParameter.Add(qualifier.Name == "SWAP");
                    exprs = GetArgByIndex(exprs, 2) as FuncTerm;
                }

                var calleeName = GetName(ft, 0);
                var calleeInfo = pToCSharp.allGlobalFuns.ContainsKey(calleeName) ? pToCSharp.allGlobalFuns[calleeName] : pToCSharp.allMachines[owner.machineName].funNameToFunInfo[calleeName];

                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                List<ExpressionSyntax> paramList = new List<ExpressionSyntax>();
                int parameterCount = 0;
                List<StatementSyntax> processOutput = new List<StatementSyntax>();
                foreach (var child in children)
                {
                    if (parameterCount == calleeInfo.parameterNames.Count)
                    {
                        // output variable
                        processOutput.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(child, CSharpHelper.MkCSharpDot("parent", "continuation", "retVal")));
                        break;
                    }
                    var calleeArg = calleeInfo.parameterNames[parameterCount];
                    var calleeArgInfo = calleeInfo.localNameToInfo[calleeArg];
                    paramList.Add((ExpressionSyntax)child);
                    if (isSwapParameter[parameterCount])
                    {
                        processOutput.Add(
                            CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                                child,
                                CSharpHelper.MkCSharpElementAccessExpression(CSharpHelper.MkCSharpDot("parent", "continuation", "retLocals"), CSharpHelper.MkCSharpNumericLiteralExpression(calleeArgInfo.index))));
                    }
                    parameterCount++;
                }

                var beforeLabelId = GetFreshLabelId();
                var beforeLabel = GetLabelFromLabelId(beforeLabelId);
                stmtList.Add(ExpressionStatement(
                    CSharpHelper.MkCSharpInvocationExpression(
                        CSharpHelper.MkCSharpDot("parent", "PrtPushFunStackFrame"), 
                        IdentifierName(calleeName), 
                        CSharpHelper.MkCSharpInvocationExpression(
                            CSharpHelper.MkCSharpDot(calleeName, "CreateLocals"), paramList.ToArray()))));
                stmtList.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(beforeLabel));
                stmtList.Add(ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(calleeName, "Execute"), IdentifierName("application"), IdentifierName("parent"))));
                var elseStmt = Block(ExpressionStatement(
                    CSharpHelper.MkCSharpInvocationExpression(
                        CSharpHelper.MkCSharpDot("parent", "PrtPushFunStackFrame"), CSharpHelper.MkCSharpDot("currFun", "fun"), CSharpHelper.MkCSharpDot("currFun", "locals"), CSharpHelper.MkCSharpNumericLiteralExpression(beforeLabelId))),
                                     ReturnStatement());
                stmtList.Add(IfStatement(
                                     CSharpHelper.MkCSharpEq(CSharpHelper.MkCSharpDot("parent", "continuation", "reason"), IdentifierName("PrtContinuationReason.Return")),
                                     Block(processOutput),
                                     ElseClause(elseStmt)));
                return Block(stmtList);
            }

            SyntaxNode FoldNulStmt(FuncTerm ft, List<SyntaxNode> children)
            {
                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                var op = ((Id)GetArgByIndex(ft, 0)).Name;
                if (op == PData.Cnst_Pop.Node.Name)
                {
                    stmtList.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot("parent", "currentTrigger"), pToCSharp.GetEventVar(NullEvent)));
                    stmtList.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot("parent", "currentPayload"), pToCSharp.GetEventVar(NullEvent)));
                    stmtList.Add(ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "PrtFunContPop"))));
                    stmtList.Add(ReturnStatement());
                }
                return Block(stmtList);
            }

            SyntaxNode FoldPrint(FuncTerm ft, List<SyntaxNode> children)
            {
                string msg = (GetArgByIndex(ft, 0) as Cnst).GetStringValue();
                List<StatementSyntax> stmts = new List<StatementSyntax>();
                stmts.Add(CSharpHelper.MkCSharpPrint(msg));
                FuncTerm seg = GetArgByIndex(ft, 1) as FuncTerm;
                while (seg != null)
                {
                    int formatArg = (int)(GetArgByIndex(seg, 0) as Cnst).GetNumericValue().Numerator;
                    string str = (GetArgByIndex(seg, 1) as Cnst).GetStringValue();
                    seg = GetArgByIndex(seg, 2) as FuncTerm;
                    stmts.Add(ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot((ExpressionSyntax)children[formatArg], "ToString"))));
                    stmts.Add(CSharpHelper.MkCSharpPrint(str));
                }
                return Block(stmts);
            }

            SyntaxNode FoldBinStmt(FuncTerm ft, List<SyntaxNode> children)
            {
                var op = ((Id)GetArgByIndex(ft, 0)).Name;
                var lhs = (FuncTerm)GetArgByIndex(ft, 1);
                var type = LookupType(lhs);
                var typeName = ((Id)type.Function).Name;
                ExpressionSyntax src = null, dest = null;
                using (var it = children.GetEnumerator())
                {
                    ExpressionSyntax index = null;
                    it.MoveNext();
                    src = (ExpressionSyntax)it.Current;
                    it.MoveNext();
                    dest = (ExpressionSyntax)it.Current;
                    if (it.MoveNext())
                    {
                        index = (ExpressionSyntax)it.Current;
                    }

                    if (op == PData.Cnst_Assign.Node.Name)
                    {
                        string assignType = (GetArgByIndex(ft, 2) as Id).Name;
                        if (((Id)lhs.Function).Name == PData.Con_Field.Node.Name)
                        {
                            var field = (Cnst)GetArgByIndex(lhs, 1);
                            int fieldIndex;
                            if (field.CnstKind == CnstKind.Numeric)
                            {
                                fieldIndex = (int)field.GetNumericValue().Numerator;
                            }
                            else
                            {
                                fieldIndex = GetFieldIndex(field.GetStringValue(), LookupType(GetArgByIndex(lhs, 0)));
                            }
                            if (assignType == "NONE")
                            {
                                return ExpressionStatement(
                                            CSharpHelper.MkCSharpInvocationExpression(
                                                CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtTupleValue", dest), "Update"),
                                                CSharpHelper.MkCSharpNumericLiteralExpression(fieldIndex),
                                                CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(src, "Clone"))));
                            }
                            else if (assignType == "MOVE")
                            {
                                return ExpressionStatement(
                                            CSharpHelper.MkCSharpInvocationExpression(
                                                CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtTupleValue", dest), "Update"),
                                                CSharpHelper.MkCSharpNumericLiteralExpression(fieldIndex),
                                                src));
                            }
                            else
                            {
                                // assignType = "SWAP" 
                                return CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                                            src,
                                            CSharpHelper.MkCSharpInvocationExpression(
                                                CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtTupleValue", dest), "UpdateAndReturnOldValue"),
                                                CSharpHelper.MkCSharpNumericLiteralExpression(fieldIndex),
                                                src));
                            }
                        }
                        else if (index == null)
                        {
                            if (assignType == "NONE")
                            {
                                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                                src = (ExpressionSyntax)TranslatePossibleNondet(src, stmtList);
                                stmtList.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(dest, CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(src, "Clone"))));
                                return Block(stmtList);
                            }
                            else if (assignType == "MOVE")
                            {
                                return CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(dest, src);
                            }
                            else
                            {
                                // assignType == "SWAP"
                                return Block(
                                    CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(IdentifierName("swap"), dest),
                                    CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(dest, src),
                                    CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(src, IdentifierName("swap")));
                            }
                        }
                        else
                        {
                            lhs = (FuncTerm)GetArgByIndex(lhs, 1);
                            type = LookupType(lhs);
                            typeName = ((Id)type.Function).Name;
                            if (typeName == PData.Con_SeqType.Node.Name)
                            {
                                if (assignType == "NONE")
                                {
                                    return ExpressionStatement(
                                                CSharpHelper.MkCSharpInvocationExpression(
                                                    CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtSeqValue", dest), "Update"),
                                                    index,
                                                    CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(src, "Clone"))));
                                }
                                else if (assignType == "MOVE")
                                {
                                    return ExpressionStatement(
                                                CSharpHelper.MkCSharpInvocationExpression(
                                                    CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtSeqValue", dest), "Update"),
                                                    index,
                                                    src));
                                }
                                else
                                {
                                    // assignType == "SWAP"
                                    return CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                                                src,
                                                CSharpHelper.MkCSharpInvocationExpression(
                                                    CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtSeqValue", dest), "UpdateAndReturnOldValue"),
                                                    index,
                                                    src));
                                }
                            }
                            else
                            {
                                // type is PMapType
                                if (assignType == "NONE")
                                {
                                    return ExpressionStatement(
                                                CSharpHelper.MkCSharpInvocationExpression(
                                                    CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtMapValue", dest), "Update"),
                                                    index,
                                                    CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(src, "Clone"))));
                                }
                                else if (assignType == "MOVE")
                                {
                                    return ExpressionStatement(
                                                CSharpHelper.MkCSharpInvocationExpression(
                                                    CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtMapValue", dest), "Update"),
                                                    index,
                                                    src));
                                }
                                else
                                {
                                    // assignType == "SWAP"
                                    return CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                                                src,
                                                CSharpHelper.MkCSharpInvocationExpression(
                                                    CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtMapValue", dest), "UpdateAndReturnOldValue"),
                                                    index,
                                                    src));
                                }
                            }
                        }
                    }
                    else if (op == PData.Cnst_Remove.Node.Name)
                    {
                        if (typeName == PData.Con_SeqType.Node.Name)
                        {
                            return ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtSeqValue", dest), "Remove"), src));
                        }
                        else
                        {
                            return ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtMapValue", dest), "Remove"), src));
                        }
                    }
                    else
                    {
                        // op == PData.Cnst_Insert.Node.Name
                        return ExpressionStatement(
                                    CSharpHelper.MkCSharpInvocationExpression(
                                        CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtSeqValue", dest), "Insert"),
                                        CSharpHelper.MkCSharpElementAccessExpression(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtTupleValue", src), "fieldValues"), 0),
                                        CSharpHelper.MkCSharpElementAccessExpression(CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtTupleValue", src), "fieldValues"), 1)));
                    }
                }
            }

            SyntaxNode FoldReturn(FuncTerm ft, List<SyntaxNode> children)
            {
                AST<FuncTerm> returnType = PTypeNull;
                if (funInfo != null)
                {
                    returnType = funInfo.returnType;
                }
                using (var it = children.GetEnumerator())
                {
                    List<StatementSyntax> stmtList = new List<StatementSyntax>();
                    it.MoveNext();
                    if (returnType.Equals(PTypeNull))
                    {
                        stmtList.Add(ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "PrtFunContReturn"), CSharpHelper.MkCSharpDot("currFun", "locals"))));
                    }
                    else
                    {
                        var returnExpr = (ExpressionSyntax)TranslatePossibleNondet(it.Current, stmtList);
                        stmtList.Add(ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "PrtFunContReturnVal"), returnExpr, CSharpHelper.MkCSharpDot("currFun", "locals"))));
                    }
                    stmtList.Add(ReturnStatement());
                    return Block(stmtList);
                }
            }

            SyntaxNode FoldWhile(FuncTerm ft, List<SyntaxNode> children)
            {
                using (var it = children.GetEnumerator())
                {
                    List<StatementSyntax> stmtList = new List<StatementSyntax>();
                    it.MoveNext();
                    var condExpr = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtBoolValue", TranslatePossibleNondet(it.Current, stmtList)), "bl");
                    it.MoveNext();
                    var loopStart = pToCSharp.GetUnique(funName + "_loop_start");
                    var loopEnd = pToCSharp.GetUnique(funName + "_loop_end");
                    var body = it.Current;
                    stmtList.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(loopStart));
                    stmtList.Add(IfStatement(PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, condExpr), CSharpHelper.MkCSharpGoto(loopEnd)));
                    stmtList.Add((StatementSyntax)body);
                    stmtList.Add(CSharpHelper.MkCSharpGoto(loopStart));
                    stmtList.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(loopEnd));
                    return Block(stmtList);
                }
            }

            SyntaxNode FoldIte(FuncTerm ft, List<SyntaxNode> children)
            {
                using (var it = children.GetEnumerator())
                {
                    List<StatementSyntax> stmtList = new List<StatementSyntax>();
                    it.MoveNext();
                    var condExpr = CSharpHelper.MkCSharpDot(CSharpHelper.MkCSharpCastExpression("PrtBoolValue", TranslatePossibleNondet(it.Current, stmtList)), "bl");
                    it.MoveNext();
                    var thenStmt = it.Current;
                    it.MoveNext();
                    var elseStmt = it.Current;

                    var ifName = pToCSharp.GetUnique(funName + "_if");
                    var elseLabel = ifName + "_else";
                    var afterLabel = ifName + "_end";
                    stmtList.Add(IfStatement(PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, condExpr), CSharpHelper.MkCSharpGoto(elseLabel)));
                    stmtList.Add((StatementSyntax)thenStmt);
                    stmtList.Add(CSharpHelper.MkCSharpGoto(afterLabel));
                    stmtList.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(elseLabel));
                    stmtList.Add((StatementSyntax)elseStmt);
                    stmtList.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(afterLabel));
                    return Block(stmtList);
                }
            }

            SyntaxNode FoldSeq(FuncTerm ft, List<SyntaxNode> children)
            {
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var first = it.Current;
                    it.MoveNext();
                    var second = it.Current;
                    return Block((StatementSyntax)first, (StatementSyntax)second);
                }
            }

            SyntaxNode TranslatePossibleNondet(SyntaxNode expr, List<StatementSyntax> stmtList)
            {
                var id = expr as IdentifierNameSyntax;
                if (id == null) return expr;
                var name = id.Identifier.ToString();
                if (name != "$" && name != "$$")
                {
                    return expr;
                }
                var afterLabelId = GetFreshLabelId();
                var afterLabel = GetLabelFromLabelId(afterLabelId);
                stmtList.Add(ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "PrtFunContNondet"), ThisExpression(), CSharpHelper.MkCSharpDot("currFun", "locals"), CSharpHelper.MkCSharpNumericLiteralExpression(afterLabelId))));
                stmtList.Add(ReturnStatement());
                stmtList.Add(CSharpHelper.MkCSharpEmptyLabeledStatement(afterLabel));
                return CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName("PrtBoolValue"), CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("parent", "continuation", "ReturnAndResetNondet")));
            }
            #endregion

            public SyntaxNode MkFunStackFrameClass()
            {
                SyntaxList<MemberDeclarationSyntax> members = new SyntaxList<MemberDeclarationSyntax>();
                string frameClassName = StackFrameClassName(funName);
                //public F1_Class_StackFrame(PrtFun fun, List<PrtValue> _locals) : base(fun, _locals) {}
                var pars = new List<SyntaxNode> { CSharpHelper.MkCSharpParameter(Identifier("locals"), CSharpHelper.MkCSharpGenericListType(IdentifierName("PrtValue"))),
                                                  CSharpHelper.MkCSharpParameter(Identifier("retLoc"), PredefinedType(Token(SyntaxKind.IntKeyword))) };
                SyntaxTokenList modifiers = new SyntaxTokenList();
                modifiers = modifiers.Add(Token(SyntaxKind.PublicKeyword));
                members = members.Add(CSharpHelper.MkCSharpConstructor(Identifier(frameClassName),
                                                          modifiers,
                                                          new List<SyntaxNode>() {
                                                              CSharpHelper.MkCSharpParameter(Identifier("fun"), (TypeSyntax) IdentifierName("PrtFun")),
                                                              CSharpHelper.MkCSharpParameter(Identifier("_locals"), CSharpHelper.MkCSharpGenericListType(IdentifierName("PrtValue"))) },
                                                          CSharpHelper.MkCSharpConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                                                              CSharpHelper.MkCSharpArgumentList(IdentifierName("fun"), IdentifierName("_locals"))),
                                                          new List<StatementSyntax>()));

                //public F2_Class_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base(fun, _locals, retLocation) {}
                members = members.Add(CSharpHelper.MkCSharpConstructor(Identifier(frameClassName),
                                                         modifiers,
                                                         new List<SyntaxNode>() {
                                                              CSharpHelper.MkCSharpParameter(Identifier("fun"), (TypeSyntax) IdentifierName("PrtFun")),
                                                              CSharpHelper.MkCSharpParameter(Identifier("_locals"), CSharpHelper.MkCSharpGenericListType(IdentifierName("PrtValue"))),
                                                              CSharpHelper.MkCSharpParameter(Identifier("retLocation"), PredefinedType(Token(SyntaxKind.IntKeyword))) },
                                                         CSharpHelper.MkCSharpConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                                                             CSharpHelper.MkCSharpArgumentList(IdentifierName("fun"), IdentifierName("_locals"), IdentifierName("retLocation"))),
                                                         new List<StatementSyntax>()));

                //public override PrtFunStackFrame Clone() {return this.Clone();}
                var body = SingletonList<StatementSyntax>(
                             ReturnStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        ThisExpression(),
                                        IdentifierName("Clone")))));
                var clonePars = new List<SyntaxNode>();
                members = members.Add((MemberDeclarationSyntax)CSharpHelper.MkCSharpMethodDeclaration(IdentifierName("PrtFunStackFrame"),
                                Identifier("Clone"),
                                new[] { Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword) },
                                body,
                                clonePars));

                //Getters/setters for locals variables of the function: parameters and locals
                foreach (var pair in funInfo.localNameToInfo)
                {
                    string varName = VarName(pair.Key);
                    //Debug:
                    //Console.WriteLine("Next local of function {0} is {1}", funName, varName);

                    int ind = pair.Value.index;
                    //Console.WriteLine("Index of the next local {0} is {1}", varName, ind);
                    //Example: public PrtValue Par1 {get {return locals[0];} {set {locals[0] = value;}}

                    modifiers = new SyntaxTokenList();
                    modifiers = modifiers.Add(Token(SyntaxKind.PublicKeyword));
                    var getBody = SingletonList<StatementSyntax>(ReturnStatement(
                           (ExpressionSyntax)CSharpHelper.MkCSharpElementAccessExpression(
                               IdentifierName("locals"), ind)));
                    var setBody = SingletonList<StatementSyntax>((StatementSyntax)CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                            (ExpressionSyntax)CSharpHelper.MkCSharpElementAccessExpression(
                                IdentifierName("locals"), ind),
                            IdentifierName("value")));
                    AccessorDeclarationSyntax[] accessorList = new AccessorDeclarationSyntax[]
                        { CSharpHelper.MkCSharpAccessor("get", getBody), CSharpHelper.MkCSharpAccessor("set", setBody)};
                    members = members.Add((MemberDeclarationSyntax)CSharpHelper.MkCSharpPropertyDecl("PrtValue", varName,
                                          modifiers,
                                          accessorList));
                }

                modifiers = new SyntaxTokenList();
                modifiers = modifiers.Add(Token(SyntaxKind.InternalKeyword));
                return CSharpHelper.MkCSharpClassDecl(frameClassName, modifiers,
                                         SingletonSeparatedList<BaseTypeSyntax>(CSharpHelper.MkCSharpIdentifierNameType("PrtFunStackFrame")),
                                         members);
            }

            private List<StatementSyntax> Flatten(StatementSyntax stmt)
            {
                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                BlockSyntax blockStmt = stmt as BlockSyntax;
                if (blockStmt == null)
                {
                    stmtList.Add(stmt);
                }
                else
                {
                    foreach (var x in blockStmt.Statements)
                    {
                        stmtList.AddRange(Flatten(x));
                    }
                }
                return stmtList;
            }

            public SyntaxNode MkExecuteMethod()
            {
                List<StatementSyntax> funStmts = new List<StatementSyntax>();
                //Line below is a template:
                //PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                if (owner != null)
                {
                    funStmts.Add(
                        LocalDeclarationStatement(
                            VariableDeclaration(
                                IdentifierName(owner.machineName))
                            .WithVariables(
                                SingletonSeparatedList<VariableDeclaratorSyntax>(
                                    VariableDeclarator(
                                        Identifier("parent"))
                                    .WithInitializer(
                                        EqualsValueClause(
                                                CSharpHelper.MkCSharpCastExpression(owner.machineName, IdentifierName("_parent")))))))
                        .NormalizeWhitespace());
                }
                string stackFrameClassName = StackFrameClassName(funName);
                
                funStmts.Add(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName(stackFrameClassName))
                        .WithVariables(
                            SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(
                                    Identifier("currFun"))
                                .WithInitializer(
                                    EqualsValueClause(
                                        CSharpHelper.MkCSharpCastExpression(
                                            stackFrameClassName, 
                                            InvocationExpression(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("parent"),
                                                    IdentifierName("PrtPopFunStackFrame")))))))))
                    .NormalizeWhitespace());

                funStmts.Add(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName("PrtValue"))
                        .WithVariables(
                            SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(
                                    Identifier("swap")))))
                        .NormalizeWhitespace());

                // Compute the body before calculating the label prelude
                SyntaxNode funBody = Factory.Instance.ToAST(funInfo.body).Compute<SyntaxNode>(
                   x => Unfold(x),
                   (x, ch) => Fold(x, ch.ToList()));

                if (labelCount > 0)
                {
                    funStmts.Add(EmitLabelPrelude());
                }
                funStmts.AddRange(Flatten((StatementSyntax)funBody));

                funStmts.Add(
                        ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("parent"),
                                    IdentifierName("PrtFunContReturn")))
                            .WithArgumentList(
                                ArgumentList(
                                    SingletonSeparatedList<ArgumentSyntax>(
                                        Argument(
                                            CSharpHelper.MkCSharpDot("currFun", "locals"))))))
                    .NormalizeWhitespace());

                var executeMethodDecl =
                    MethodDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.VoidKeyword)),
                        Identifier("Execute"))
                    .WithModifiers(
                        TokenList(
                            new[]{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.OverrideKeyword)}))
                    .WithParameterList(
                        ParameterList(
                            SeparatedList<ParameterSyntax>(
                                new SyntaxNodeOrToken[]{
                                    Parameter(
                                        Identifier("application"))
                                    .WithType(
                                        IdentifierName("StateImpl")),
                                    Token(SyntaxKind.CommaToken),
                                    Parameter(
                                        owner == null ? Identifier("parent") : Identifier("_parent"))
                                    .WithType(
                                        IdentifierName("PrtMachine"))})))
                    .WithBody(
                        //Block(stmt1, stmt2, stmt3, stmt4))
                        Block(funStmts))
                    .NormalizeWhitespace();

                return executeMethodDecl;
            }
            public SyntaxNode MkCreateLocalsMethod()
            {
                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                
                //var locals = new List<PrtValue>();
                stmtList.Add(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName("var"))
                        .WithVariables(
                            SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(
                                    Identifier("locals"))
                                .WithInitializer(
                                    EqualsValueClause(
                                        ObjectCreationExpression(
                                            GenericName(
                                                Identifier("List"))
                                            .WithTypeArgumentList(
                                                TypeArgumentList(
                                                    SingletonSeparatedList<TypeSyntax>(
                                                        IdentifierName("PrtValue")))))
                                        .WithArgumentList(
                                            ArgumentList()))))))
                    .NormalizeWhitespace());

                //foreach (var item in args)
                stmtList.Add(
                    ForEachStatement(
                        IdentifierName("var"),
                        Identifier("item"),
                        IdentifierName("args"),
                        Block(
                            SingletonList<StatementSyntax>(
                                ExpressionStatement(
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("locals"),
                                            IdentifierName("Add")))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList<ArgumentSyntax>(
                                                Argument(
                                                    InvocationExpression(
                                                        MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            IdentifierName("item"),
                                                            IdentifierName("Clone")))))))))))
                    .NormalizeWhitespace());

                foreach (var varName in funInfo.localNames)
                {
                    var varInfo = funInfo.localNameToInfo[varName];
                    var defaultValue = CSharpHelper.MkCSharpInvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("PrtValue"), IdentifierName("PrtMkDefaultValue")),
                        pToCSharp.typeContext.PTypeToCSharpExpr(varInfo.type));
                    stmtList.Add(ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("locals", "Add"), defaultValue)));
                }

                for (int i = funInfo.parameterNames.Count + funInfo.localNames.Count; i < funInfo.maxNumLocals; i++)
                {
                    stmtList.Add(ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("locals", "Add"), MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("PrtValue"), IdentifierName("@null")))));
                }

                //return locals;
                stmtList.Add(
                    ReturnStatement(IdentifierName("locals"))
                    .NormalizeWhitespace());

                //public override List<PrtValue> CreateLocals(params PrtValue[] args) { ... }
                var createLocalsMethodDecl =
                    MethodDeclaration(
                        GenericName(
                            Identifier("List"))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SingletonSeparatedList<TypeSyntax>(
                                    IdentifierName("PrtValue")))),
                        Identifier("CreateLocals"))
                    .WithModifiers(
                        TokenList(
                            new[]{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.OverrideKeyword)}))
                    .WithParameterList(
                        ParameterList(
                            SingletonSeparatedList<ParameterSyntax>(
                                Parameter(
                                    Identifier("args"))
                                .WithModifiers(
                                    TokenList(
                                        Token(SyntaxKind.ParamsKeyword)))
                                .WithType(
                                    ArrayType(
                                        IdentifierName("PrtValue"))
                                    .WithRankSpecifiers(
                                        SingletonList<ArrayRankSpecifierSyntax>(
                                            ArrayRankSpecifier(
                                                SingletonSeparatedList<ExpressionSyntax>(
                                                    OmittedArraySizeExpression()))))))))
                    .WithBody(
                        Block(stmtList))
                    .NormalizeWhitespace();

                return createLocalsMethodDecl;
            }
            public SyntaxNode MkFunToStringMethod()
            {
                var body = SingletonList<StatementSyntax>(ReturnStatement(CSharpHelper.MkCSharpStringLiteralExpression(funName)));
                var pars = new List<SyntaxNode> { };
                return CSharpHelper.MkCSharpMethodDeclaration(IdentifierName("string"), Identifier("ToString"),
                    new[] { Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword) },
                    body,
                    pars);
            }
            public SyntaxNode MkCreateFunStackFrameMethod()
            {
                var body = SingletonList<StatementSyntax>(
                            ReturnStatement((ExpressionSyntax)CSharpHelper.MkCSharpObjectCreationExpression(
                                IdentifierName(StackFrameClassName(funName)),
                                new SyntaxNode[] { ThisExpression(), IdentifierName("locals"), IdentifierName("retLoc") })));
                var pars = new List<SyntaxNode> { CSharpHelper.MkCSharpParameter(Identifier("locals"), CSharpHelper.MkCSharpGenericListType(IdentifierName("PrtValue"))),
                                                  CSharpHelper.MkCSharpParameter(Identifier("retLoc"), PredefinedType(Token(SyntaxKind.IntKeyword))) };
                return CSharpHelper.MkCSharpMethodDeclaration(IdentifierName("PrtFunStackFrame"), Identifier("CreateFunStackFrame"),
                    new[] { Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword) },
                    body,
                    pars);
            }
            public SyntaxNode MkFuncClass()
            {
                SyntaxList<MemberDeclarationSyntax> funMembers = new SyntaxList<MemberDeclarationSyntax>();

                //IsAnonFun property for anon functions only (getter only):
                PropertyDeclarationSyntax isAnonProperty;
                if (funName.StartsWith("AnonFun"))
                {
                    isAnonProperty =
                        PropertyDeclaration(
                            PredefinedType(
                                Token(SyntaxKind.BoolKeyword)),
                            Identifier("IsAnonFun"))
                        .WithModifiers(
                            TokenList(
                                new[]{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.OverrideKeyword)}))
                        .WithAccessorList(
                            AccessorList(
                                SingletonList<AccessorDeclarationSyntax>(
                                    AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration,
                                        Block(
                                            SingletonList<StatementSyntax>(
                                                ReturnStatement(
                                                    LiteralExpression(
                                                        SyntaxKind.TrueLiteralExpression))))))))
                        .NormalizeWhitespace();
                }
                else
                {
                    isAnonProperty =
                        PropertyDeclaration(
                            PredefinedType(
                                Token(SyntaxKind.BoolKeyword)),
                            Identifier("IsAnonFun"))
                        .WithModifiers(
                            TokenList(
                                new[]{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.OverrideKeyword)}))
                        .WithAccessorList(
                            AccessorList(
                                SingletonList<AccessorDeclarationSyntax>(
                                    AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration,
                                        Block(
                                            SingletonList<StatementSyntax>(
                                                ReturnStatement(
                                                    LiteralExpression(
                                                        SyntaxKind.FalseLiteralExpression))))))))
                        .NormalizeWhitespace();
                }
                funMembers = funMembers.Add(isAnonProperty);
                funMembers = funMembers.Add((MemberDeclarationSyntax)MkFunStackFrameClass());
                funMembers = funMembers.Add((MemberDeclarationSyntax)MkExecuteMethod());
                funMembers = funMembers.Add((MemberDeclarationSyntax)MkCreateLocalsMethod());
                funMembers = funMembers.Add((MemberDeclarationSyntax)MkCreateFunStackFrameMethod());
                funMembers = funMembers.Add((MemberDeclarationSyntax)MkFunToStringMethod());
                var funClassDecl =
                    ClassDeclaration(funClassName)
                    .WithModifiers(
                        TokenList(
                            Token(SyntaxKind.PublicKeyword)))
                    .WithBaseList(
                        BaseList(
                            SingletonSeparatedList<BaseTypeSyntax>(
                                SimpleBaseType(
                                    IdentifierName("PrtFun")))))
                    .WithMembers(funMembers)
                   .NormalizeWhitespace();

                return funClassDecl;
            }

            public void AddFunClass()
            {
                //Function class declaration should be generated in two cases:
                //1. For all global static functions
                //2. For other functions: if this function name was not already encountered
                if (owner == null || !(owner == null) && !owner.processedFuns.Contains(funName))
                {
                    //Class declaration:
                    List<SyntaxNode> whereToAdd;
                    whereToAdd = (this.owner == null) ? pToCSharp.members : owner.machineMembers;

                    whereToAdd.Add(MkFuncClass());

                    //Variable declaration:
                    whereToAdd.Add(
                        FieldDeclaration(
                            VariableDeclaration(
                                IdentifierName(funClassName))
                            .WithVariables(
                                SingletonSeparatedList<VariableDeclaratorSyntax>(
                                    VariableDeclarator(
                                        Identifier(funName)).WithInitializer(EqualsValueClause(CSharpHelper.MkCSharpObjectCreationExpression(IdentifierName(funClassName)))))))
                        .WithModifiers(
                            TokenList(
                                new[]{
                                        Token(SyntaxKind.PublicKeyword),
                                        Token(SyntaxKind.StaticKeyword)}))
                        .NormalizeWhitespace());

                    if (!(owner == null))
                    {
                        owner.processedFuns.Add(funName);
                    }
                }
            }
        }
        private void MkStaticFunctions()
        {
            foreach (var pair in allGlobalFuns)
            {
                if(!pair.Value.isFunProto)
                {
                    MkFunctionDecl funDecl = new MkFunctionDecl(pair.Key, pair.Value, null, this);
                    funDecl.AddFunClass();
                }
                
            }
        }

        private void MkCreateMachineMethods()
        {
            foreach (var machineName in allMachines.Keys)
            {
                if (allMachines[machineName].IsReal)
                {
                    MkCreateRealMachineMethod(machineName);
                }
                else
                {
                    MkCreateSpecMachineMethod(machineName);
                }
            }
        }

        private void MkCreateRealMachineMethod(string machineName)
        {
            List<SyntaxNode> fields = new List<SyntaxNode>();

            //create machine
            MachineInfo machineInfo = allMachines[machineName];
            //There are three cases:
            //- default (no constraint on queue size): maxQueueSizeAssumed == false; maxQueueSize = default (10?) 
            //- assume <maxQueueSize>: maxQueueSize > 0, maxQueueSizeAssumed == true;
            //- assert <maxQueueSize>: maxQueueSize > 0, maxQueueSizeAssumed == false;   
            if (machineInfo.maxQueueSize > 0)
            {
                fields.Add(generator.LocalDeclarationStatement(generator.IdentifierName("var"), "machine",
                               generator.ObjectCreationExpression(generator.IdentifierName(machineName),
                               new List<SyntaxNode>() { generator.IdentifierName("application"), generator.LiteralExpression(machineInfo.maxQueueSize),
                                                            generator.LiteralExpression(machineInfo.maxQueueSizeAssumed)})));
            }
            else
            {
                fields.Add(generator.LocalDeclarationStatement(generator.IdentifierName("var"), "machine",
                               generator.ObjectCreationExpression(generator.IdentifierName(machineName),
                               new List<SyntaxNode>() {
                                   generator.IdentifierName("application"),
                                   MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("PrtImplMachine"), IdentifierName("DefaultMaxBufferSize")),
                                   generator.LiteralExpression(machineInfo.maxQueueSizeAssumed) })));
            }

            

            // log
            fields.Add(
                CSharpHelper.MkCSharpTrace(
                    string.Format("<CreateLog> Created Machine {0}-{{0}}", machineName),
                    CSharpHelper.MkCSharpDot("machine", "instanceNumber")));

            //initialize the permission set for self
            if(allMachines[machineName].receiveSet == null)
            {
                fields.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot(IdentifierName("machine"), "self", "permissions"), LiteralExpression(SyntaxKind.NullLiteralExpression)));
            }
            else
            {
                foreach (var ev in allMachines[machineName].receiveSet)
                {
                    fields.Add(
                        CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(IdentifierName("machine"), "self", "permissions", "Add"), GetEventVar(ev))
                        );
                }
            }

            //initialize the send set
            if (allMachines[machineName].sendsSet ==  null)
            {
                fields.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot(IdentifierName("machine"), "sends"), LiteralExpression(SyntaxKind.NullLiteralExpression)));
            }
            else
            {
                foreach (var ev in allMachines[machineName].sendsSet)
                {
                    fields.Add(
                        CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(IdentifierName("machine"), "sends", "Add"), GetEventVar(ev))
                        );
                }
            }

            //machine.currentPayload = payload;
            fields.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                CSharpHelper.MkCSharpDot("machine", "currentPayload"),
                IdentifierName("payload")));

            //return machine;
            fields.Add(generator.ReturnStatement(generator.IdentifierName("machine")));

            var methodPars = new SyntaxNode[] {
                    generator.ParameterDeclaration("application", generator.IdentifierName("StateImpl")),
                    generator.ParameterDeclaration("payload", generator.IdentifierName("PrtValue")) };
            var makeCreateMachineDecl = generator.MethodDeclaration(string.Format("CreateMachine_{0}", machineName), methodPars,
              null, generator.IdentifierName("PrtImplMachine"),
              Accessibility.Public, DeclarationModifiers.Static,
              statements: fields);
            members.Add(makeCreateMachineDecl);
        }

        private void MkCreateSpecMachineMethod(string machineName)
        {
            List<SyntaxNode> fields = new List<SyntaxNode>();
            
            //create machine
            fields.Add(generator.LocalDeclarationStatement(generator.IdentifierName("var"), "machine",
                           generator.ObjectCreationExpression(generator.IdentifierName(machineName),
                           new List<SyntaxNode>() { generator.IdentifierName("application") })));

            //log
            fields.Add(CSharpHelper.MkCSharpTrace(string.Format("<CreateLog> Created spec Machine {0}", machineName)));

            foreach (var x in allMachines[machineName].observesEvents)
            {
                fields.Add(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("machine", "observes", "Add"), GetEventVar(x)));
            }

            //return machine;
            fields.Add(generator.ReturnStatement(generator.IdentifierName("machine")));

            var methodPars = new SyntaxNode[] {
                    generator.ParameterDeclaration("application", generator.IdentifierName("StateImpl")) };
            var makeCreateSpecDecl = generator.MethodDeclaration(string.Format("CreateSpecMachine_{0}", machineName), methodPars,
              null, IdentifierName("PrtSpecMachine"),    
              Accessibility.Public, DeclarationModifiers.Static,
              statements: fields);
            members.Add(makeCreateSpecDecl);
        }

        private void MkMachineClasses()
        {
            //Debug only:
            //Console.WriteLine("Number of machines: {0}", allMachines.Count());
            foreach (var pair in allMachines)
            {
                MkMachineClass mkMachine;
                mkMachine = new MkMachineClass(this, pair.Key, pair.Value);
                SyntaxNode node = mkMachine.MkClass();
                members.Add(node);
            }
        }
        internal class MkMachineClass
        {
            public PToCSharpCompiler translator;
            public string machineName;
            public MachineInfo machineInfo;
            public List<SyntaxNode> machineMembers = new List<SyntaxNode>();
            public List<StatementSyntax> mainConstructorFields = new List<StatementSyntax>();
            //keeps track of already encountered function names:
            public HashSet<string> processedFuns = new HashSet<string>();
            private int transition_count = 1;
            public MkMachineClass(PToCSharpCompiler ptoCsharp, string name, MachineInfo info)
            {
                translator = ptoCsharp;
                machineName = name;
                machineInfo = info;
            }
            public StatementSyntax MkStateInstantiation(string stateName, string stateType, string entryFun, string exitFun,
                                                    bool hasNullTransition, StateTemperature temperature)
            {
                if (hasNullTransition)
                {
                    if (temperature == StateTemperature.WARM)
                    {
                        return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(stateName),
                                ObjectCreationExpression(
                                    IdentifierName(stateType))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                Argument(
                                                    IdentifierName("\"" + stateName + "\"")),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(entryFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(exitFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    LiteralExpression(
                                                        SyntaxKind.TrueLiteralExpression)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("StateTemperature"),
                                                        IdentifierName("Warm")))})))))
                        .NormalizeWhitespace();
                    }
                    else if (temperature == StateTemperature.HOT)
                    {
                        return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(stateName),
                                ObjectCreationExpression(
                                    IdentifierName(stateType))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                Argument(
                                                    IdentifierName("\"" + stateName + "\"")),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(entryFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(exitFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    LiteralExpression(
                                                        SyntaxKind.TrueLiteralExpression)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("StateTemperature"),
                                                        IdentifierName("Hot")))})))))
                        .NormalizeWhitespace();
                    }
                    else
                    {
                        return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(stateName),
                                ObjectCreationExpression(
                                    IdentifierName(stateType))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                Argument(
                                                    IdentifierName("\"" + stateName + "\"")),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(entryFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(exitFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    LiteralExpression(
                                                        SyntaxKind.TrueLiteralExpression)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("StateTemperature"),
                                                        IdentifierName("Cold")))})))))
                        .NormalizeWhitespace();
                    }
                }
                else
                {
                    if (temperature == StateTemperature.WARM)
                    {
                        return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(stateName),
                                ObjectCreationExpression(
                                    IdentifierName(stateType))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                Argument(
                                                    IdentifierName("\"" + stateName + "\"")),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(entryFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(exitFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    LiteralExpression(
                                                        SyntaxKind.FalseLiteralExpression)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("StateTemperature"),
                                                        IdentifierName("Warm")))})))))
                        .NormalizeWhitespace();
                    }
                    else if (temperature == StateTemperature.HOT)
                    {
                        return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(stateName),
                                ObjectCreationExpression(
                                    IdentifierName(stateType))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                Argument(
                                                    IdentifierName("\"" + stateName + "\"")),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(entryFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(exitFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    LiteralExpression(
                                                        SyntaxKind.FalseLiteralExpression)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("StateTemperature"),
                                                        IdentifierName("Hot")))})))))
                        .NormalizeWhitespace();
                    }
                    else
                    {
                        return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(stateName),
                                ObjectCreationExpression(
                                    IdentifierName(stateType))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                Argument(
                                                    IdentifierName("\"" + stateName + "\"")),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(entryFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(exitFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    LiteralExpression(
                                                        SyntaxKind.FalseLiteralExpression)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("StateTemperature"),
                                                        IdentifierName("Cold")))})))))
                        .NormalizeWhitespace();
                    }
                }
            }
            public FunInfo GetFunInfo(string funName)
            {
                if (translator.allGlobalFuns.ContainsKey(funName))
                {
                    return translator.allGlobalFuns[funName];
                }
                else
                {
                    return machineInfo.funNameToFunInfo[funName];
                }
            }
            public SyntaxNode MkClass()
            {
                var generator = translator.generator;
                //StartState property (getter only, since there's no setter in the base class):
                string startState = machineInfo.initStateName;
                var startStateProperty =
                    PropertyDeclaration(
                        IdentifierName("PrtState"),
                        Identifier("StartState"))
                    .WithModifiers(
                        TokenList(
                            new[]{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.OverrideKeyword)}))
                    .WithAccessorList(
                        AccessorList(
                            SingletonList<AccessorDeclarationSyntax>(
                                AccessorDeclaration(
                                    SyntaxKind.GetAccessorDeclaration,
                                    Block(
                                        SingletonList<StatementSyntax>(
                                            ReturnStatement(
                                                IdentifierName(startState))))))))
                    .NormalizeWhitespace();
                machineMembers.Add(startStateProperty);

                //Add getters/setters for machine variables:
                int ind = 0;
                foreach (var pair in machineInfo.localVariableToVarInfo)
                {
                    SyntaxTokenList modifiers = new SyntaxTokenList();
                    modifiers = modifiers.Add(Token(SyntaxKind.PublicKeyword));
                    var getBody = SingletonList<StatementSyntax>(ReturnStatement(
                               (ExpressionSyntax)CSharpHelper.MkCSharpElementAccessExpression(
                                   IdentifierName("fields"), ind)));
                    var setBody = SingletonList<StatementSyntax>((StatementSyntax)CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                            (ExpressionSyntax)CSharpHelper.MkCSharpElementAccessExpression(
                                IdentifierName("fields"), ind),
                            IdentifierName("value")));
                    AccessorDeclarationSyntax[] accessorList = new AccessorDeclarationSyntax[]
                            { CSharpHelper.MkCSharpAccessor("get", getBody), CSharpHelper.MkCSharpAccessor("set", setBody)};
                    machineMembers.Add(CSharpHelper.MkCSharpPropertyDecl("PrtValue", VarName(pair.Key), modifiers, accessorList));
                    ind += 1;
                }

                var skeletonMethodBody = generator.ReturnStatement(generator.ObjectCreationExpression(generator.IdentifierName(machineName)));
                var skeletonMethodDecl = generator.MethodDeclaration("MakeSkeleton", null,
                  null, generator.IdentifierName(machineInfo.IsReal ? "PrtImplMachine" : "PrtSpecMachine"),
                  Accessibility.Public,
                  DeclarationModifiers.Override,
                  new SyntaxNode[] { skeletonMethodBody });
                machineMembers.Add(skeletonMethodDecl);

                //(this.GetType())
                var returnMethodPars = new SyntaxNode[]
                {
                    generator.InvocationExpression(generator.MemberAccessExpression(generator.ThisExpression(), generator.IdentifierName("GetType")))
                };
                //return app.NextMachineInstanceNumber(this.GetType());
                var nextInstanceNumberMethodBody = generator.ReturnStatement(generator.InvocationExpression(generator.MemberAccessExpression(
                     generator.IdentifierName("app"), generator.IdentifierName("NextMachineInstanceNumber")), returnMethodPars));

                //(StateImpl app)
                if (machineInfo.IsReal)
                {
                    var methodPars = new SyntaxNode[] { generator.ParameterDeclaration("app", generator.IdentifierName("StateImpl")) };
                    var nextInstanceNumberMethodDecl = generator.MethodDeclaration("NextInstanceNumber", methodPars,
                      //null, generator.IdentifierName("int"),
                      null, generator.TypeExpression(SpecialType.System_Int32),
                      Accessibility.Public,
                      DeclarationModifiers.Override,
                      new SyntaxNode[] { nextInstanceNumberMethodBody });
                    machineMembers.Add(nextInstanceNumberMethodDecl);
                }

                //Name property (getter only):
                var nameProperty =
                    PropertyDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.StringKeyword)),
                        Identifier("Name"))
                    .WithModifiers(
                        TokenList(
                            new[]{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.OverrideKeyword)}))
                    .WithAccessorList(
                        AccessorList(
                            SingletonList<AccessorDeclarationSyntax>(
                                AccessorDeclaration(
                                    SyntaxKind.GetAccessorDeclaration,
                                    Block(
                                        SingletonList<StatementSyntax>(
                                            ReturnStatement(
                                                IdentifierName("\"" + machineName + "\""))))))))
                    .NormalizeWhitespace();
                machineMembers.Add(nameProperty);

                //constructor for cloning
                var constructor_1 = generator.ConstructorDeclaration(machineName, null, Accessibility.Public, baseConstructorArguments: new SyntaxNode[0]);
                machineMembers.Add(constructor_1);

                //Machine class constructor
                //public PONG(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
                List<SyntaxNode> constructorStmtList = new List<SyntaxNode>();
                foreach (var varInfo in machineInfo.localVariableToVarInfo.Values)
                {
                    var defaultValue = CSharpHelper.MkCSharpInvocationExpression(MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("PrtValue"),
                                IdentifierName("PrtMkDefaultValue")), translator.typeContext.PTypeToCSharpExpr(varInfo.type));
                    constructorStmtList.Add(ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("fields", "Add"), defaultValue)));
                }
                if (machineInfo.IsReal)
                {
                    var constructorPars = new SyntaxNode[]
                    {
                        generator.ParameterDeclaration("app", generator.IdentifierName("StateImpl")),
                        generator.ParameterDeclaration("maxB", generator.TypeExpression(SpecialType.System_Int32)),
                        generator.ParameterDeclaration("assume", generator.TypeExpression(SpecialType.System_Boolean))
                    };
                    var baseConstructorArgs = new SyntaxNode[] { generator.IdentifierName("app"), generator.IdentifierName("maxB"), generator.IdentifierName("assume") };

                    var constructor_2 = generator.ConstructorDeclaration(machineName, constructorPars, Accessibility.Public,
                                        baseConstructorArguments: baseConstructorArgs,
                                        statements: constructorStmtList);
                    machineMembers.Add(constructor_2);
                }
                else
                {
                    var constructorPars = new SyntaxNode[]
                    {
                        generator.ParameterDeclaration("app", generator.IdentifierName("StateImpl"))
                    };
                    var baseConstructorArgs = new SyntaxNode[] { generator.IdentifierName("app") };
                    var constructor_2 = generator.ConstructorDeclaration(machineName, constructorPars, Accessibility.Public,
                                        baseConstructorArguments: baseConstructorArgs,
                                        statements: constructorStmtList);
                    machineMembers.Add(constructor_2);
                }

                // Fun classes for all functions of the machine:
                foreach (var pair in machineInfo.funNameToFunInfo)
                {
                    MkFunctionDecl funDecl = new MkFunctionDecl(pair.Key, pair.Value, this, translator);
                    funDecl.AddFunClass();
                }

                foreach (var pair in machineInfo.stateNameToStateInfo)
                {
                    string stateName = pair.Key;
                    string stateType = stateName + "_Class";
                    //state instantiation for the machine class constructor:
                    //Example: 
                    //PONG_Pong_WaitPing = new PONG_Pong_WaitPing_Class("PONG_Pong_WaitPing", AnonFun1, AnonFun0, false, StateTemperature.Warm);
                    //Consider 6 cases (write a mmethod): for each bool value of hasNullTransition, there are 3 possible "temperatures"
                    mainConstructorFields.Add(MkStateInstantiation(stateName, stateType, pair.Value.entryActionName, pair.Value.exitFunName,
                                              pair.Value.hasNullTransition, pair.Value.temperature));
                }

                //State classes for all states of the machine:
                foreach (var pair in machineInfo.stateNameToStateInfo)
                {
                    string stateName = pair.Key;
                    string stateType = stateName + "_Class";
                    //state class declaration:
                    var stateClassDecl =
                        ClassDeclaration(stateType)
                            .WithModifiers(
                                TokenList(
                                    Token(SyntaxKind.PublicKeyword)))
                            .WithBaseList(
                                BaseList(
                                    SingletonSeparatedList<BaseTypeSyntax>(
                                        SimpleBaseType(
                                            IdentifierName("PrtState")))))
                            .WithMembers(
                                SingletonList<MemberDeclarationSyntax>(
                                    ConstructorDeclaration(
                                        Identifier(stateType))
                                    .WithModifiers(
                                        TokenList(
                                            Token(SyntaxKind.PublicKeyword)))
                                    .WithParameterList(
                                        ParameterList(
                                            SeparatedList<ParameterSyntax>(
                                                new SyntaxNodeOrToken[]{
                                                    Parameter(
                                                        Identifier("name"))
                                                    .WithType(
                                                        PredefinedType(
                                                            Token(SyntaxKind.StringKeyword))),
                                                    Token(SyntaxKind.CommaToken),
                                                    Parameter(
                                                        Identifier("entryFun"))
                                                    .WithType(
                                                        IdentifierName("PrtFun")),
                                                    Token(SyntaxKind.CommaToken),
                                                    Parameter(
                                                        Identifier("exitFun"))
                                                    .WithType(
                                                        IdentifierName("PrtFun")),
                                                    Token(SyntaxKind.CommaToken),
                                                    Parameter(
                                                        Identifier("hasNullTransition"))
                                                    .WithType(
                                                        PredefinedType(
                                                            Token(SyntaxKind.BoolKeyword))),
                                                    Token(SyntaxKind.CommaToken),
                                                    Parameter(
                                                        Identifier("temperature"))
                                                    .WithType(
                                                        IdentifierName("StateTemperature"))})))
                                    .WithInitializer(
                                        ConstructorInitializer(
                                            SyntaxKind.BaseConstructorInitializer,
                                            ArgumentList(
                                                SeparatedList<ArgumentSyntax>(
                                                    new SyntaxNodeOrToken[]{
                                                        Argument(
                                                            IdentifierName("name")),
                                                        Token(SyntaxKind.CommaToken),
                                                        Argument(
                                                            IdentifierName("entryFun")),
                                                        Token(SyntaxKind.CommaToken),
                                                        Argument(
                                                            IdentifierName("exitFun")),
                                                        Token(SyntaxKind.CommaToken),
                                                        Argument(
                                                            IdentifierName("hasNullTransition")),
                                                        Token(SyntaxKind.CommaToken),
                                                        Argument(
                                                            IdentifierName("temperature"))}))))
                                    .WithBody(
                                        Block())))
                        .NormalizeWhitespace();
                    machineMembers.Add(stateClassDecl);

                    //state declaration: 
                    //Example: public static Init Init_State;
                    var stateDeclaration =
                        FieldDeclaration(
                            VariableDeclaration(
                                IdentifierName(stateType))
                            .WithVariables(
                                SingletonSeparatedList<VariableDeclaratorSyntax>(
                                    VariableDeclarator(
                                        Identifier(stateName)))))
                        .WithModifiers(
                            TokenList(
                                new[]{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.StaticKeyword)}))
                        .NormalizeWhitespace();
                    machineMembers.Add(stateDeclaration);

                    //Add DoDecls to the StateInfo:
                    foreach (var doFun in pair.Value.dos)
                    {
                        ExpressionSyntax doFunExpr = IdentifierName(doFun.Value);
                        if (doFun.Value == "ignore")
                        {
                            doFunExpr = MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("PrtFun"),
                                            IdentifierName("IgnoreFun"));
                        }
                        mainConstructorFields.Add(
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(stateName),
                                            IdentifierName("dos")),
                                        IdentifierName("Add")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                Argument(
                                                    translator.GetEventVar(doFun.Key)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(doFunExpr)}))))
                            .NormalizeWhitespace()
                            );
                    }
                    
                    //Add deferred events (if any) to the state:
                    foreach (var deferredEvent in pair.Value.deferredEvents)
                    {
                        mainConstructorFields.Add(
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(stateName),
                                            IdentifierName("deferredSet")),
                                        IdentifierName("Add")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList<ArgumentSyntax>(
                                            Argument(
                                                translator.GetEventVar(deferredEvent))))))
                            .NormalizeWhitespace()
                        );
                    }

                    //Transition instantiations for the state of the machine:

                    //"transition" + "_" + transition_count
                    foreach (var transition in pair.Value.transitions)
                    {
                        string trigger = transition.Key;
                        string transition_name = "transition" + "_" + transition_count;
                        if (transition.Value.IsPush)
                        {
                            //push transition:
                            mainConstructorFields.Add(
                            LocalDeclarationStatement(
                                VariableDeclaration(
                                    IdentifierName("PrtTransition"))
                                .WithVariables(
                                    SingletonSeparatedList<VariableDeclaratorSyntax>(
                                        VariableDeclarator(
                                            Identifier(transition_name))
                                        .WithInitializer(
                                            EqualsValueClause(
                                                ObjectCreationExpression(
                                                    IdentifierName("PrtTransition"))
                                                .WithArgumentList(
                                                    ArgumentList(
                                                        SeparatedList<ArgumentSyntax>(
                                                            new SyntaxNodeOrToken[]{
                                                                Argument(
                                                                    MemberAccessExpression(
                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                        IdentifierName("PrtFun"),
                                                                        IdentifierName("IgnoreFun"))),
                                                                        Token(SyntaxKind.CommaToken),
                                                                Argument(
                                                                    IdentifierName(transition.Value.target)),
                                                                Token(SyntaxKind.CommaToken),
                                                                Argument(
                                                                    LiteralExpression(
                                                                        SyntaxKind.TrueLiteralExpression))}))))))))
                            .NormalizeWhitespace());
                        }
                        else
                        {
                            //goto transition:
                            mainConstructorFields.Add(
                            LocalDeclarationStatement(
                                VariableDeclaration(
                                    IdentifierName("PrtTransition"))
                                .WithVariables(
                                    SingletonSeparatedList<VariableDeclaratorSyntax>(
                                        VariableDeclarator(
                                            Identifier("transition" + "_" + transition_count))
                                        .WithInitializer(
                                            EqualsValueClause(
                                                ObjectCreationExpression(
                                                    IdentifierName("PrtTransition"))
                                                .WithArgumentList(
                                                    ArgumentList(
                                                        SeparatedList<ArgumentSyntax>(
                                                            new SyntaxNodeOrToken[]{
                                                                Argument(
                                                                    IdentifierName(transition.Value.transFunName)),
                                                                Token(SyntaxKind.CommaToken),
                                                                Argument(
                                                                    IdentifierName(transition.Value.target)),
                                                                Token(SyntaxKind.CommaToken),
                                                                Argument(
                                                                    LiteralExpression(
                                                                        SyntaxKind.FalseLiteralExpression))}))))))))
                            .NormalizeWhitespace());
                        }
                        //Add transition to the state:
                        mainConstructorFields.Add(
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(stateName),
                                            IdentifierName("transitions")),
                                        IdentifierName("Add")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                    Argument(
                                                        translator.GetEventVar(trigger)),
                                                    Token(SyntaxKind.CommaToken),
                                                    Argument(
                                                        IdentifierName(transition_name))}))))
                            .NormalizeWhitespace()
                            );
                        transition_count += 1;
                    }
                }

                //Constructor for the machine class:
                //static Machine()
                var mainConstructor =
                    ConstructorDeclaration(
                        Identifier(machineName))
                            .WithModifiers(
                                TokenList(
                                    Token(SyntaxKind.StaticKeyword)))
                            .WithBody(
                                Block(mainConstructorFields
                                    ))
                    .NormalizeWhitespace();

                machineMembers.Add(mainConstructor);

                var mainMachineClassDecl = generator.ClassDeclaration(
                  machineName, typeParameters: null,
                  accessibility: Accessibility.Public,
                  baseType: generator.IdentifierName(machineInfo.IsReal ? "PrtImplMachine" : "PrtSpecMachine"),
                  members: machineMembers);

                return mainMachineClassDecl;
            }
        }
        private void MkCSharpOutput()
        {
            var applicationcClassDeclaration = generator.ClassDeclaration(
              "Application", typeParameters: null,
              modifiers: DeclarationModifiers.Partial,
              accessibility: Accessibility.Public,
              baseType: generator.IdentifierName("StateImpl"),
              members: members);

            // Declare a namespace
            //Generate namespace name: <output file name, no ".cs"> 
            //int index = this.csharpFileName.LastIndexOf(".");
            //string namespaceName = this.csharpFileName.Substring(0, index);
            //namespace is "P.Program" for all P programs:
            var programNameSpaceDeclaration = generator.NamespaceDeclaration(
                IdentifierName("P.Program"), 
                UsingDirective(IdentifierName("P.Runtime")), 
                UsingDirective(IdentifierName("System")),
                UsingDirective(IdentifierName("System.Collections.Generic")),
                applicationcClassDeclaration);
                

            // Get a CompilationUnit (code file) for the generated code
            result = generator.CompilationUnit(programNameSpaceDeclaration).NormalizeWhitespace();
        }

        private void EmitCSharpOutput()
        {
            var outputDir = compiler.Options.outputDir;
            compiler.Log.WriteMessage(string.Format("Writing {0} ...", cSharpFileName), SeverityKind.Info);
            System.IO.StreamWriter file = new System.IO.StreamWriter(Path.Combine(outputDir, cSharpFileName));
            file.WriteLine("#pragma warning disable CS0162, CS0164, CS0168, CS0649");
            file.WriteLine(result);
            file.Close();
        }

        #endregion
    }

    class PToCSharpLinker
    {
        private SyntaxGenerator generator;
        private List<string> inputFiles;
        private ICompilerOutput Log { get; set; }
        internal class TestCaseInfo
        {
            public Dictionary<string, Dictionary<string, string>> linkMap;
            public Dictionary<string, string> renameMap;
            public Dictionary<string, bool> isSafeMap;
            public Dictionary<string, List<string>> specMachineMap;
            public Dictionary<string, List<string>> interfaceMap;
            public HashSet<string> sendActions;
            public HashSet<string> createActions;
            public TestCaseInfo()
            {
                linkMap = new Dictionary<string, Dictionary<string, string>>();
                renameMap = new Dictionary<string, string>();
                isSafeMap = new Dictionary<string, bool>();
                specMachineMap = new Dictionary<string, List<string>>();
                interfaceMap = new Dictionary<string, System.Collections.Generic.List<string>>();
                sendActions = new HashSet<string>();
                createActions = new HashSet<string>();
            }
        }

        private Dictionary<string, TestCaseInfo> allTests;
        private Dictionary<string, List<string>> dependsOn;
        public PToCSharpLinker(ICompilerOutput log, AST<Program> linkerModel, List<string> inputFilesNames)
        {
            this.inputFiles = inputFilesNames.ToList();
            Log = log;
            allTests = new Dictionary<string, TestCaseInfo>();
            dependsOn = new Dictionary<string, List<string>>();
            GenerateLinkerInfo(linkerModel);
        }

        void GenerateLinkerInfo(AST<Program> model)
        {
            
            var linkerModel = model.FindAny(
                new NodePred[] { NodePredFactory.Instance.MkPredicate(NodeKind.Program), NodePredFactory.Instance.MkPredicate(NodeKind.Model) });

            var factBins = new Dictionary<string, LinkedList<AST<FuncTerm>>>();
            linkerModel.FindAll(
                new NodePred[]
                {
                    NodePredFactory.Instance.Star,
                    NodePredFactory.Instance.MkPredicate(NodeKind.ModelFact)
                },

                (path, n) =>
                {
                    var mf = (ModelFact)n;
                    FuncTerm ft = (FuncTerm)mf.Match;
                    GetBin(factBins, ft).AddLast((AST<FuncTerm>)Factory.Instance.ToAST(ft));
                });
            LinkedList<AST<FuncTerm>> terms;

            
            terms = GetBin(factBins, "CSharpLinkMap");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var currMachineName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var IorMName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var impMachineName = ((Cnst)it.Current).GetStringValue();

                    if (allTests.ContainsKey(name))
                    {
                        if (allTests[name].linkMap.ContainsKey(currMachineName))
                        {
                            allTests[name].linkMap[currMachineName].Add(IorMName, impMachineName);
                        }
                        else
                        {
                            allTests[name].linkMap[currMachineName] = new Dictionary<string, string>();
                            allTests[name].linkMap[currMachineName].Add(IorMName, impMachineName);
                        }
                    }
                    else
                    {
                        allTests[name] = new TestCaseInfo();
                        allTests[name].linkMap[currMachineName] = new Dictionary<string, string>();
                        allTests[name].linkMap[currMachineName].Add(IorMName, impMachineName);
                    }
                }
            }

            terms = GetBin(factBins, "CSharpRenameMap");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var renamedMachineName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var impName = ((Cnst)it.Current).GetStringValue();
                   
                    if (allTests.ContainsKey(name))
                    {
                        if (allTests[name].renameMap.ContainsKey(renamedMachineName))
                        {
                            Console.WriteLine("Internal Error");
                            Environment.Exit(-1);
                        }
                        else
                        {
                            allTests[name].renameMap.Add(renamedMachineName, impName);
                        }
                    }
                    else
                    {
                        allTests[name] = new TestCaseInfo();
                        allTests[name].renameMap.Add(renamedMachineName, impName);
                    }
                }
            }

            terms = GetBin(factBins, "CSharpDependsOn");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var currFileName = ((Cnst)it.Current).GetStringValue();
                    currFileName = Path.GetFileNameWithoutExtension(currFileName);
                    it.MoveNext();
                    
                    if (it.Current.NodeKind == NodeKind.Id)
                    {
                        var name = ((Id)it.Current).Name;
                        if (name == "NIL")
                        {
                            if (!dependsOn.ContainsKey(currFileName))
                            { 
                                dependsOn[currFileName] = new List<string>();
                            }
                        }
                    }
                    else
                    {
                        var dOn = ((Cnst)it.Current).GetStringValue();
                        dOn = Path.GetFileNameWithoutExtension(dOn);

                        if (dependsOn.ContainsKey(currFileName))
                        {
                            dependsOn[currFileName].Add(dOn);
                        }
                        else
                        {
                            dependsOn[currFileName] = new List<string>();
                            dependsOn[currFileName].Add(dOn);
                        }
                    }
                    
                }
            }

            terms = GetBin(factBins, "CSharpSafeMap");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var renamedMachineName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var isSafe = (it.Current as Id).Name == PData.Cnst_True.Node.Name;

                    if (allTests.ContainsKey(name))
                    {
                        if (allTests[name].isSafeMap.ContainsKey(renamedMachineName))
                        {
                            Console.WriteLine("Internal Error");
                            Environment.Exit(-1);
                        }
                        else
                        {
                            allTests[name].isSafeMap.Add(renamedMachineName, isSafe);
                        }
                    }
                    else
                    {
                        allTests[name] = new TestCaseInfo();
                        allTests[name].isSafeMap.Add(renamedMachineName, isSafe);
                    }
                }
            }

            terms = GetBin(factBins, "CSharpMonitorMap");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var newSpecMachineName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var impMachine = ((Cnst)it.Current).GetStringValue();

                    if (allTests.ContainsKey(name))
                    {
                        if (allTests[name].specMachineMap.ContainsKey(newSpecMachineName))
                        {
                            allTests[name].specMachineMap[newSpecMachineName].Add(impMachine);
                        }
                        else
                        {
                            allTests[name].specMachineMap[newSpecMachineName] = new List<string>();
                            allTests[name].specMachineMap[newSpecMachineName].Add(impMachine);
                        }
                    }
                    else
                    {
                        allTests[name] = new TestCaseInfo();
                        allTests[name].specMachineMap[newSpecMachineName] = new List<string>();
                        allTests[name].specMachineMap[newSpecMachineName].Add(impMachine);
                    }
                }
            }

            terms = GetBin(factBins, "CSharpInterfaceMap");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var iname = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var evname = it.Current is Cnst ? ((Cnst)it.Current).GetStringValue() : "halt";


                    var testInfo = new TestCaseInfo();
                    if (allTests.ContainsKey(name))
                    {
                        if (allTests[name].interfaceMap.ContainsKey(iname))
                        {
                            allTests[name].interfaceMap[iname].Add(evname);
                        }
                        else
                        {
                            allTests[name].interfaceMap[iname] = new List<string>();
                            allTests[name].interfaceMap[iname].Add(evname);
                        }
                    }
                    else
                    {
                        allTests[name] = new TestCaseInfo();
                        allTests[name].interfaceMap[iname] = new List<string>();
                        allTests[name].interfaceMap[iname].Add(evname);
                    }
                }
            }

            terms = GetBin(factBins, "CSharpEventActionsRefinementTest");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var eventName = ((Cnst)it.Current).GetStringValue();

                    var lhsName = string.Format("{0}LHS", name);
                    var rhsName = string.Format("{0}RHS", name);
                    allTests[lhsName].sendActions.Add(eventName);
                    allTests[rhsName].sendActions.Add(eventName);
                }
            }

            terms = GetBin(factBins, "CSharpInterfaceActionsRefinementTest");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var eventName = ((Cnst)it.Current).GetStringValue();

                    var lhsName = string.Format("{0}LHS", name);
                    var rhsName = string.Format("{0}RHS", name);
                    allTests[lhsName].createActions.Add(eventName);
                    allTests[rhsName].createActions.Add(eventName);
                }
            }
        }

        public LinkedList<AST<FuncTerm>> GetBin(Dictionary<string, LinkedList<AST<FuncTerm>>> factBins, FuncTerm ft)
        {
            var fun = (Id)ft.Function;
            return GetBin(factBins, fun.Name);
        }

        public LinkedList<AST<FuncTerm>> GetBin(Dictionary<string, LinkedList<AST<FuncTerm>>> factBins, string name)
        {
            LinkedList<AST<FuncTerm>> bin;
            if (!factBins.TryGetValue(name, out bin))
            {
                bin = new LinkedList<AST<FuncTerm>>();
                factBins.Add(name, bin);
            }
            return bin;
        }

        private SyntaxNode MkEvents()
        {
            List<SyntaxNode> evDeclarations = new List<SyntaxNode>();
            List<StatementSyntax> eventInitializationStmts = new List<StatementSyntax>();
            string eventsClassName = "Events";

            //add halt event
            evDeclarations.Add(
            CSharpHelper.MkCSharpFieldDeclarationWithInit(IdentifierName("PrtEventValue"),
                "halt",
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.StaticKeyword),
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("PrtValue"), IdentifierName("halt"))
                )
            );

            //add null event
            evDeclarations.Add(
            CSharpHelper.MkCSharpFieldDeclarationWithInit(IdentifierName("PrtEventValue"),
                "@null",
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.StaticKeyword),
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("PrtValue"), IdentifierName("@null"))
                )
            );

            var eventsClass = generator.ClassDeclaration(
              eventsClassName, typeParameters: null,
              accessibility: Accessibility.Public,
              modifiers: DeclarationModifiers.Partial,
              members: evDeclarations);

            return eventsClass;
        }
        private List<SyntaxNode> MkAppConstructors(string testName)
        {

            //parameterless constructor
            List<SyntaxNode> constructorList = new List<SyntaxNode>();
            var constructor_1 = generator.ConstructorDeclaration("Application", null, Accessibility.Public, baseConstructorArguments: new SyntaxNode[0]);
            constructorList.Add(constructor_1);

            //constructor with the initialize paramenter which is called by Ptester at the start.
            var constructorParameters = new SyntaxNode[] {
                generator.ParameterDeclaration("initialize",
                    generator.TypeExpression(SpecialType.System_Boolean)) };
            List<SyntaxNode> stmtList = new List<SyntaxNode>();


            foreach (var monName in allTests[testName].specMachineMap.Keys)
            {
                stmtList.Add(ExpressionStatement(
                                CSharpHelper.MkCSharpInvocationExpression(IdentifierName("CreateSpecMachine"), CSharpHelper.MkCSharpStringLiteralExpression(monName))));
            }
            

            var constructorBody = generator.ExpressionStatement(
                generator.InvocationExpression(
                    generator.IdentifierName("CreateMainMachine")));
            stmtList.Add(constructorBody);
            
            var constructor_2 = generator.ConstructorDeclaration("Application", constructorParameters, Accessibility.Public, baseConstructorArguments: new SyntaxNode[0],
                                                                statements: stmtList.ToArray());
            constructorList.Add(constructor_2);

            //skeleton constructor used by the clone function.
            var makeSkeletonMethodBody = generator.ReturnStatement(generator.ObjectCreationExpression(generator.IdentifierName("Application")));
            var makeSkeletonMethodDecl = generator.MethodDeclaration("MakeSkeleton", null,
              null, generator.IdentifierName("StateImpl"),
              Accessibility.Public,
              DeclarationModifiers.Override,
              new SyntaxNode[] { makeSkeletonMethodBody });
            constructorList.Add(makeSkeletonMethodDecl);

            return constructorList;
        }


        private SyntaxNode MkStaticAppConstructor(string testName)
        {

            
            var stmtList = new List<StatementSyntax>();

            //Initialize types and events
            var nodes = dependsOn.Keys.Select(s => s.ToLower()).ToList();
            foreach(var files in dependsOn.Values)
            {
                foreach(var f in files)
                {
                    if(!nodes.Contains(f))
                    {
                        nodes.Add(f);
                    }
                }
            }
            var edges = new List<Tuple<string, string>>();
            foreach (var file in dependsOn)
            {
                foreach (var dep in file.Value)
                {
                    if (file.Key != dep)
                    {
                        edges.Add(new Tuple<string, string>(dep.ToLower(), file.Key.ToLower()));
                    }
                }
            }
            List<string> topoOrder = TopologicalSortFiles<string>(nodes, edges);
            //make invocations to the initialization of all types
            foreach (var fName in topoOrder)
            {
                stmtList.Add(
                    ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Types"), IdentifierName("Types_" + fName))
                    )));
            }

            //make invocation to the initialization of all events
            foreach (var fName in topoOrder)
            {
                stmtList.Add(
                    ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Events"), IdentifierName("Events_" + fName))
                    )));
            }

            //Initialize all the maps

            //safe map
            foreach (var sm in allTests[testName].isSafeMap)
            {
                var safeadd = CSharpHelper.MkCSharpInvocationExpression(
                    CSharpHelper.MkCSharpDot("isSafeMap", "Add"),
                    CSharpHelper.MkCSharpStringLiteralExpression(sm.Key),
                    sm.Value ? CSharpHelper.MkCSharpTrueLiteralExpression() : CSharpHelper.MkCSharpFalseLiteralExpression()
                    );
                stmtList.Add(ExpressionStatement(safeadd));
            }

            //rename map
            foreach (var rename in allTests[testName].renameMap)
            {
                var renameadd = CSharpHelper.MkCSharpInvocationExpression(
                    CSharpHelper.MkCSharpDot("renameMap", "Add"),
                    CSharpHelper.MkCSharpStringLiteralExpression(rename.Key),
                    CSharpHelper.MkCSharpStringLiteralExpression(rename.Value)
                    );
                stmtList.Add(ExpressionStatement(renameadd));
            }
            //create map
            var listOfSpecMachineNames = allTests[testName].renameMap.Where(item => allTests[testName].specMachineMap.ContainsKey(item.Key)).Select(item => item.Value).Distinct();
            var listOfMachineNames = allTests[testName].renameMap.Where(item => !allTests[testName].specMachineMap.ContainsKey(item.Key)).Select(item => item.Value).Distinct();
            foreach (var specMachine in listOfSpecMachineNames)
            {
                var createadd = CSharpHelper.MkCSharpInvocationExpression(
                CSharpHelper.MkCSharpDot("createSpecMap", "Add"),
                CSharpHelper.MkCSharpStringLiteralExpression(specMachine),
                IdentifierName(string.Format("CreateSpecMachine_{0}", specMachine)));
                stmtList.Add(ExpressionStatement(createadd));
            }
            foreach (var machine in listOfMachineNames)
            {
                //its a machine
                var createadd = CSharpHelper.MkCSharpInvocationExpression(
                CSharpHelper.MkCSharpDot("createMachineMap", "Add"),
                CSharpHelper.MkCSharpStringLiteralExpression(machine),
                IdentifierName(string.Format("CreateMachine_{0}", machine))
                );
                stmtList.Add(ExpressionStatement(createadd));
 
            }
            //interface map
            foreach (var it in allTests[testName].interfaceMap)
            {
                var eventsParams = new SeparatedSyntaxList<ExpressionSyntax>();
                foreach (var ev in it.Value)
                {
                    eventsParams = eventsParams.Add(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Events"), IdentifierName(String.Format("event_{0}", ev))));
                }
                var initStmt = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                    ElementAccessExpression(
                        IdentifierName("interfaceMap"),
                        BracketedArgumentList(SingletonSeparatedList<ArgumentSyntax>(Argument(CSharpHelper.MkCSharpStringLiteralExpression(it.Key))))
                    ),
                    ObjectCreationExpression(
                        GenericName(Identifier("List"), TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName("PrtEventValue")))),
                        ArgumentList(),
                        InitializerExpression(SyntaxKind.CollectionInitializerExpression, eventsParams))
                );
                stmtList.Add(initStmt);
            }

            //send actions
            {
                var eventsParams = new SeparatedSyntaxList<ExpressionSyntax>();
                foreach (var ev in allTests[testName].sendActions)
                {
                    eventsParams = eventsParams.Add(CSharpHelper.MkCSharpStringLiteralExpression(ev));
                }
                var initStmt = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                    IdentifierName("visibleEvents"),
                    ObjectCreationExpression(
                        GenericName(Identifier("List"), TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName("string")))),
                        ArgumentList(),
                        InitializerExpression(SyntaxKind.CollectionInitializerExpression, eventsParams))
                );
                stmtList.Add(initStmt);
            }

            //interface actions
            {
                var interfaceParams = new SeparatedSyntaxList<ExpressionSyntax>();
                foreach (var it in allTests[testName].createActions)
                {
                    interfaceParams = interfaceParams.Add(CSharpHelper.MkCSharpStringLiteralExpression(it));
                }
                var initStmt = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                    IdentifierName("visibleInterfaces"),
                    ObjectCreationExpression(
                        GenericName(Identifier("List"), TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName("string")))),
                        ArgumentList(),
                        InitializerExpression(SyntaxKind.CollectionInitializerExpression, interfaceParams))
                );
                stmtList.Add(initStmt);
            }

            //initialize the interface types
            foreach (var it in allTests[testName].interfaceMap)
            {
                var initStmt = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ParenthesizedExpression(
                    CSharpHelper.MkCSharpCastExpression("PrtInterfaceType",
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Types"), IdentifierName(String.Format("type_{0}", it.Key))))),
                    IdentifierName("permissions")),
                    ElementAccessExpression(
                        IdentifierName("interfaceMap"),
                        BracketedArgumentList(SingletonSeparatedList<ArgumentSyntax>(Argument(CSharpHelper.MkCSharpStringLiteralExpression(it.Key))))
                    )
                );
                stmtList.Add(initStmt);
            }

            //spec machine map
            foreach (var specMachine in allTests[testName].specMachineMap)
            {
                SeparatedSyntaxList<ExpressionSyntax> machines = new SeparatedSyntaxList<ExpressionSyntax>();
                machines = machines.AddRange(specMachine.Value.Select(x => CSharpHelper.MkCSharpStringLiteralExpression(x)));

                var addstmt =
                    CSharpHelper.MkCSharpInvocationExpression(
                        CSharpHelper.MkCSharpDot("specMachineMap", "Add"),
                        CSharpHelper.MkCSharpStringLiteralExpression(specMachine.Key),
                        ObjectCreationExpression(
                            CSharpHelper.MkCSharpGenericListType(IdentifierName("string")),
                            ArgumentList(),
                            InitializerExpression(SyntaxKind.CollectionInitializerExpression, machines))
                        );
                stmtList.Add(ExpressionStatement(addstmt));
            }
            //link map
            //declare temp variable 
            var dictStringString = 
                GenericName(
                    Identifier("Dictionary"),
                    TypeArgumentList(SeparatedList<TypeSyntax>(
                        new SyntaxNodeOrToken[]{
                                PredefinedType(
                                    Token(SyntaxKind.StringKeyword)),
                                Token(SyntaxKind.CommaToken),
                                PredefinedType(
                                    Token(SyntaxKind.StringKeyword))}))
                );
            stmtList.Add(
                LocalDeclarationStatement(
                    VariableDeclaration(
                        dictStringString,
                        SingletonSeparatedList<VariableDeclaratorSyntax>(
                            VariableDeclarator(
                                Identifier("_temp"))
                        )  
                    )
                )
            );
            foreach(var creatorM in allTests[testName].linkMap)
            {
                var tempInit = 
                    CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                            IdentifierName("_temp"),
                            CSharpHelper.MkCSharpObjectCreationExpression(dictStringString)
                    );
                stmtList.Add(tempInit);
                foreach(var linkmapitem in creatorM.Value)
                {
                    stmtList.Add(
                        ExpressionStatement(
                        CSharpHelper.MkCSharpInvocationExpression(
                            CSharpHelper.MkCSharpDot("_temp", "Add"),
                            CSharpHelper.MkCSharpStringLiteralExpression(linkmapitem.Key),
                            CSharpHelper.MkCSharpStringLiteralExpression(linkmapitem.Value)
                            ))
                        );
                }

                var addLinkItem = ExpressionStatement(
                    CSharpHelper.MkCSharpInvocationExpression(
                        CSharpHelper.MkCSharpDot("linkMap", "Add"),
                        CSharpHelper.MkCSharpStringLiteralExpression(creatorM.Key),
                        IdentifierName("_temp"))
                    );
                stmtList.Add(addLinkItem);
            }



            

            var staticConstrutor = ConstructorDeclaration(
                                        Identifier("Application"))
                                    .WithModifiers(
                                        TokenList(
                                            Token(SyntaxKind.StaticKeyword)))
                                    .WithBody(Block(stmtList))
                                    .NormalizeWhitespace();
            return staticConstrutor;
        }

        #region Topological Sorting Dependencies
        /// <summary>
        /// Topological Sorting (Kahn's algorithm) 
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Topological_sorting</remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="nodes">All nodes of directed acyclic graph.</param>
        /// <param name="edges">All edges of directed acyclic graph.</param>
        /// <returns>Sorted node in topological order.</returns>
        static List<T> TopologicalSortFiles<T>(List<T> nodes, List<Tuple<T, T>> edges) where T : IEquatable<T>
        {
            // Empty list that will contain the sorted elements
            var L = new List<T>();

            // Set of all nodes with no incoming edges
            var S = new HashSet<T>(nodes.Where(n => edges.All(e => e.Item2.Equals(n) == false)));

            // while S is non-empty do
            while (S.Any())
            {

                //  remove a node n from S
                var n = S.First();
                S.Remove(n);

                // add n to tail of L
                L.Add(n);

                // for each node m with an edge e from n to m do
                foreach (var e in edges.Where(e => e.Item1.Equals(n)).ToList())
                {
                    var m = e.Item2;

                    // remove edge e from the graph
                    edges.Remove(e);

                    // if m has no other incoming edges then
                    if (edges.All(me => me.Item2.Equals(m) == false))
                    {
                        // insert m into S
                        S.Add(m);
                    }
                }
            }

            // if graph has edges then
            if (edges.Any())
            {
                // return error (graph has at least one cycle)
                return null;
            }
            else
            {
                // return L (a topologically sorted order)
                return L;
            }
        }
        #endregion

        public void GenerateCSharpLinkerOutput(string outputDir)
        {
            foreach(var testCase in allTests)
            {
                //make sure test case has a main file
                if(!testCase.Value.renameMap.ContainsKey("Main"))
                {
                    Log.WriteMessage(string.Format("No Main Machine, cannot generate {0}.dll", testCase.Key), SeverityKind.Error);
                    return;
                }
                SyntaxNode finalOutput = null;

                var workspace = new AdhocWorkspace();
                // Get the SyntaxGenerator for the specified language
                generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);

                var members = new List<SyntaxNode>();
                // Create using/Imports directives
                var usingDirectives = generator.NamespaceImportDeclaration("System");

                members.Add(MkEvents());
                members.AddRange(MkAppConstructors(testCase.Key));
                members.Add(MkStaticAppConstructor(testCase.Key));

                //create the class
                var applicationcClassDeclaration = generator.ClassDeclaration(
                  "Application", typeParameters: null,
                  modifiers: DeclarationModifiers.Partial,
                  accessibility: Accessibility.Public,
                  baseType: generator.IdentifierName("StateImpl"),
                  members: members);
                var programNameSpaceDeclaration = generator.NamespaceDeclaration(
                    IdentifierName("P.Program"),
                    UsingDirective(IdentifierName("P.Runtime")),
                    UsingDirective(IdentifierName("System")),
                    UsingDirective(IdentifierName("System.Collections.Generic")), 
                    applicationcClassDeclaration);

                // Get a CompilationUnit (code file) for the generated code
                finalOutput = generator.CompilationUnit(
                            generator.NamespaceImportDeclaration("P.Runtime"),
                            generator.NamespaceImportDeclaration("System"),
                            generator.NamespaceImportDeclaration("System.Collections.Generic"),
                            programNameSpaceDeclaration).
                                NormalizeWhitespace();
                var outputFile = Path.Combine(outputDir, testCase.Key + ".cs");
                EmitLinkerCS(finalOutput, outputFile);
                Log.WriteMessage(string.Format("Writing {0}.cs ...", testCase.Key), SeverityKind.Info);
                EmitCSDll(outputDir, testCase.Key);
            }
        }

        private void EmitCSDll(string outputDir, string testCaseName)
        {
            List<string> allCSFiles = new List<string>();

            allCSFiles.Add(Path.Combine(outputDir, testCaseName + ".cs"));
            allCSFiles.AddRange(inputFiles.Select(fileName => Path.Combine(outputDir, fileName + ".cs")).ToList());
            
            string cs_code = "";
            foreach(var file in allCSFiles)
            {
                if (!File.Exists(file))
                {
                    //Log.WriteMessage(string.Format("{0} not found, recompile the corresponding P file", file), SeverityKind.Warning);
                    return;
                }
                using (var sr = new StreamReader(file))
                {
                    cs_code = string.Concat(cs_code, sr.ReadToEnd());
                }
            }

            Log.WriteMessage(string.Format("Writing {0}.dll ...", testCaseName), SeverityKind.Info);
            var tree = CSharpSyntaxTree.ParseText(cs_code);

            var pruntime = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\prt.dll";
            if(!File.Exists(pruntime))
            {
                Log.WriteMessage(string.Format("could not find file {0}", pruntime), SeverityKind.Error);
                return;
            }
            CSharpCompilation compilation = CSharpCompilation.Create(
            testCaseName,
            new[] { tree },
            new [] {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(pruntime),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


            var outputDll = Path.Combine(outputDir, testCaseName + ".dll");
            var outputPdb = Path.Combine(outputDir, testCaseName + ".pdb");
            var emitResult = compilation.Emit(outputDll, outputPdb);

            //If our compilation failed, we can discover exactly why.
            if (!emitResult.Success)
            {
                Log.WriteMessage(string.Format("C# file corresponding to dll generated : {0}_dllerror.cs", testCaseName), SeverityKind.Error);
                using (var sw = new StreamWriter(Path.Combine(outputDir, testCaseName + "_dllerror.cs")))
                {
                    sw.WriteLine(tree);
                }
                foreach (var diagnostic in emitResult.Diagnostics)
                {
                    Log.WriteMessage(diagnostic.ToString(), SeverityKind.Error);
                }
                return;
            }
        }
        private void EmitLinkerCS(SyntaxNode finalOutput, string fileName)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(fileName);
            file.WriteLine("#pragma warning disable CS0162, CS0164, CS0168");
            file.WriteLine(finalOutput);
            file.Close();
        }
    }
}

