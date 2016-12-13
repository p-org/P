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
        public PToCSharp(Compiler compiler, AST<Model> modelWithTypes, Dictionary<int, SourceInfo> idToSourceInfo, string csharpFileName)
            : base(compiler, modelWithTypes, idToSourceInfo)
        {
            this.csharpFileName = csharpFileName;
            this.typeContext = new TypeTranslationContext(this);
            GenerateTypeInfo(modelWithTypes);
        }

        #region CSharpCompiler
        string csharpFileName;
        //for storing members of the Application class:
        static List<SyntaxNode> members = new List<SyntaxNode>();
        //final C# program:
        SyntaxNode result = null;
        static SyntaxGenerator generator;

        #region Types
        void GenerateTypeInfo(AST<Model> model)
        {
            var factBins = new Dictionary<string, LinkedList<AST<FuncTerm>>>();
            model.FindAll(
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

            var terms = GetBin(factBins, "TypeOf");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    FuncTerm typingContext = (FuncTerm)it.Current;
                    it.MoveNext();
                    var expr = Factory.Instance.ToAST(it.Current);
                    it.MoveNext();
                    var type = it.Current as FuncTerm;
                    if (type == null) continue;

                    string typingContextKind = ((Id)typingContext.Function).Name;
                    if (typingContextKind == "FunDecl")
                    {
                        string ownerName = GetOwnerName(typingContext, 1, 0);
                        string funName = GetName(typingContext, 0);
                        if (ownerName == null)
                        {
                            allStaticFuns[funName].typeInfo[expr] = type;
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
                        string funName = anonFunToName[Factory.Instance.ToAST(typingContext)];
                        if (ownerName == null)
                        {
                            allStaticFuns[funName].typeInfo[expr] = type;
                        }
                        else
                        {
                            allMachines[ownerName].funNameToFunInfo[funName].typeInfo[expr] = type;
                        }
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

            terms = GetBin(factBins, "LinkMap");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var createdIorM = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var createdM = ((Cnst)it.Current).GetStringValue();
                    linkMap.Add(createdIorM, createdM);
                }
            }

            terms = GetBin(factBins, "MaxNumLocals");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    FuncTerm typingContext = (FuncTerm)it.Current;
                    string typingContextKind = ((Id)typingContext.Function).Name;
                    if (!(typingContextKind == "FunDecl" || typingContextKind == "AnonFunDecl")) continue;
                    it.MoveNext();
                    var maxNumLocals = (int)((Cnst)it.Current).GetNumericValue().Numerator;

                    if (typingContextKind == "FunDecl")
                    {
                        string ownerName = GetOwnerName(typingContext, 1, 0);
                        string funName = GetName(typingContext, 0);
                        if (ownerName == null)
                        {
                            allStaticFuns[funName].maxNumLocals = maxNumLocals;
                        }
                        else
                        {
                            allMachines[ownerName].funNameToFunInfo[funName].maxNumLocals = maxNumLocals;
                        }
                    }
                    else
                    {
                        // typingContextKind == "AnonFunDecl"
                        string ownerName = GetOwnerName(typingContext, 0, 0);
                        string funName = anonFunToName[Factory.Instance.ToAST(typingContext)];
                        if (ownerName == null)
                        {
                            allStaticFuns[funName].maxNumLocals = maxNumLocals;
                        }
                        else
                        {
                            allMachines[ownerName].funNameToFunInfo[funName].maxNumLocals = maxNumLocals;
                        }
                    }
                }
            }
        }
        TypeTranslationContext typeContext;
        internal class TypeTranslationContext
        {
            private int fieldCount;
            private int typeCount;

            //This field is for emitting types; order is important
            public List<StatementSyntax> typeInitialization;
            public List<FieldDeclarationSyntax> typeDeclaration;
            private Dictionary<AST<Node>, SyntaxNode> pTypeToCSharpExpr;
            private PToCSharp pToCSharp;

            public TypeTranslationContext(PToCSharp pToCSharp)
            {
                this.pToCSharp = pToCSharp;
                fieldCount = 0;
                typeCount = 0;
                typeDeclaration = new List<FieldDeclarationSyntax>();
                typeInitialization = new List<StatementSyntax>();
                pTypeToCSharpExpr = new Dictionary<AST<Node>, SyntaxNode>();
            }

            private SyntaxNode GetType(string typeName)
            {
                var retVal = MkCSharpIdentifierName(typeName);
                typeCount++;
                return retVal;
            }

            private void AddTypeInitialization(SyntaxNode n)
            {
                typeInitialization.Add((StatementSyntax)n);
            }

            private void AddTypeDeclaration(SyntaxNode n)
            {
                typeDeclaration.Add((FieldDeclarationSyntax)n);
            }

            public SyntaxNode PTypeToCSharpExpr(FuncTerm pType)
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
                if (!pTypeToCSharpExpr.ContainsKey(typeAST))
                {
                    pTypeToCSharpExpr[typeAST] = pTypeToCSharpExpr[eTypeAST];
                }
            }

            private SyntaxNode ConstructType(FuncTerm type)
            {
                string typeKind = ((Id)type.Function).Name;

                //Debug only:
                //Console.WriteLine("typeKind in ConstructType: {0}", typeKind);

                if (typeKind == "BaseType")
                {
                    var primitiveType = ((Id)GetArgByIndex(type, 0)).Name;
                    if (primitiveType == "NULL")
                    {
                        var tmpVar = GetType("typeNull");
                        AddTypeInitialization(MkCSharpSimpleAssignmentExpressionStatement(tmpVar, MkCSharpObjectCreationExpression(IdentifierName("PrtNullType"))));
                        AddTypeDeclaration(MkCSharpFieldDeclaration(IdentifierName("PrtType"), "typeNull", Token(SyntaxKind.PublicKeyword),
                                                                    Token(SyntaxKind.StaticKeyword)));
                        return tmpVar;
                    }
                    else if (primitiveType == "BOOL")
                    {
                        var tmpVar = GetType("typeBool");
                        AddTypeInitialization(MkCSharpSimpleAssignmentExpressionStatement(tmpVar, MkCSharpObjectCreationExpression(IdentifierName("PrtBoolType"))));
                        AddTypeDeclaration(MkCSharpFieldDeclaration(IdentifierName("PrtType"), "typeBool", Token(SyntaxKind.PublicKeyword),
                                                                   Token(SyntaxKind.StaticKeyword)));
                        return tmpVar;
                    }
                    else if (primitiveType == "INT")
                    {
                        var tmpVar = GetType("typeInt");
                        AddTypeInitialization(MkCSharpSimpleAssignmentExpressionStatement(tmpVar, MkCSharpObjectCreationExpression(IdentifierName("PrtIntType"))));
                        AddTypeDeclaration(MkCSharpFieldDeclaration(IdentifierName("PrtType"), "typeInt", Token(SyntaxKind.PublicKeyword),
                                                                   Token(SyntaxKind.StaticKeyword)));
                        return tmpVar;
                    }
                    else if (primitiveType == "EVENT")
                    {
                        var tmpVar = GetType("typeEvent");
                        AddTypeInitialization(MkCSharpSimpleAssignmentExpressionStatement(tmpVar, MkCSharpObjectCreationExpression(IdentifierName("PrtEventType"))));
                        AddTypeDeclaration(MkCSharpFieldDeclaration(IdentifierName("PrtType"), "typeEvent", Token(SyntaxKind.PublicKeyword),
                                                                   Token(SyntaxKind.StaticKeyword)));
                        return tmpVar;
                    }
                    else if (primitiveType == "MACHINE")
                    {
                        var tmpVar = GetType("typeMachine");
                        AddTypeInitialization(MkCSharpSimpleAssignmentExpressionStatement(tmpVar, MkCSharpObjectCreationExpression(IdentifierName("PrtMachineType"))));
                        AddTypeDeclaration(MkCSharpFieldDeclaration(IdentifierName("PrtType"), "typeMachine", Token(SyntaxKind.PublicKeyword),
                                                                   Token(SyntaxKind.StaticKeyword)));
                        return tmpVar;
                    }
                    else
                    {
                        Debug.Assert(primitiveType == "ANY", "Illegal BaseType");
                        var tmpVar = GetType("typeAny");
                        AddTypeInitialization(MkCSharpSimpleAssignmentExpressionStatement(tmpVar, MkCSharpObjectCreationExpression(IdentifierName("PrtAnyType"))));
                        AddTypeDeclaration(MkCSharpFieldDeclaration(IdentifierName("PrtType"), "typeAny", Token(SyntaxKind.PublicKeyword),
                                                                   Token(SyntaxKind.StaticKeyword)));
                        return tmpVar;
                    }
                }
                else if (typeKind == "NameType")
                {
                    //NameType is for enum values only; "synonym" types are expanded by the compiler
                    //TODO(expand): there's no ref to the enum type left in C# code, which would not allow
                    //to treat casts (enum->int) properly; 
                    //therefore, code below would have to be changed, after PrtEnumType is defined in PrtTypes.cs.
                    string typeName = "typeNameType_" + typeCount;
                    var tmpVar = GetType(typeName);
                    AddTypeInitialization(MkCSharpSimpleAssignmentExpressionStatement(tmpVar, MkCSharpObjectCreationExpression(IdentifierName("PrtIntType"))));
                    AddTypeDeclaration(MkCSharpFieldDeclaration(IdentifierName("PrtType"), typeName, Token(SyntaxKind.PublicKeyword),
                                                                   Token(SyntaxKind.StaticKeyword)));
                    return tmpVar;
                }
                else if (typeKind == "TupType")
                {
                    List<SyntaxNode> memberTypes = new List<SyntaxNode>();
                    while (type != null)
                    {
                        memberTypes.Add(PTypeToCSharpExpr((FuncTerm)GetArgByIndex(type, 0)));
                        type = GetArgByIndex(type, 1) as FuncTerm;
                    }
                    string typeName = "typeTupType_" + typeCount;
                    var tmpVar = GetType(typeName);
                    //TODO(improve): create a generic method for inserting CommaToken into a generic list 
                    List<SyntaxNodeOrToken> initializer = new List<SyntaxNodeOrToken>();
                    foreach (var memberType in memberTypes)
                    {
                        initializer.Add(memberType);
                        initializer.Add(Token(SyntaxKind.CommaToken));
                    }
                    initializer.RemoveAt(initializer.Count() - 1);
                    AddTypeInitialization(MkCSharpSimpleAssignmentExpressionStatement(tmpVar, MkCSharpObjectCreationExpression(IdentifierName("PrtTupleType"),
                        MkCSharpArrayCreationExpression("PrtType", initializer.ToArray()))));
                    AddTypeDeclaration(MkCSharpFieldDeclaration(IdentifierName("PrtTupleType"), typeName, Token(SyntaxKind.PublicKeyword),
                                                                   Token(SyntaxKind.StaticKeyword)));
                    return tmpVar;

                }
                else if (typeKind == "NmdTupType")
                {
                    List<SyntaxNode> memberNames = new List<SyntaxNode>();
                    List<SyntaxNode> memberTypes = new List<SyntaxNode>();

                    while (type != null)
                    {
                        var typeField = (FuncTerm)GetArgByIndex(type, 0);
                        string nameField = ((Cnst)GetArgByIndex(typeField, 0)).GetStringValue();
                        memberNames.Add(MkCSharpStringLiteralExpression(nameField));
                        memberTypes.Add(PTypeToCSharpExpr((FuncTerm)GetArgByIndex(typeField, 1)));
                        type = GetArgByIndex(type, 1) as FuncTerm;
                    }
                    string typeName = "typeNmdTupType_" + typeCount;
                    var tmpVar = GetType(typeName);

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

                    AddTypeInitialization(MkCSharpSimpleAssignmentExpressionStatement(tmpVar, MkCSharpObjectCreationExpression(IdentifierName("PrtNamedTupleType"),
                        MkCSharpArrayCreationExpression("object", initializer.ToArray()))));
                    AddTypeDeclaration(MkCSharpFieldDeclaration(IdentifierName("PrtNamedTupleType"), typeName, Token(SyntaxKind.PublicKeyword),
                                                                   Token(SyntaxKind.StaticKeyword)));
                    return tmpVar;
                }
                else if (typeKind == "SeqType")
                {
                    SyntaxNode innerType = PTypeToCSharpExpr((FuncTerm)GetArgByIndex(type, 0));
                    string typeName = "typeSeqType_" + typeCount;
                    var tmpVar = GetType(typeName);
                    AddTypeInitialization(MkCSharpSimpleAssignmentExpressionStatement(tmpVar, MkCSharpObjectCreationExpression(IdentifierName("PrtSeqType"), innerType)));
                    AddTypeDeclaration(MkCSharpFieldDeclaration(IdentifierName("PrtSeqType"), typeName, Token(SyntaxKind.PublicKeyword),
                                                                   Token(SyntaxKind.StaticKeyword)));
                    return tmpVar;
                }
                else
                {
                    // typeKind == "MapType"
                    SyntaxNode keyType = PTypeToCSharpExpr((FuncTerm)GetArgByIndex(type, 0));
                    SyntaxNode valType = PTypeToCSharpExpr((FuncTerm)GetArgByIndex(type, 1));
                    string typeName = "typeMapType_" + typeCount;
                    var tmpVar = GetType(typeName);
                    AddTypeInitialization(MkCSharpSimpleAssignmentExpressionStatement(tmpVar, MkCSharpObjectCreationExpression(IdentifierName("PrtMapType"), keyType, valType)));
                    AddTypeDeclaration(MkCSharpFieldDeclaration(IdentifierName("PrtMapType"), typeName, Token(SyntaxKind.PublicKeyword),
                                                                   Token(SyntaxKind.StaticKeyword)));
                    return tmpVar;
                }
            }
        }
        
        #endregion

        #region Utilities
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
            return GotoStatement(SyntaxKind.GotoStatement, MkCSharpIdentifierName(label));
        }
        public static IdentifierNameSyntax MkCSharpIdentifierName(string name)
        {
            return IdentifierName(name);
        }
        public static SyntaxToken MkCSharpIdentifier(string name)
        {
            return Identifier(name);
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
        public static List<SyntaxNodeOrToken> MkCSharpArgumentList(params ArgumentSyntax[] args)
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

        #region types
        public static SimpleBaseTypeSyntax MkCSharpIdentifierNameType(string type)
        {
            return SimpleBaseType((TypeSyntax)MkCSharpIdentifierName(type));
        }
        public static TypeSyntax MkCSharpGenericListType(TypeSyntax type)
        {
            return GenericName(
                            Identifier("List"))
                   .WithTypeArgumentList(
                            TypeArgumentList(
                                SingletonSeparatedList<TypeSyntax>(
                                    type)));
        }
        #endregion
        public static SyntaxNode MkCSharpFieldDeclaration(SyntaxNode type,
                        string name, SyntaxToken accessibility, SyntaxToken publicStatic)
        {
            var nameDecl = VariableDeclarator(MkCSharpIdentifier("" + name + ""));
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

            Debug.Assert(names.Length > 0);

            ExpressionSyntax lhs = IdentifierName(first);
            for (int i = 0; i < names.Length; i++)
            {
                SimpleNameSyntax rhs = IdentifierName(names[i]);
                lhs = MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            lhs,
                            rhs);
            }
            return lhs.NormalizeWhitespace();
        }
        public static MemberAccessExpressionSyntax MkCSharpDot(ExpressionSyntax first, string second)
        {
            return MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ParenthesizedExpression(first),
                        (SimpleNameSyntax)MkCSharpIdentifierName(second));
        }
        public static SyntaxNode MkCSharpElementAccessExpression(SyntaxNode first, int index)
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
        public static SyntaxNode MkCSharpElementAccessExpression(SyntaxNode first, SyntaxNode index)
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
                         SingletonList<ArrayRankSpecifierSyntax>(
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
                List<SyntaxNodeOrToken> hdWithCommas = MkCSharpParameterList(hd);
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
        public static StatementSyntax MkCSharpPrint(string msg)
        {
            return ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("Console", "Write"), Argument(MkCSharpStringLiteralExpression(msg))));
        }
        public static InvocationExpressionSyntax MkCSharpInvocationExpression(SyntaxNode first, params ArgumentSyntax[] pars)
        {
            var args = MkCSharpArgumentList(pars);
            return InvocationExpression((ExpressionSyntax)first)
                    .WithArgumentList(
                         ArgumentList(
                                SeparatedList<ArgumentSyntax>(args)));
        }
        public static PrefixUnaryExpressionSyntax MkCSharpUnaryExpression(SyntaxKind op, SyntaxNode arg)
        {
            return PrefixUnaryExpression(op, (ExpressionSyntax)arg);
        }
        public static BinaryExpressionSyntax MkCSharpBinaryExpression(SyntaxKind op, SyntaxNode left, SyntaxNode right)
        {
            //TODO(fix) For binary plus for now:
            return BinaryExpression(SyntaxKind.AddExpression, (ExpressionSyntax)left, (ExpressionSyntax)right);
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
            List<SyntaxNodeOrToken> pars = MkCSharpParameterList(methodPars);
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
            List<SyntaxNodeOrToken> pars = MkCSharpParameterList(constrPars);
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
        public static BlockSyntax MkCSharpLabeledBlock(string label, StatementSyntax body)
        {
            return Block(SingletonList<StatementSyntax>(
                            LabeledStatement(
                                Identifier(label),
                                body)));
        }
        public static LabeledStatementSyntax MkCSharpEmptyLabeledStatement(string label)
        {
            return LabeledStatement(
                            Identifier(label),
                            EmptyStatement());
        }
        #endregion
        public bool GenerateCSharp()
        {
            if (!allMachines.ContainsKey("Main"))
            {
                Console.WriteLine("Unable to generate CSharp code since Main machine is absent.");
                return false;
            }
            var workspace = new AdhocWorkspace();

            // Get the SyntaxGenerator for the specified language
            generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);


            // Create using/Imports directives
            var usingDirectives = generator.NamespaceImportDeclaration("System");


            List<AST<Node>> elements = new List<AST<Node>>();
            MkAppConstructors();
            MkEvents();
            MkTypes();
            MkStaticAppConstructor();
            //generated static anon function name is: 
            //var funName = "AnonFunStatic" + anonFunCounterStatic;
            //In PToZing.cs:
            //string funName = anonFunToName[Factory.Instance.ToAST(fun)];
            //, where fun is a Node.
            MkStaticFunctions();
            MkOtherAppFields();
            MkMachineClasses();
            //MkMonitorClasses(elements, workspace, generator);
            MkCSharpOutput();
            EmitCSharpOutput(csharpFileName);
            return true;
        }
        private void MkAppConstructors()
        {
            //parameterless constructor
            var constructor_1 = generator.ConstructorDeclaration("Application", null, Accessibility.Public, baseConstructorArguments: new SyntaxNode[0]);
            members.Add(constructor_1);

            var constructorParameters = new SyntaxNode[] {
                generator.ParameterDeclaration("initialize",
                    generator.TypeExpression(SpecialType.System_Boolean)) };
            var constructorBody = generator.ExpressionStatement(generator.InvocationExpression(generator.IdentifierName("CreateMainMachine")));
            var constructor_2 = generator.ConstructorDeclaration("Application", constructorParameters, Accessibility.Public, baseConstructorArguments: new SyntaxNode[0],
                                                                statements: new SyntaxNode[] { constructorBody });
            members.Add(constructor_2);
            //Generate "new Application();" 
            var makeSkeletonMethodBody = generator.ReturnStatement(generator.ObjectCreationExpression(generator.IdentifierName("Application")));
            var makeSkeletonMethodDecl = generator.MethodDeclaration("MakeSkeleton", null,
              null, generator.IdentifierName("StateImpl"),
              Accessibility.Public,
              DeclarationModifiers.Override,
              new SyntaxNode[] { makeSkeletonMethodBody });
            members.Add(makeSkeletonMethodDecl);
        }
        private void MkEvents()
        {
            foreach (var pair in allEvents)
            {
                SyntaxNode payloadType = typeContext.PTypeToCSharpExpr((pair.Value).payloadType);
                SyntaxNode maxInstances;
                if ((pair.Value).maxInstances == -1)
                {
                    String s = "PrtEvent.DefaultMaxInstances";
                    maxInstances = IdentifierName(s);
                }
                else
                {
                    maxInstances = MkCSharpNumericLiteralExpression((pair.Value).maxInstances);
                }

                SyntaxNode doAssume;
                if ((pair.Value).maxInstancesAssumed)
                {
                    doAssume = MkCSharpTrueLiteralExpression();
                }
                else
                {
                    doAssume = MkCSharpFalseLiteralExpression();
                }

                //Example: public static PrtEventValue halt;
                //public static PrtEventValue @null;
                string eventNameOrNull = pair.Key;
                if (pair.Key == NullEvent)
                {
                    //"@null" is used as event name for the null event; this is done to disambiguate null as a C# keyword
                    //output with "@null" compiles
                    eventNameOrNull = "@" + eventNameOrNull;
                }
                    members.Add(
                    MkCSharpFieldDeclaration(IdentifierName("PrtEventValue"),
                        eventNameOrNull,
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.StaticKeyword)
                        )
                    );
            }
        }
        private void MkTypes()
        {
            members.AddRange(typeContext.typeDeclaration);
        }
        private void MkStaticAppConstructor()
        {
            List<StatementSyntax> inits = new List<StatementSyntax>();

            //initialize types:
            inits.AddRange(typeContext.typeInitialization);

            //initialize events:
            foreach (var pair in allEvents)
            {
                SyntaxNode payloadType = typeContext.PTypeToCSharpExpr((pair.Value).payloadType);
                SyntaxNode maxInstances;
                if ((pair.Value).maxInstances == -1)
                {
                    String s = "PrtEvent.DefaultMaxInstances";
                    maxInstances = IdentifierName(s);
                }
                else
                {
                    maxInstances = MkCSharpNumericLiteralExpression((pair.Value).maxInstances);
                }

                SyntaxNode doAssume;
                if ((pair.Value).maxInstancesAssumed)
                {
                    doAssume = MkCSharpTrueLiteralExpression();
                }
                else
                {
                    doAssume = MkCSharpFalseLiteralExpression();
                }

                //Example: halt = new PrtEventValue(new PrtEvent("halt", typeNull, 1, false));
                string eventNameOrNull = pair.Key;
                if (pair.Key == NullEvent)
                {
                    //"@null" is used as event name for the null event; this is done to disambiguate null as a C# keyword
                    //output with "@null" compiles
                    eventNameOrNull = "@" + eventNameOrNull;
                }
                SyntaxNode lhs = MkCSharpIdentifierName(eventNameOrNull);
                SyntaxNode rhs = MkCSharpObjectCreationExpression(
                    MkCSharpIdentifierName("PrtEventValue"),
                    MkCSharpObjectCreationExpression(
                        MkCSharpIdentifierName("PrtEvent"),
                        MkCSharpStringLiteralExpression(pair.Key),
                        payloadType,
                        maxInstances,
                        doAssume
                    ));
                inits.Add((StatementSyntax)MkCSharpSimpleAssignmentExpressionStatement(lhs, rhs));
            }
          
            members.Add(ConstructorDeclaration(
                               MkCSharpIdentifier("Application"))
                            .WithModifiers(
                               TokenList(
                                    Token(SyntaxKind.StaticKeyword)))
                            .WithBody(Block(inits))
                            .NormalizeWhitespace());
        }
        internal partial class MkFunctionDecl
        {
            //funName is former entityName
            public string funName;
            public string funType;
            //funInfo is former entityInfo
            public FunInfo funInfo;
            //TODO: might not be needed
            public bool isGlobalStatic;
            //null if global function:
            public MkMachineClass owner;
            //owner.machName is former machineName

            private PToCSharp pToCSharp; 
            public List<Tuple<SyntaxNode, string>> locals;
            public Stack<bool> lhsStack;
            //labels are used for "continuations" in send, new, nondet, receive
            private int labelCount;

            public MkFunctionDecl(string funName, FunInfo funInfo, bool isGlobalStatic, MkMachineClass owner, PToCSharp pToCSharp)
            {
                this.funName = funName;
                this.funType = funName + "_Class";
                this.funInfo = funInfo;
                this.isGlobalStatic = isGlobalStatic;
                this.owner = owner;
                this.pToCSharp = pToCSharp;
                this.locals = new List<Tuple<SyntaxNode, string>>();
                this.lhsStack = new Stack<bool>();
                this.labelCount = 0;
            }
            
            public string GetFreshLabel()
            {
                labelCount++;
                return string.Format("{0}_{1}", funName, labelCount);
            }

            public string GetLabel(int i)
            {
                return string.Format("{0}_{1}", funName, i);
            }

            public SwitchStatementSyntax EmitLabelPrelude()
            {
                SyntaxList<SwitchSectionSyntax> caseList = new SyntaxList<SwitchSectionSyntax>();
                for (int i = 1; i <= labelCount; i++)
                {
                    SyntaxList<SwitchLabelSyntax> switchLabels = new SyntaxList<SwitchLabelSyntax>();
                    switchLabels = switchLabels.Add(CaseSwitchLabel(MkCSharpNumericLiteralExpression(i)));
                    SyntaxList <StatementSyntax> switchStmts = new SyntaxList<StatementSyntax>();
                    switchStmts = switchStmts.Add(MkCSharpGoto(GetLabel(i)));
                    caseList = caseList.Add(SwitchSection(switchLabels, switchStmts));
                }
                return SwitchStatement(MkCSharpDot("currFun", "returnToLocation"), caseList);
            }

            public SyntaxNode GetTmpVar(SyntaxNode type, string baseName)
            {
                var tmpVarName = pToCSharp.GetUnique(baseName);
                var tmpVar = MkCSharpIdentifierName(tmpVarName);
                this.locals.Add(new Tuple<SyntaxNode, string>(type, tmpVarName));
                return tmpVar;
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
                        //PushSideEffectStack();
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
                        //PushSideEffectStack();
                        yield return it.Current;
                        it.MoveNext();
                        //PushSideEffectStack();
                        yield return it.Current;
                    }
                }
                else if (funName == PData.Con_While.Node.Name)
                {
                    using (var it = ft.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        //PushSideEffectStack();
                        yield return it.Current;
                        it.MoveNext();
                        //PushSideEffectStack();
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
                    //return ZingData.Cnst_Nil;
                    //TODO: use "null" instead?
                    return MkCSharpIdentifierName("NIL");

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
                    return MkCSharpIdentifierName("NIL");
                }
                else
                {
                    Console.WriteLine("Unknown term name: " + funName);
                    throw new NotImplementedException();
                }
            }
            private List<SyntaxNode> CaseFunCallHelper(List<string> eventNames, List<string> funNames, string afterAfterLabel)
            {
                throw new NotImplementedException();
            }
            SyntaxNode FoldReceive(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            //In the context of expressions only; no children
            SyntaxNode FoldName(FuncTerm ft, List<SyntaxNode> children)
            {
                SyntaxNode retVal;
                var name = GetName(ft, 0);
                if (funInfo != null && funInfo.localNameToInfo.ContainsKey(name))
                {
                    //local var of a function:
                    LocalVariableInfo entry = (funInfo.localNameToInfo[name]);
                    int ind = entry.index;
                    retVal = (ExpressionSyntax)MkCSharpElementAccessExpression(
                        MkCSharpIdentifierName("locals"), ind);
                }
                else if (owner != null && pToCSharp.allMachines[owner.machName].localVariableToVarInfo.ContainsKey(name))
                {
                    retVal = MkCSharpDot("parent", name);
                }
                else
                {
                    //PrtEvent case: emit "new PrtEventValue(eventStaticVar);"
                    //, where eventStaticVar is eventName
                    var type = LookupType(ft);
                    if (PTypeEvent.Equals(Factory.Instance.ToAST(type)))
                    {
                        retVal = MkCSharpIdentifierName(name);
                    }
                    else
                    {
                        //TODO: check type and add default case to throw an exception
                        //Enum case:
                        retVal = MkCSharpObjectCreationExpression(MkCSharpIdentifierName("PrtIntValue"),
                            MkCSharpIdentifierName(name));
                    }
                }
                return retVal;
            }
            SyntaxNode FoldNewStmt(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            SyntaxNode FoldFunApp(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            SyntaxNode FoldNulApp(FuncTerm ft, List<SyntaxNode> children)
            {
                //No children
                var n = GetArgByIndex(ft, 0);
              
                if (n.NodeKind == NodeKind.Cnst)
                {
                    //Value of the integer:
                    int val = (int)((Cnst)n).GetNumericValue().Numerator;
                    //emit new PrtIntValue(a):
                    return MkCSharpObjectCreationExpression(MkCSharpIdentifierName("PrtIntValue"),
                            MkCSharpNumericLiteralExpression(val));
                }
                // n.NodeKind == NodeKind.Id
                var op = ((Id)n).Name;
                if (op == PData.Cnst_True.Node.Name)
                {
                    return MkCSharpObjectCreationExpression(MkCSharpIdentifierName("PrtBoolValue"),
                            MkCSharpTrueLiteralExpression());
                }
                else if (op == PData.Cnst_False.Node.Name)
                {
                    return MkCSharpObjectCreationExpression(MkCSharpIdentifierName("PrtBoolValue"),
                            MkCSharpFalseLiteralExpression());
                }
                else if (op == PData.Cnst_This.Node.Name)
                {
                    //Owner machine pointer:
                    return MkCSharpObjectCreationExpression(MkCSharpIdentifierName("PrtMachineValue"),
                            MkCSharpCastExpression("PrtImplMachine", MkCSharpIdentifierName("parent")));
                }
                else if (op == PData.Cnst_Nondet.Node.Name || op == PData.Cnst_FairNondet.Node.Name)
                {
                    //TODO(expand): NONDET, FAIRNONDET
                    throw new NotImplementedException();
                }
                else if (op == PData.Cnst_Null.Node.Name)
                {
                    //Constant "@null":
                    return MkCSharpObjectCreationExpression(MkCSharpIdentifierName("PrtEventValue"),
                            MkCSharpIdentifierName("@null"));
                }
                else 
                {
                    //op == PData.Cnst_Halt.Node.Name
                    //Constant "halt":
                    return MkCSharpObjectCreationExpression(MkCSharpIdentifierName("PrtEventValue"),
                            MkCSharpIdentifierName("halt"));
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
                        return MkCSharpUnaryExpression(SyntaxKind.LogicalNotExpression, arg);
                    }
                    else if (op == PData.Cnst_Neg.Node.Name)
                    {
                        return MkCSharpUnaryExpression(SyntaxKind.UnaryMinusExpression, arg);
                    }
                    else if (op == PData.Cnst_Keys.Node.Name)
                    {
                        //TODO(expand):
                        throw new NotImplementedException();
                    }
                    else if (op == PData.Cnst_Values.Node.Name)
                    {
                        //TODO(expand):
                        throw new NotImplementedException();
                    }
                    else
                    {
                        //  op == PData.Cnst_Sizeof.Node.Name
                        //TODO(expand):
                        throw new NotImplementedException();
                    }
                }
            }
            SyntaxNode FoldBinApp(FuncTerm ft, List<SyntaxNode> children)
            {
                var op = ((Id)GetArgByIndex(ft, 0)).Name;
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var arg1 = it.Current;
                    it.MoveNext();
                    var arg2 = it.Current;

                    if (op == PData.Cnst_Add.Node.Name)
                    {
                        return MkCSharpBinaryExpression(SyntaxKind.AddExpression, arg1, arg2);
                    }
                    else if (op == PData.Cnst_Sub.Node.Name)
                    {
                        return MkCSharpBinaryExpression(SyntaxKind.SubtractExpression, arg1, arg2);
                    }
                    else if (op == PData.Cnst_Mul.Node.Name)
                    {
                        return MkCSharpBinaryExpression(SyntaxKind.MultiplyExpression, arg1, arg2);
                    }
                    else if (op == PData.Cnst_IntDiv.Node.Name)
                    {
                        //TODO(question): would SyntaxKind.DivideExpression work for PData.Cnst_IntDiv.Node.Nam?
                        //It seems there's no "int division" in Roslyn = checked "2/3" and "(int)a / (int)b"
                        return MkCSharpBinaryExpression(SyntaxKind.DivideExpression, arg1, arg2);
                    }
                    else if (op == PData.Cnst_And.Node.Name)
                    {
                        return MkCSharpBinaryExpression(SyntaxKind.BitwiseAndExpression, arg1, arg2);
                    }
                    else if (op == PData.Cnst_And.Node.Name)
                    {
                        return MkCSharpBinaryExpression(SyntaxKind.BitwiseOrExpression, arg1, arg2);
                    }
                    else if (op == PData.Cnst_Eq.Node.Name)
                    {
                        return MkCSharpBinaryExpression(SyntaxKind.EqualsExpression, arg1, arg2);
                    }
                    else if (op == PData.Cnst_NEq.Node.Name)
                    {
                        return MkCSharpBinaryExpression(SyntaxKind.NotEqualsExpression, arg1, arg2);
                    }
                    else if (op == PData.Cnst_Lt.Node.Name)
                    {
                        return MkCSharpBinaryExpression(SyntaxKind.LessThanExpression, arg1, arg2);
                    }
                    else if (op == PData.Cnst_Le.Node.Name)
                    {
                        return MkCSharpBinaryExpression(SyntaxKind.LessThanOrEqualExpression, arg1, arg2);
                    }
                    else if (op == PData.Cnst_Gt.Node.Name)
                    {
                        return MkCSharpBinaryExpression(SyntaxKind.GreaterThanExpression, arg1, arg2);
                    }
                    else if (op == PData.Cnst_Ge.Node.Name)
                    {
                        return MkCSharpBinaryExpression(SyntaxKind.GreaterThanOrEqualExpression, arg1, arg2);
                    }
                    else if (op == PData.Cnst_Idx.Node.Name)
                    {
                        //TODO(expand)
                        throw new NotImplementedException();
                    }
                    else
                    {
                        //TODO(expand)
                        // op == PData.Cnst_In.Node.Name
                        throw new NotImplementedException();
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
                throw new NotImplementedException();
            }
            SyntaxNode FoldDefault(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            SyntaxNode FoldCast(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            SyntaxNode FoldTuple(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            SyntaxNode FoldNamedTuple(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            SyntaxNode FoldGoto(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }

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
                    SyntaxNode[] pars = new SyntaxNode[args.Count];
                    pars[0] = tupTypeExpr;
                    for (int i = 1; i < pars.Length; i++)
                    {
                        pars[i] = args[i - 1];
                    }
                    return MkCSharpObjectCreationExpression(IdentifierName("PrtTupleValue"), pars);
                }
            }

            SyntaxNode FoldRaise(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }

            SyntaxNode FoldSend(FuncTerm ft, List<SyntaxNode> args)
            {
                SyntaxNode targetExpr = MkCSharpCastExpression("PrtMachineValue", args[0]);
                ExpressionSyntax eventExpr = (ExpressionSyntax)MkCSharpCastExpression("PrtEventValue", args[1]);
                args.RemoveRange(0, 2);
                ExpressionSyntax tupleTypeExpr = (ExpressionSyntax)MkCSharpDot(eventExpr, "payloadType");
                ExpressionSyntax payloadExpr = (ExpressionSyntax)MkPayload(tupleTypeExpr, args);
                var invocationArgs = new ArgumentSyntax[]
                {
                    Argument(eventExpr), Argument(payloadExpr), Argument((ExpressionSyntax)MkCSharpIdentifierName("parent"))
                };
                StatementSyntax enqueueEventStmt = ExpressionStatement(
                    MkCSharpInvocationExpression(
                        MkCSharpDot(MkCSharpDot((ExpressionSyntax)targetExpr, "mach"), "PrtEnqueueEvent"),
                        invocationArgs));

                invocationArgs = new ArgumentSyntax[]
                {
                    Argument(ThisExpression()),
                    Argument((ExpressionSyntax)MkCSharpDot("currFun", "locals")),
                    Argument((ExpressionSyntax)MkCSharpDot("currFun", "returnToLocation"))
                };
                StatementSyntax contStmt = ExpressionStatement(
                    (ExpressionSyntax)MkCSharpInvocationExpression(
                    (ExpressionSyntax)MkCSharpDot("parent", "PrtFunContSend"),
                     invocationArgs));

                var afterLabel = GetFreshLabel();
                StatementSyntax afterStmt = MkCSharpEmptyLabeledStatement(afterLabel);
                return Block(enqueueEventStmt, contStmt, afterStmt);
            }

            SyntaxNode FoldAnnounce(FuncTerm ft, List<SyntaxNode> args)
            {
                ExpressionSyntax eventExpr = (ExpressionSyntax)MkCSharpCastExpression("PrtEventValue", args[0]);
                args.RemoveAt(0);
                ExpressionSyntax tupleTypeExpr = (ExpressionSyntax)MkCSharpDot(eventExpr, "payloadType");
                ExpressionSyntax payloadExpr = (ExpressionSyntax)MkPayload(tupleTypeExpr, args);
                var invocationArgs = new ArgumentSyntax[]
                {
                    Argument(eventExpr), Argument(payloadExpr), Argument((ExpressionSyntax)MkCSharpIdentifierName("parent"))
                };
                StatementSyntax announceEventStmt = ExpressionStatement(
                    MkCSharpInvocationExpression(IdentifierName("Announce"), invocationArgs));

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
                    return IfStatement(
                        MkCSharpDot((ExpressionSyntax)it.Current, "bl"),
                        ThrowStatement(MkCSharpObjectCreationExpression(IdentifierName("PrtAssertFailureException"), MkCSharpStringLiteralExpression(errorMsg))));
                }
            }

            SyntaxNode FoldFunStmt(FuncTerm ft, List<SyntaxNode> children)
            {
                return FoldFunApp(ft, children);
            }

            SyntaxNode FoldNulStmt(FuncTerm ft, List<SyntaxNode> children)
            {
                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                var op = ((Id)GetArgByIndex(ft, 0)).Name;
                if (op == PData.Cnst_Pop.Node.Name)
                {
                    stmtList.Add(MkCSharpSimpleAssignmentExpressionStatement(MkCSharpDot("parent", "currentTrigger"), MkCSharpIdentifierName("null")));
                    stmtList.Add(MkCSharpSimpleAssignmentExpressionStatement(MkCSharpDot("parent", "currentEvent"), MkCSharpIdentifierName("null")));
                    stmtList.Add(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("parent", "PrtPopState"), Argument(MkCSharpTrueLiteralExpression()))));
                    stmtList.Add(ReturnStatement());
                }
                return Block(stmtList);
            }

            SyntaxNode FoldPrint(FuncTerm ft, List<SyntaxNode> children)
            {
                string msg = (GetArgByIndex(ft, 0) as Cnst).GetStringValue();
                List<StatementSyntax> stmts = new List<StatementSyntax>();
                stmts.Add(MkCSharpPrint(msg));
                FuncTerm seg = GetArgByIndex(ft, 1) as FuncTerm;
                while (seg != null)
                {
                    int formatArg = (int)(GetArgByIndex(seg, 0) as Cnst).GetNumericValue().Numerator;
                    string str = (GetArgByIndex(seg, 1) as Cnst).GetStringValue();
                    seg = GetArgByIndex(seg, 2) as FuncTerm;
                    stmts.Add(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot((ExpressionSyntax)children[formatArg], "ToString"))));
                    stmts.Add(MkCSharpPrint(str));
                }
                return Block(stmts);
            }

            SyntaxNode FoldBinStmt(FuncTerm ft, List<SyntaxNode> children)
            {
                var op = ((Id)GetArgByIndex(ft, 0)).Name;
                var lhs = (FuncTerm)GetArgByIndex(ft, 1);
                var type = LookupType(lhs);
                var typeName = ((Id)type.Function).Name;
                SyntaxNode src = null, dest = null;
                using (var it = children.GetEnumerator())
                {
                    SyntaxNode index = null;
                    it.MoveNext();
                    src = it.Current;
                    it.MoveNext();
                    dest = it.Current;
                    if (it.MoveNext())
                    {
                        index = it.Current;
                    }

                    if (op == PData.Cnst_Assign.Node.Name)
                    {
                        //arg #2 is Qualifier - ignored for now
                        string assignType = (GetArgByIndex(ft, 2) as Id).Name;
                        if (((Id)lhs.Function).Name == PData.Con_Field.Node.Name)
                        {
                            //TODO(question): is this the case of: "f[5] = e" or "f[i] = e"?
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
                                //TODO: two cases are needed to use NumericLiteralExpression
                                //for a numeric index; could be merged into one case
                                if (field.CnstKind == CnstKind.Numeric)
                                {
                                    return MkCSharpSimpleAssignmentExpressionStatement(
                                         MkCSharpElementAccessExpression(src, fieldIndex), dest);
                                }
                                else
                                {
                                    return MkCSharpSimpleAssignmentExpressionStatement(
                                         MkCSharpElementAccessExpression(src, fieldIndex), dest);
                                }
                            }
                            else if (assignType == "XFER")
                            {
                                //TODO(expand):
                                throw new NotImplementedException();
                            }
                            else
                            {   
                                // assignType = "SWAP" 
                                //TODO(expand):
                                throw new NotImplementedException();
                            }
                        }
                        else if (index == null)
                        {
                            if (assignType == "NONE")
                            {
                                return MkCSharpSimpleAssignmentExpressionStatement(dest, src);
                            }
                            else if (assignType == "XFER")
                            {
                                //TODO(expand):
                                throw new NotImplementedException();
                            }
                            else
                            {   
                                // assignType == "SWAP"
                                //TODO(expand):
                                throw new NotImplementedException();
                            }
                        }
                        //Asgn when lhs is not a field (?):
                        else
                        {
                            lhs = (FuncTerm)GetArgByIndex(lhs, 1);
                            type = LookupType(lhs);
                            typeName = ((Id)type.Function).Name;
                            if (typeName == PData.Con_SeqType.Node.Name)
                            {
                                if (assignType == "NONE")
                                {
                                    //TODO(expand):
                                    throw new NotImplementedException();
                                }
                                else if (assignType == "XFER")
                                {
                                    //TODO(expand):
                                    throw new NotImplementedException();
                                }
                                else
                                {   
                                    // assignType == "SWAP"
                                    //TODO(expand):
                                    throw new NotImplementedException();
                                }
                            }
                            else
                            {
                                // type is PMapType
                                if (assignType == "NONE")
                                {
                                    //TODO(expand):
                                    throw new NotImplementedException();
                                }
                                else if (assignType == "XFER")
                                {
                                    //TODO(expand):
                                    throw new NotImplementedException();
                                }
                                else
                                {   
                                    // assignType == "SWAP"
                                    //TODO(expand):
                                    throw new NotImplementedException();
                                }
                            }
                        }
                    }
                    else if (op == PData.Cnst_Remove.Node.Name)
                    {
                        if (typeName == PData.Con_SeqType.Node.Name)
                        {
                            //TODO(expand):
                            throw new NotImplementedException();
                        }
                        else
                        {
                            //TODO(expand):
                            throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        // op == PData.Cnst_Insert.Node.Name
                        if (typeName == PData.Con_SeqType.Node.Name)
                        {
                            //TODO(expand):
                            throw new NotImplementedException();
                        }
                        else
                        {
                            //TODO(expand):
                            throw new NotImplementedException();
                        }
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
                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    if (returnType.Equals(PTypeNull))
                    {
                        stmtList.Add(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("entryCtxt", "Return"), Argument(MkCSharpIdentifierName("locals")))));
                    }
                    else
                    {
                        stmtList.Add(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("entryCtxt", "ReturnVal"), Argument((ExpressionSyntax)it.Current), Argument(MkCSharpIdentifierName("locals")))));
                    }
                    stmtList.Add(ReturnStatement());
                    return Block(stmtList);
                }
            }

            SyntaxNode FoldWhile(FuncTerm ft, List<SyntaxNode> children)
            {
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var condExpr = MkCSharpDot((ExpressionSyntax)it.Current, "bl");
                    it.MoveNext();
                    var loopStart = pToCSharp.GetUnique(funName + "_loop_start");
                    var loopEnd = pToCSharp.GetUnique(funName + "_loop_end");
                    var body = it.Current;
                    var res = MkCSharpLabeledBlock(loopStart, 
                        Block(
                            IfStatement(MkCSharpUnaryExpression(SyntaxKind.LogicalNotExpression, condExpr), MkCSharpGoto(loopEnd)),
                            (StatementSyntax)body,
                            MkCSharpGoto(loopStart),
                            MkCSharpEmptyLabeledStatement(loopEnd)));
                    return res;
                }
            }

            SyntaxNode FoldIte(FuncTerm ft, List<SyntaxNode> children)
            {
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var condExpr = MkCSharpDot((ExpressionSyntax)it.Current, "bl");
                    it.MoveNext();
                    var thenStmt = it.Current;
                    it.MoveNext();
                    var elseStmt = it.Current;

                    var ifName = pToCSharp.GetUnique(funName + "_if");
                    var elseLabel = ifName + "_else";
                    var afterLabel = ifName + "_end";
                    var cookedElse = MkCSharpLabeledBlock(elseLabel, (StatementSyntax)elseStmt);
                    var cookedThen = (StatementSyntax)thenStmt;
                    var res = Block(IfStatement(MkCSharpUnaryExpression(SyntaxKind.LogicalNotExpression, condExpr), MkCSharpGoto(elseLabel)),
                        cookedThen,
                        MkCSharpGoto(afterLabel),
                        cookedElse,
                        MkCSharpEmptyLabeledStatement(afterLabel));
                    return res;
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
            #endregion

            public SyntaxNode MkFunStackFrameClass()
            {
                SyntaxList<MemberDeclarationSyntax> members = new SyntaxList<MemberDeclarationSyntax>();
                string frameClassName = funName + "_StackFrame";
                //public F1_Class_StackFrame(PrtFun fun, List<PrtValue> locs) : base(fun, locs) {}
                var pars = new List<SyntaxNode> { MkCSharpParameter(MkCSharpIdentifier("locals"), MkCSharpGenericListType(IdentifierName("PrtValue"))),
                                                  MkCSharpParameter(MkCSharpIdentifier("retLoc"), PredefinedType(Token(SyntaxKind.IntKeyword))) };
                SyntaxTokenList modifiers = new SyntaxTokenList();
                modifiers = modifiers.Add(Token(SyntaxKind.PublicKeyword));
                members = members.Add(MkCSharpConstructor(MkCSharpIdentifier(frameClassName),
                                                          modifiers,
                                                          new List<SyntaxNode>() {
                                                              MkCSharpParameter(MkCSharpIdentifier("fun"), (TypeSyntax) MkCSharpIdentifierName("PrtFun")),
                                                              MkCSharpParameter(MkCSharpIdentifier("locs"), MkCSharpGenericListType(IdentifierName("PrtValue"))) },
                                                          MkCSharpConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                                                              MkCSharpArgumentList(new ArgumentSyntax[] { Argument(IdentifierName("fun")), Argument(IdentifierName("locs")) })),
                                                          new List<StatementSyntax>()));

                //public F2_Class_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base(fun, locs, retLocation) {}
                members = members.Add(MkCSharpConstructor(MkCSharpIdentifier(frameClassName),
                                                         modifiers,
                                                         new List<SyntaxNode>() {
                                                              MkCSharpParameter(MkCSharpIdentifier("fun"), (TypeSyntax) MkCSharpIdentifierName("PrtFun")),
                                                              MkCSharpParameter(MkCSharpIdentifier("locs"), MkCSharpGenericListType(IdentifierName("PrtValue"))),
                                                              MkCSharpParameter(MkCSharpIdentifier("retLocation"), PredefinedType(Token(SyntaxKind.IntKeyword))) },
                                                         MkCSharpConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                                                             MkCSharpArgumentList(new ArgumentSyntax[] { Argument(IdentifierName("fun")), Argument(IdentifierName("locs")),
                                                                                                         Argument(IdentifierName("retLocation")) })),
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
                members = members.Add((MemberDeclarationSyntax)MkCSharpMethodDeclaration(MkCSharpIdentifierName("PrtFunStackFrame"),
                                MkCSharpIdentifier("Clone"),
                                new[] { Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword) },
                                body,
                                clonePars));

                //Getters/setters for locals variables of the function: parameters and locals
                foreach (var pair in funInfo.localNameToInfo)
                {
                    string varName = pair.Key;
                    //Debug:
                    //Console.WriteLine("Next local of function {0} is {1}", funName, varName);

                    int ind = pair.Value.index;
                    //Console.WriteLine("Index of the next local {0} is {1}", varName, ind);
                    //Example: public PrtValue Par1 {get {return locals[0];} {set {locals[0] = value;}}

                    modifiers = new SyntaxTokenList();
                    modifiers = modifiers.Add(Token(SyntaxKind.PublicKeyword));
                    var getBody = SingletonList<StatementSyntax>(ReturnStatement(
                           (ExpressionSyntax) MkCSharpElementAccessExpression(
                               MkCSharpIdentifierName("locals"), ind)));
                    var setBody = SingletonList<StatementSyntax>((StatementSyntax)MkCSharpSimpleAssignmentExpressionStatement(
                            (ExpressionSyntax)MkCSharpElementAccessExpression(
                                MkCSharpIdentifierName("locals"), ind),
                            MkCSharpIdentifierName("value")));
                    AccessorDeclarationSyntax[] accessorList = new AccessorDeclarationSyntax[]
                        { MkCSharpAccessor("get", getBody), MkCSharpAccessor("set", setBody)};
                    members = members.Add((MemberDeclarationSyntax)MkCSharpPropertyDecl("PrtValue", varName,
                                          modifiers,
                                          accessorList));
                }

                modifiers = new SyntaxTokenList();
                modifiers = modifiers.Add(Token(SyntaxKind.InternalKeyword));
                return MkCSharpClassDecl(frameClassName, modifiers,
                                         SingletonSeparatedList<BaseTypeSyntax>(MkCSharpIdentifierNameType("PrtFunStackFrame")),
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
                                IdentifierName(owner.machName))
                            .WithVariables(
                                SingletonSeparatedList<VariableDeclaratorSyntax>(
                                    VariableDeclarator(
                                        MkCSharpIdentifier("parent"))
                                    .WithInitializer(
                                        EqualsValueClause(
                                                MkCSharpCastExpression(owner.machName, MkCSharpIdentifierName("_parent")))))))
                        .NormalizeWhitespace());
                }
                funStmts.Add(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName("PrtFunStackFrame"))
                        .WithVariables(
                            SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(
                                    MkCSharpIdentifier("currFun"))
                                .WithInitializer(
                                    EqualsValueClause(
                                        InvocationExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("parent"),
                                                IdentifierName("PrtPopFunStackFrame"))))))))
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
                                            LiteralExpression(
                                                SyntaxKind.NullLiteralExpression))))))
                    .NormalizeWhitespace());

                var executeMethodDecl =
                    MethodDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.VoidKeyword)),
                        MkCSharpIdentifier("Execute"))
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
                                        MkCSharpIdentifier("application"))
                                    .WithType(
                                        IdentifierName("StateImpl")),
                                    Token(SyntaxKind.CommaToken),
                                    Parameter(
                                        owner == null ? MkCSharpIdentifier("parent") : MkCSharpIdentifier("_parent"))
                                    .WithType(
                                        IdentifierName("PrtMachine"))})))
                    .WithBody(
                        //Block(stmt1, stmt2, stmt3, stmt4))
                        Block(funStmts))
                    .NormalizeWhitespace();

                return executeMethodDecl;
            }
            //TODO(fix): replace this code with general case: CreateLocals method for any function
            public SyntaxNode MkCreateLocalsMethod()
            {
                //var locals = new List<PrtValue>();
                var stmt1 =
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName("var"))
                        .WithVariables(
                            SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(
                                    MkCSharpIdentifier("locals"))
                                .WithInitializer(
                                    EqualsValueClause(
                                        ObjectCreationExpression(
                                            GenericName(
                                                MkCSharpIdentifier("List"))
                                            .WithTypeArgumentList(
                                                TypeArgumentList(
                                                    SingletonSeparatedList<TypeSyntax>(
                                                        IdentifierName("PrtValue")))))
                                        .WithArgumentList(
                                            ArgumentList()))))))
                    .NormalizeWhitespace();

                //foreach (var item in args)
                var stmt2 =
                    ForEachStatement(
                        IdentifierName("var"),
                        MkCSharpIdentifier("item"),
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
                    .NormalizeWhitespace();

                ////no local variables hence nothing to add
                //return locals;
                var stmt3 =
                    ReturnStatement(
                        IdentifierName("locals"))
                    .WithReturnKeyword(
                        Token(
                            TriviaList(
                                Comment("//no local variables hence nothing to add")),
                            SyntaxKind.ReturnKeyword,
                            TriviaList()))
                    .NormalizeWhitespace();

                //public override List<PrtValue> CreateLocals(params PrtValue[] args) { ... }
                var createLocalsMethodDecl =
                    MethodDeclaration(
                        GenericName(
                            MkCSharpIdentifier("List"))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SingletonSeparatedList<TypeSyntax>(
                                    IdentifierName("PrtValue")))),
                        MkCSharpIdentifier("CreateLocals"))
                    .WithModifiers(
                        TokenList(
                            new[]{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.OverrideKeyword)}))
                    .WithParameterList(
                        ParameterList(
                            SingletonSeparatedList<ParameterSyntax>(
                                Parameter(
                                    MkCSharpIdentifier("args"))
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
                        Block(stmt1, stmt2, stmt3))
                    .NormalizeWhitespace();

                return createLocalsMethodDecl;
            }
            public SyntaxNode MkCreateFunStackFrameMethod()
            {
                //TODO(expand): returns "null" for now
                //Body: return new F2_StackFrame(this, locals, retLoc);
                var body = SingletonList<StatementSyntax>(
                            ReturnStatement((ExpressionSyntax)MkCSharpObjectCreationExpression(
                                MkCSharpIdentifierName(funName + "_StackFrame"),
                                new SyntaxNode[] { ThisExpression(), MkCSharpIdentifierName("locals"), MkCSharpIdentifierName("retLoc") })));
                var pars = new List<SyntaxNode> { MkCSharpParameter(MkCSharpIdentifier("locals"), MkCSharpGenericListType(IdentifierName("PrtValue"))),
                                                  MkCSharpParameter(MkCSharpIdentifier("retLoc"), PredefinedType(Token(SyntaxKind.IntKeyword))) };
                return MkCSharpMethodDeclaration(MkCSharpIdentifierName("PrtFunStackFrame"), MkCSharpIdentifier("CreateFunStackFrame"),
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
                            MkCSharpIdentifier("IsAnonFun"))
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
                            MkCSharpIdentifier("IsAnonFun"))
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

                var funClassDecl =
                    ClassDeclaration(funType)
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
                    whereToAdd = (this.isGlobalStatic) ? PToCSharp.members : owner.machineMembers;

                    whereToAdd.Add(MkFuncClass());
                    //Variable declaration:
                    whereToAdd.Add(
                        FieldDeclaration(
                            VariableDeclaration(
                                IdentifierName(funType))
                            .WithVariables(
                                SingletonSeparatedList<VariableDeclaratorSyntax>(
                                    VariableDeclarator(
                                        MkCSharpIdentifier(funName)))))
                        .WithModifiers(
                            TokenList(
                                new[]{
                                        Token(SyntaxKind.PublicKeyword),
                                        Token(SyntaxKind.StaticKeyword)}))
                        .NormalizeWhitespace());

                    //Add function variable instantiation to:
                    //PToCsharp class for global static functions;
                    //Main constructor for other functions
                    if (this.isGlobalStatic)
                    {
                        PToCSharp.members.Add(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName(funName),
                                    ObjectCreationExpression(
                                        IdentifierName(funType))
                                    .WithArgumentList(
                                        ArgumentList())))
                            .NormalizeWhitespace());
                    }
                    else
                    {
                        owner.mainConstructorFields.Add(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName(funName),
                                    ObjectCreationExpression(
                                        IdentifierName(funType))
                                    .WithArgumentList(
                                        ArgumentList())))
                            .NormalizeWhitespace());
                    }
   
                    if (!(owner == null))
                    {
                        owner.processedFuns.Add(funName);
                    }   
                }
            }
        }
        private void MkStaticFunctions()
        {
            foreach (var pair in allStaticFuns)
            {
                var funName = pair.Key;
                var funType = funName + "_Class";
                MkFunctionDecl funDecl = new MkFunctionDecl(funName, pair.Value, true, null, this);
                funDecl.AddFunClass();
            }
        }
        private void MkOtherAppFields()
        {
            //CreateMainMachine method declaration:
            List<SyntaxNode> fields = new List<SyntaxNode>();
            //stmt1: var mainMachine = new Main(this, mainMachineMaxQueueSize, true);
            MachineInfo mainMachInfo;
            //There are three cases:
            //- default (no constraint on queue size): maxQueueSizeAssumed == false; maxQueueSize = default (10?) 
            //- assume <maxQueueSize>: maxQueueSize > 0, maxQueueSizeAssumed == true;
            //- assert <maxQueueSize>: maxQueueSize > 0, maxQueueSizeAssumed == false;   
            if (allMachines.TryGetValue("Main", out mainMachInfo))
            {
                if (mainMachInfo.maxQueueSize > 0)
                {
                    fields.Add(generator.LocalDeclarationStatement(generator.IdentifierName("var"), "mainMachine",
                                   generator.ObjectCreationExpression(generator.IdentifierName("Main"),
                                   new List<SyntaxNode>() { generator.ThisExpression(), generator.LiteralExpression(mainMachInfo.maxQueueSize),
                                                            generator.LiteralExpression(mainMachInfo.maxQueueSizeAssumed)})));
                }
                else
                {
                    //TODO(question): 10 is the default maxQueueSize for Main machine
                    //TODO: create "PrtImplMachine.DefaultMaxBuffer"
                    fields.Add(generator.LocalDeclarationStatement(generator.IdentifierName("var"), "mainMachine",
                                   generator.ObjectCreationExpression(generator.IdentifierName("Main"),
                                   new List<SyntaxNode>() { generator.ThisExpression(), generator.LiteralExpression(10),
                                                            generator.LiteralExpression(mainMachInfo.maxQueueSizeAssumed) })));
                }
            }
        
            //stmt2: AddImplMachineToStateImpl(mainMachine);
            fields.Add(generator.InvocationExpression(generator.IdentifierName("AddImplMachineToStateImpl"), 
                                 new List<SyntaxNode>() { generator.IdentifierName("mainMachine") }));
            //stmt3: return mainMachine;
            fields.Add(generator.ReturnStatement(generator.IdentifierName("mainMachine")));
            //public PrtImplMachine CreateMainMachine() {stmt1; stmt2; stmt3};
            var makeCreateMainMachineDecl = generator.MethodDeclaration("CreateMainMachine", null,
              null, generator.IdentifierName("PrtImplMachine"),
              Accessibility.Public,
              statements: fields);
            members.Add(makeCreateMainMachineDecl);
        }
        private void MkMachineClasses()
        {
            //TODO(expand) For now: only calls MkMainMachineClass  
            //Debug only:
            Console.WriteLine("Number of machines: {0}", allMachines.Count());
            foreach (var pair in allMachines)
            {
                MkMachineClass mkMachine;
                if ((pair.Value).IsReal)
                {
                    //Regular machine:
                    //Debug only:
                    Console.WriteLine("Next Real machine: name: {0}", pair.Key);
                    mkMachine = new MkMachineClass(this, pair.Key, pair.Value);
                    SyntaxNode node = mkMachine.MkRealMachineClass();
                    members.Add(node);
                }
                else
                {
                    //monitor machine
                    //Debug only:
                    Console.WriteLine("Next Spec machine: name: {0}", pair.Key);
                    mkMachine = new MkMachineClass(this, pair.Key, pair.Value);
                    SyntaxNode node = mkMachine.MkSpecMachineClass();
                    members.Add(node);
                }
            }
        }
        internal class MkMachineClass
        {
            public PToCSharp translator;
            public string machName;
            public MachineInfo machInfo;
            public List<SyntaxNode> machineMembers = new List<SyntaxNode>();
            public List<StatementSyntax> mainConstructorFields = new List<StatementSyntax>();
            //keeps track of already encountered function names:
            public HashSet<string> processedFuns = new HashSet<string>();
            private int transition_count = 1;
            public MkMachineClass(PToCSharp ptoCsharp, string name, MachineInfo info) {
                translator = ptoCsharp;
                machName = name;
                machInfo = info;
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
            //public enum FunContext { EntryFun, ExitFun, Transitions, Dos } 
            public FunInfo GetFunInfo(string funName)
            {
                if (translator.allStaticFuns.ContainsKey(funName))
                {
                    return translator.allStaticFuns[funName];
                }
                else
                {
                    return machInfo.funNameToFunInfo[funName];
                }
                //return null;
            }
            public SyntaxNode MkRealMachineClass()
            {
                //StartState property (getter only, since there's no setter in the base class):
                string startState = machInfo.initStateName;
                var startStateProperty =
                    PropertyDeclaration(
                        IdentifierName("PrtState"),
                        MkCSharpIdentifier("StartState"))
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
                                                //TODO(bug): replace with init state name from machineInfo, + "_State"
                                                IdentifierName(startState))))))))
                    .NormalizeWhitespace();
                machineMembers.Add(startStateProperty);

                //Add getters/setters for machine variables:
                int ind = 0;
                foreach (var pair in machInfo.localVariableToVarInfo)
                {
                    SyntaxTokenList modifiers = new SyntaxTokenList();
                    modifiers = modifiers.Add(Token(SyntaxKind.PublicKeyword));
                    var getBody = SingletonList<StatementSyntax>(ReturnStatement(
                               (ExpressionSyntax)MkCSharpElementAccessExpression(
                                   MkCSharpIdentifierName("fields"), ind)));
                    var setBody = SingletonList<StatementSyntax>((StatementSyntax)MkCSharpSimpleAssignmentExpressionStatement(
                            (ExpressionSyntax)MkCSharpElementAccessExpression(
                                MkCSharpIdentifierName("fields"), ind),
                            MkCSharpIdentifierName("value")));
                    AccessorDeclarationSyntax[] accessorList = new AccessorDeclarationSyntax[]
                            { MkCSharpAccessor("get", getBody), MkCSharpAccessor("set", setBody)};
                    machineMembers.Add(MkCSharpPropertyDecl("PrtValue", pair.Key, modifiers, accessorList));
                    ind += 1;
                }
                  
                var skeletonMethodBody = generator.ReturnStatement(generator.ObjectCreationExpression(generator.IdentifierName(machName)));
                var skeletonMethodDecl = generator.MethodDeclaration("MakeSkeleton", null,
                  null, generator.IdentifierName("PrtImplMachine"),
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
                var methodPars = new SyntaxNode[] { generator.ParameterDeclaration("app", generator.IdentifierName("StateImpl")) };
                var nextInstanceNumberMethodDecl = generator.MethodDeclaration("NextInstanceNumber", methodPars,
                  //null, generator.IdentifierName("int"),
                  null, generator.TypeExpression(SpecialType.System_Int32),
                  Accessibility.Public,
                  DeclarationModifiers.Override,
                  new SyntaxNode[] { nextInstanceNumberMethodBody });
                machineMembers.Add(nextInstanceNumberMethodDecl);
                
                //Name property (getter only):
                var nameProperty =
                    PropertyDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.StringKeyword)),
                        MkCSharpIdentifier("Name"))
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
                                                IdentifierName("\"" + machName + "\""))))))))
                    .NormalizeWhitespace();
                machineMembers.Add(nameProperty);

                //constructor for cloning
                var constructor_1 = generator.ConstructorDeclaration(machName, null, Accessibility.Public, baseConstructorArguments: new SyntaxNode[0]);
                machineMembers.Add(constructor_1);

                //Machine class constructor
                //public PONG(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
                //TODO(expand): add inits for all fields
                var constructorPars = new SyntaxNode[]
                {
                generator.ParameterDeclaration("app", generator.IdentifierName("StateImpl")),
                generator.ParameterDeclaration("maxB", generator.TypeExpression(SpecialType.System_Int32)),
                generator.ParameterDeclaration("assume", generator.TypeExpression(SpecialType.System_Boolean))
                };
                var baseConstructorArgs = new SyntaxNode[] { generator.IdentifierName("app"), generator.IdentifierName("maxB"), generator.IdentifierName("assume") };
                var constructor_2 = generator.ConstructorDeclaration(machName, constructorPars, Accessibility.Public,
                                    baseConstructorArguments: baseConstructorArgs);
                machineMembers.Add(constructor_2);

                //classes for functions for each state of the machine
                //and variable declarations for those functions:
                foreach (var pair in machInfo.stateNameToStateInfo)
                {
                    //entry function of the state:
                    var funName = pair.Value.entryActionName;
                    FunInfo funInfo = GetFunInfo(funName);
                    //TODO(remove): Debug only:
                    Console.WriteLine("Entry func name for state {0}: {1}", pair.Key, funName);
                    MkFunctionDecl funDecl = new MkFunctionDecl(funName, funInfo, false, this, translator);
                    funDecl.AddFunClass();

                    //exit function of the state: 
                    funName = pair.Value.exitFunName;
                    //TODO(remove): Debug only:
                    Console.WriteLine("Exit func name for state {0}: {1}", pair.Key, funName);
                    funInfo = GetFunInfo(funName);
                    funDecl = new MkFunctionDecl(funName, funInfo, false, this, translator);
                    funDecl.AddFunClass();

                    //Functions in transitions:
                    foreach (var transition in pair.Value.transitions)
                    {
                        funName = transition.Value.transFunName;
                        funInfo = GetFunInfo(funName);

                        if (!transition.Value.IsPush && !translator.allStaticFuns.ContainsKey(funName))
                        {
                            //TODO(remove): Debug only:
                            Console.WriteLine("For goto transition: func name for state {0}: {1}", pair.Key, funName);
                            funDecl = new MkFunctionDecl(funName, funInfo, false, this, translator);
                            funDecl.AddFunClass();
                        }
                    }

                    //Functions in dos: loop over StateInfo.actions (to be renamed into StateInfo.dos):
                    foreach (var doFun in pair.Value.dos)
                    {
                        funName = doFun.Value;
                        funInfo = GetFunInfo(funName);
                        //TODO(remove): Debug only:
                        Console.WriteLine("For Do declaration: func name for state {0}: {1}", pair.Key, funName);
                        if (!translator.allStaticFuns.ContainsKey(funName))
                        {
                            funDecl = new MkFunctionDecl(funName, funInfo, false, this, translator);
                            funDecl.AddFunClass();
                        } 
                    }

                }

                //State classes for all states of the machine:
                foreach (var pair in machInfo.stateNameToStateInfo)
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
                                        MkCSharpIdentifier(stateType))
                                    .WithModifiers(
                                        TokenList(
                                            Token(SyntaxKind.PublicKeyword)))
                                    .WithParameterList(
                                        ParameterList(
                                            SeparatedList<ParameterSyntax>(
                                                new SyntaxNodeOrToken[]{
                                                    Parameter(
                                                        MkCSharpIdentifier("name"))
                                                    .WithType(
                                                        PredefinedType(
                                                            Token(SyntaxKind.StringKeyword))),
                                                    Token(SyntaxKind.CommaToken),
                                                    Parameter(
                                                        MkCSharpIdentifier("entryFun"))
                                                    .WithType(
                                                        IdentifierName("PrtFun")),
                                                    Token(SyntaxKind.CommaToken),
                                                    Parameter(
                                                        MkCSharpIdentifier("exitFun"))
                                                    .WithType(
                                                        IdentifierName("PrtFun")),
                                                    Token(SyntaxKind.CommaToken),
                                                    Parameter(
                                                        MkCSharpIdentifier("hasNullTransition"))
                                                    .WithType(
                                                        PredefinedType(
                                                            Token(SyntaxKind.BoolKeyword))),
                                                    Token(SyntaxKind.CommaToken),
                                                    Parameter(
                                                        MkCSharpIdentifier("temperature"))
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
                                        MkCSharpIdentifier(stateName)))))
                        .WithModifiers(
                            TokenList(
                                new[]{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.StaticKeyword)}))
                        .NormalizeWhitespace();
                    machineMembers.Add(stateDeclaration);

                    //state instantiation for the machine class constructor:
                    //Example: 
                    //PONG_Pong_WaitPing = new PONG_Pong_WaitPing_Class("PONG_Pong_WaitPing", AnonFun1, AnonFun0, false, StateTemperature.Warm);
                    //Consider 6 cases (write a mmethod): for each bool value of hasNullTransition, there are 3 possible "temperatures"
                    //TODO(remove): Debug only:
                    Console.WriteLine("hasNullTransition for state {0} is {1}", stateName, pair.Value.hasNullTransition);
                    mainConstructorFields.Add(MkStateInstantiation(stateName, stateType, pair.Value.entryActionName, pair.Value.exitFunName,
                                              pair.Value.hasNullTransition, pair.Value.temperature));

                    //Add DoDecls to the StateInfo:
                    //Example: Main_Ping_SendPing.dos.Add(Pong, foo);
                    //Note: "ignore E" is considered a DeDecl with the default "ignore" function, and is present in StateInfo.actions
                    //TODO(question): do I have to use pre-defined 
                    //public class PrtIgnoreFun : PrtFun
                    //for "ignore" function?
                    foreach (var doFun in pair.Value.dos)
                    {
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
                                                    IdentifierName(doFun.Key)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(doFun.Value))}))))
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
                                                IdentifierName(deferredEvent))))))
                            .NormalizeWhitespace()
                        );
                    }

                    //Transition instantiations for the state of the machine:

                    //"transition" + "_" + transition_count
                    foreach (var transition in pair.Value.transitions)
                    {
                        string trigger = transition.Key;
                        string transition_name = "transition" + "_" + transition_count;
                        //For push transition, transition.Value.transFunName name is null - 
                        //replacing wuth PrtCommonFunctions.SkipFun
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
                                            MkCSharpIdentifier(transition_name))
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
                                                                        IdentifierName("PrtCommonFunctions"),
                                                                        IdentifierName("SkipFun"))),
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
                                            MkCSharpIdentifier("transition" + "_" + transition_count))
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
                                                        IdentifierName(trigger)),
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
                        MkCSharpIdentifier(machName))
                            .WithModifiers(
                                TokenList(
                                    Token(SyntaxKind.StaticKeyword)))
                            .WithBody(
                                Block(mainConstructorFields
                                    ))
                    .NormalizeWhitespace();

                machineMembers.Add(mainConstructor);

                var mainMachineClassDecl = generator.ClassDeclaration(
                  machName, typeParameters: null,
                  accessibility: Accessibility.Public,
                  baseType: generator.IdentifierName("PrtImplMachine"),
                  members: machineMembers);

                return mainMachineClassDecl;
            }
            
            public SyntaxNode MkSpecMachineClass()
            {
                //TODO(expand)
                return null;
            }
        }
        private void MkCSharpOutput()
        {
            var applicationcClassDeclaration = generator.ClassDeclaration(
              "Application", typeParameters: null,
              accessibility: Accessibility.Public,
              baseType: generator.IdentifierName("StateImpl"),
              members: members);

            // Declare a namespace
            //Generate namespace name: <output file name, no ".cs"> 
            //int index = this.csharpFileName.LastIndexOf(".");
            //string namespaceName = this.csharpFileName.Substring(0, index);
            //namespace is "P.Program" for all P programs:
            string namespaceName = "P.Program";
            var programNameSpaceDeclaration = generator.NamespaceDeclaration(namespaceName, applicationcClassDeclaration);

            List<SyntaxNode> usingDirectivesList = new List<SyntaxNode>();
            usingDirectivesList.Add(generator.NamespaceImportDeclaration("P.Runtime"));
            usingDirectivesList.Add(generator.NamespaceImportDeclaration("System.Collections.Generic"));

            // Get a CompilationUnit (code file) for the generated code
            //var newNode = generator.CompilationUnit(programNameSpaceDeclaration).
            // NormalizeWhitespace();

            // Get a CompilationUnit (code file) for the generated code
            result = generator.CompilationUnit(usingDirectivesList[0], usingDirectivesList[1], programNameSpaceDeclaration).
              NormalizeWhitespace();
        }

        private void EmitCSharpOutput(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            using (StringWriter writer = new StringWriter(sb))
            {
                result.WriteTo(writer);

                //Debug only:
                //Console.WriteLine(writer);
                
                System.IO.StreamWriter file = new System.IO.StreamWriter(fileName);
                file.WriteLine(result);

                file.Close();

            }
        }

        #endregion
    }
    
}
