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
            private Dictionary<AST<Node>, ExpressionSyntax> pTypeToCSharpExpr;
            private PToCSharp pToCSharp;

            public TypeTranslationContext(PToCSharp pToCSharp)
            {
                this.pToCSharp = pToCSharp;
                fieldCount = 0;
                typeCount = 0;
                typeDeclaration = new List<FieldDeclarationSyntax>();
                typeInitialization = new List<StatementSyntax>();
                pTypeToCSharpExpr = new Dictionary<AST<Node>, ExpressionSyntax>();
            }

            private ExpressionSyntax GetType(string typeName)
            {
                var retVal = IdentifierName(typeName);
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
                if (!pTypeToCSharpExpr.ContainsKey(typeAST))
                {
                    pTypeToCSharpExpr[typeAST] = pTypeToCSharpExpr[eTypeAST];
                }
            }

            private ExpressionSyntax ConstructType(FuncTerm type)
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
        public static string EventName(string rawName)
        {
            if (rawName == NullEvent)
                return "@null";
            else
                return rawName;
        }
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

        #region types
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
        #endregion
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
        public static ExpressionSyntax MkCSharpDot(string first, params string[] names)
        {
            return MkCSharpDot(IdentifierName(first), names);
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
                MkCSharpNot(expr),
                ThrowStatement(MkCSharpObjectCreationExpression(IdentifierName("PrtAssertFailureException"), MkCSharpStringLiteralExpression(errorMsg))));
        }
        public static StatementSyntax MkCSharpPrint(string msg, params ExpressionSyntax[] pars)
        {
            var allPars = new List<ExpressionSyntax>(pars);
            allPars.Insert(0, MkCSharpStringLiteralExpression(msg));
            return ExpressionStatement(MkCSharpInvocationExpression(
                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Console"), IdentifierName("Write")), 
                                            allPars.ToArray()));
        }
        public static InvocationExpressionSyntax MkCSharpInvocationExpression(SyntaxNode first, params ExpressionSyntax[] pars)
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
            MkCreateMachineMethods();
            MkMachineClasses();
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
            List<SyntaxNode> stmtList = new List<SyntaxNode>();
            foreach (var machineName in allMachines.Keys)
            {
                if (allMachines[machineName].IsReal) continue;
                stmtList.Add(generator.ExpressionStatement(
                                generator.InvocationExpression(generator.IdentifierName(string.Format("CreateMachine_{0}", machineName)), 
                                                               ThisExpression())));
            }
            var constructorBody = generator.ExpressionStatement(
                generator.InvocationExpression(
                    generator.IdentifierName("CreateMachine_Main"),
                    ThisExpression(),
                    IdentifierName("@null")));
            stmtList.Add(constructorBody);
            var constructor_2 = generator.ConstructorDeclaration("Application", constructorParameters, Accessibility.Public, baseConstructorArguments: new SyntaxNode[0],
                                                                statements: stmtList.ToArray());
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

                string eventNameOrNull = EventName(pair.Key);
                members.Add(
                MkCSharpFieldDeclaration(IdentifierName("PrtEventValue"),
                    EventName(pair.Key),
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

                SyntaxNode lhs = IdentifierName(EventName(pair.Key));
                SyntaxNode rhs = MkCSharpObjectCreationExpression(
                    IdentifierName("PrtEventValue"),
                    MkCSharpObjectCreationExpression(
                        IdentifierName("PrtEvent"),
                        MkCSharpStringLiteralExpression(pair.Key),
                        payloadType,
                        maxInstances,
                        doAssume
                    ));
                inits.Add((StatementSyntax)MkCSharpSimpleAssignmentExpressionStatement(lhs, rhs));
            }

            members.Add(ConstructorDeclaration(
                               Identifier("Application"))
                            .WithModifiers(
                               TokenList(
                                    Token(SyntaxKind.StaticKeyword)))
                            .WithBody(Block(inits))
                            .NormalizeWhitespace());
        }
        internal partial class MkFunctionDecl
        {
            public string funName;
            public FunInfo funInfo;
            public MkMachineClass owner;  // null if global function
            private PToCSharp pToCSharp;
            public Stack<bool> lhsStack;
            private int labelCount; // labels are used for "continuations" in send, new, nondet, receive, function calls

            public MkFunctionDecl(string funName, FunInfo funInfo, MkMachineClass owner, PToCSharp pToCSharp)
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
                    switchLabels = switchLabels.Add(CaseSwitchLabel(MkCSharpNumericLiteralExpression(i)));
                    SyntaxList<StatementSyntax> switchStmts = new SyntaxList<StatementSyntax>();
                    switchStmts = switchStmts.Add(MkCSharpGoto(GetLabelFromLabelId(i)));
                    caseList = caseList.Add(SwitchSection(switchLabels, switchStmts));
                }
                return SwitchStatement(MkCSharpDot("currFun", "returnToLocation"), caseList);
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
                    var calleeInfo = pToCSharp.allStaticFuns.ContainsKey(funName) ? pToCSharp.allStaticFuns[funName] : pToCSharp.allMachines[owner.machineName].funNameToFunInfo[funName];
                    Debug.Assert(calleeInfo.isAnonymous);
                    List<StatementSyntax> ifStmts = new List<StatementSyntax>();
                    ifStmts.Add(ExpressionStatement(MkCSharpInvocationExpression(
                        MkCSharpDot("parent", "PrtPushFunStackFrame"), 
                        IdentifierName(funName), MkCSharpInvocationExpression(MkCSharpDot(funName, "CreateLocals"), MkCSharpDot("parent", "currentPayload")))));
                    ifStmts.Add(MkCSharpGoto(beforeLabel));
                    eventStmts.Add(IfStatement(MkCSharpEq(MkCSharpDot("parent", "currentTrigger"), IdentifierName(eventName)), Block(ifStmts)));
                    funStmts.Add(MkCSharpEmptyLabeledStatement(beforeLabel));
                    funStmts.Add(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot(funName, "Execute"), IdentifierName("application"), IdentifierName("parent"))));
                    var elseStmt = Block(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("parent", "PrtPushFunStackFrame"), IdentifierName(funName), MkCSharpDot("currFun", "locals"), MkCSharpNumericLiteralExpression(beforeLabelId))),
                                         ReturnStatement());
                    funStmts.Add(IfStatement(
                                         MkCSharpEq(MkCSharpDot("parent", "continuation", "reason"), IdentifierName("PrtContinuationReason.Return")),
                                         MkCSharpGoto(afterAfterLabel),
                                         ElseClause(elseStmt)));
                }
                List<StatementSyntax> stmts = new List<StatementSyntax>();
                stmts.AddRange(eventStmts);
                stmts.Add(MkCSharpAssert(MkCSharpFalseLiteralExpression(), "Internal error"));
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
                    stmts.Add(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("parent", "receiveSet", "Add"), IdentifierName(eventName))));
                    var fun = GetArgByIndex(cases, 1);
                    string funName = pToCSharp.anonFunToName[Factory.Instance.ToAST(fun)];
                    funNames.Add(funName);
                    cases = GetArgByIndex(cases, 2) as FuncTerm;
                }
                var afterLabelId = GetFreshLabelId();
                var afterLabel = GetLabelFromLabelId(afterLabelId);
                stmts.Add(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("parent", "PrtFunContReceive"), ThisExpression(), MkCSharpDot("currFun", "locals"), MkCSharpNumericLiteralExpression(afterLabelId))));
                stmts.Add(ReturnStatement());
                stmts.Add(MkCSharpEmptyLabeledStatement(afterLabel));
                var afterAfterLabelId = GetFreshLabelId();
                var afterAfterLabel = GetLabelFromLabelId(afterAfterLabelId);
                stmts.AddRange(CaseFunCallHelper(eventNames, funNames, afterAfterLabel));
                stmts.Add(MkCSharpEmptyLabeledStatement(afterAfterLabel));
                return Block(stmts);
            }

            //In the context of expressions only; no children
            SyntaxNode FoldName(FuncTerm ft, List<SyntaxNode> children)
            {
                var name = GetName(ft, 0);
                if (funInfo != null && funInfo.localNameToInfo.ContainsKey(name))
                {
                    //local var of a function:
                    return MkCSharpElementAccessExpression(MkCSharpDot("currFun", "locals"), funInfo.localNameToInfo[name].index);
                }
                else if (owner != null && pToCSharp.allMachines[owner.machineName].localVariableToVarInfo.ContainsKey(name))
                {
                    return MkCSharpDot("parent", name);
                }
                else
                {
                    //PrtEvent case: emit "new PrtEventValue(eventStaticVar);", where eventStaticVar is eventName
                    var type = LookupType(ft);
                    if (PTypeEvent.Equals(Factory.Instance.ToAST(type)))
                    {
                        return IdentifierName(name);
                    }
                    else
                    {
                        //TODO: check type and add default case to throw an exception
                        //Enum case:
                        return MkCSharpObjectCreationExpression(IdentifierName("PrtIntValue"), IdentifierName(name));
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
                var machineName = pToCSharp.linkMap[createdIorM];
                MachineInfo machineInfo = pToCSharp.allMachines[machineName];
                string initStateEntryActionName = machineInfo.stateNameToStateInfo[machineInfo.initStateName].entryActionName;
                FunInfo entryFunInfo = pToCSharp.allStaticFuns.ContainsKey(initStateEntryActionName)
                                        ? pToCSharp.allStaticFuns[initStateEntryActionName]
                                        : machineInfo.funNameToFunInfo[initStateEntryActionName];
                var payloadVar = MkPayload(pToCSharp.typeContext.PTypeToCSharpExpr(entryFunInfo.PayloadType), children);
                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                stmtList.Add(MkCSharpSimpleAssignmentExpressionStatement(IdentifierName("createdMachine"), MkCSharpInvocationExpression(IdentifierName(string.Format("CreateMachine_{0}", machineName)), IdentifierName("application"), payloadVar)));
                int afterLabelId = GetFreshLabelId();
                string afterLabel = GetLabelFromLabelId(afterLabelId);
                stmtList.Add(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("parent", "PrtFunContNewMachine"), ThisExpression(), MkCSharpDot("currFun", "locals"), IdentifierName("createdMachine"), MkCSharpNumericLiteralExpression(afterLabelId))));
                stmtList.Add(ReturnStatement());
                stmtList.Add(MkCSharpEmptyLabeledStatement(afterLabel));
                stmtList.Add(MkCSharpSimpleAssignmentExpressionStatement(IdentifierName("createdMachine"), MkCSharpDot("parent", "continuation", "createdMachine")));
                stmtList.Add(MkCSharpSimpleAssignmentExpressionStatement(MkCSharpDot("parent", "continuation", "createdMachine"), IdentifierName("null")));
                if (aout != null)
                {
                    stmtList.Add(MkCSharpSimpleAssignmentExpressionStatement(aout, MkCSharpObjectCreationExpression(IdentifierName("PrtMachineValue"), IdentifierName("createdMachine"))));
                }
                return Block(stmtList);
            }

            SyntaxNode FoldFunApp(FuncTerm ft, List<SyntaxNode> children)
            {
                string calleeName = (GetArgByIndex(ft, 0) as Cnst).GetStringValue();
                var paramList = new List<ExpressionSyntax>();
                paramList.Add(IdentifierName("application"));
                paramList.Add(IdentifierName("parent"));
                children.ForEach(x => paramList.Add((ExpressionSyntax)x));
                return MkCSharpInvocationExpression(MkCSharpDot(calleeName, "ExecuteToCompletion"), paramList.ToArray());
            }

            SyntaxNode FoldNulApp(FuncTerm ft, List<SyntaxNode> children)
            {
                //No children
                var n = GetArgByIndex(ft, 0);

                if (n.NodeKind == NodeKind.Cnst)
                {
                    int val = (int)((Cnst)n).GetNumericValue().Numerator;
                    return MkCSharpObjectCreationExpression(IdentifierName("PrtIntValue"),
                            MkCSharpNumericLiteralExpression(val));
                }
                // n.NodeKind == NodeKind.Id
                var op = ((Id)n).Name;
                if (op == PData.Cnst_True.Node.Name)
                {
                    return MkCSharpObjectCreationExpression(IdentifierName("PrtBoolValue"),
                            MkCSharpTrueLiteralExpression());
                }
                else if (op == PData.Cnst_False.Node.Name)
                {
                    return MkCSharpObjectCreationExpression(IdentifierName("PrtBoolValue"),
                            MkCSharpFalseLiteralExpression());
                }
                else if (op == PData.Cnst_This.Node.Name)
                {
                    return MkCSharpObjectCreationExpression(IdentifierName("PrtMachineValue"),
                            MkCSharpCastExpression("PrtImplMachine", IdentifierName("parent")));
                }
                else if (op == PData.Cnst_Nondet.Node.Name)
                {
                    return IdentifierName("$");
                }
                else if (op == PData.Cnst_FairNondet.Node.Name)
                {
                    return IdentifierName("$$");
                }
                else if (op == PData.Cnst_Null.Node.Name)
                {
                    return IdentifierName("@null");
                }
                else
                {
                    //op == PData.Cnst_Halt.Node.Name
                    return IdentifierName("halt");
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
                        return MkCSharpObjectCreationExpression(
                            IdentifierName("PrtBoolValue"),
                            PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, MkCSharpDot(MkCSharpCastExpression("PrtBoolValue", arg), "bl")));
                    }
                    else if (op == PData.Cnst_Neg.Node.Name)
                    {
                        return MkCSharpObjectCreationExpression(
                            IdentifierName("PrtIntValue"),
                            PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg), "nt")));
                    }
                    else if (op == PData.Cnst_Keys.Node.Name)
                    {
                        return MkCSharpInvocationExpression(MkCSharpDot(MkCSharpCastExpression("PrtMapValue", arg), "Keys"));
                    }
                    else if (op == PData.Cnst_Values.Node.Name)
                    {
                        return MkCSharpInvocationExpression(MkCSharpDot(MkCSharpCastExpression("PrtMapValue", arg), "Values"));
                    }
                    else
                    {
                        //  op == PData.Cnst_Sizeof.Node.Name
                        return MkCSharpObjectCreationExpression(
                                        IdentifierName("PrtIntValue"), 
                                        MkCSharpInvocationExpression(MkCSharpDot((ExpressionSyntax)arg, "Size")));
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
                        var arg1Int = MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                        var arg2Int = MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                        return MkCSharpObjectCreationExpression(
                                IdentifierName("PrtIntValue"), 
                                BinaryExpression(SyntaxKind.AddExpression, arg1Int, arg2Int));
                    }
                    else if (op == PData.Cnst_Sub.Node.Name)
                    {
                        var arg1Int = MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                        var arg2Int = MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                        return MkCSharpObjectCreationExpression(
                                IdentifierName("PrtIntValue"), 
                                BinaryExpression(SyntaxKind.SubtractExpression, arg1Int, arg2Int));
                    }
                    else if (op == PData.Cnst_Mul.Node.Name)
                    {
                        var arg1Int = MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                        var arg2Int = MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                        return MkCSharpObjectCreationExpression(
                                IdentifierName("PrtIntValue"), 
                                BinaryExpression(SyntaxKind.MultiplyExpression, arg1Int, arg2Int));
                    }
                    else if (op == PData.Cnst_IntDiv.Node.Name)
                    {
                        var arg1Int = MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                        var arg2Int = MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                        return MkCSharpObjectCreationExpression(
                                IdentifierName("PrtIntValue"), 
                                BinaryExpression(SyntaxKind.DivideExpression, arg1Int, arg2Int));
                    }
                    else if (op == PData.Cnst_And.Node.Name)
                    {
                        var arg1Bool = MkCSharpDot(MkCSharpCastExpression("PrtBoolValue", arg1), "bl");
                        var arg2Bool = MkCSharpDot(MkCSharpCastExpression("PrtBoolValue", arg2), "bl");
                        return MkCSharpObjectCreationExpression(
                                IdentifierName("PrtBoolValue"), 
                                BinaryExpression(SyntaxKind.LogicalAndExpression, arg1Bool, arg2Bool));
                    }
                    else if (op == PData.Cnst_Or.Node.Name)
                    {
                        var arg1Bool = MkCSharpDot(MkCSharpCastExpression("PrtBoolValue", arg1), "bl");
                        var arg2Bool = MkCSharpDot(MkCSharpCastExpression("PrtBoolValue", arg2), "bl");
                        return MkCSharpObjectCreationExpression(
                                IdentifierName("PrtBoolValue"), 
                                BinaryExpression(SyntaxKind.LogicalOrExpression, arg1Bool, arg2Bool));
                    }
                    else if (op == PData.Cnst_Eq.Node.Name)
                    {
                        return MkCSharpObjectCreationExpression(
                                IdentifierName("PrtBoolValue"), 
                                MkCSharpInvocationExpression(MkCSharpDot(arg1, "Equals"), arg2));
                    }
                    else if (op == PData.Cnst_NEq.Node.Name)
                    {
                        return MkCSharpObjectCreationExpression(
                                IdentifierName("PrtBoolValue"),
                                PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, MkCSharpInvocationExpression(MkCSharpDot(arg1, "Equals"), arg2)));
                    }
                    else if (op == PData.Cnst_Lt.Node.Name)
                    {
                        var arg1Int = MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                        var arg2Int = MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                        return MkCSharpObjectCreationExpression(
                                IdentifierName("PrtBoolValue"), 
                                BinaryExpression(SyntaxKind.LessThanExpression, arg1Int, arg2Int));
                    }
                    else if (op == PData.Cnst_Le.Node.Name)
                    {
                        var arg1Int = MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                        var arg2Int = MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                        return MkCSharpObjectCreationExpression(
                                IdentifierName("PrtBoolValue"), 
                                BinaryExpression(SyntaxKind.LessThanOrEqualExpression, arg1Int, arg2Int));
                    }
                    else if (op == PData.Cnst_Gt.Node.Name)
                    {
                        var arg1Int = MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                        var arg2Int = MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                        return MkCSharpObjectCreationExpression(
                                IdentifierName("PrtBoolValue"), 
                                BinaryExpression(SyntaxKind.GreaterThanExpression, arg1Int, arg2Int));
                    }
                    else if (op == PData.Cnst_Ge.Node.Name)
                    {
                        var arg1Int = MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg1), "nt");
                        var arg2Int = MkCSharpDot(MkCSharpCastExpression("PrtIntValue", arg2), "nt");
                        return MkCSharpObjectCreationExpression(
                                IdentifierName("PrtBoolValue"), 
                                BinaryExpression(SyntaxKind.GreaterThanOrEqualExpression, arg1Int, arg2Int));
                    }
                    else if (op == PData.Cnst_Idx.Node.Name)
                    {
                        var type = LookupType(GetArgByIndex(ft, 1));
                        var typeOp = ((Id)type.Function).Name;
                        if (typeOp == PData.Con_SeqType.Node.Name)
                        {
                            arg1 = MkCSharpCastExpression("PrtSeqValue", arg1);
                        }
                        else
                        {
                            // op == PData.Con_MapType.Node.Name
                            arg1 = MkCSharpCastExpression("PrtMapValue", arg1);
                        }
                        var lookupExpr = MkCSharpInvocationExpression(MkCSharpDot(arg1, "Lookup"), arg2);
                        if (lhsStack.Count > 0 && lhsStack.Peek())
                        {
                            return lookupExpr;
                        }
                        else
                        {
                            return MkCSharpInvocationExpression(MkCSharpDot(lookupExpr, "Clone"));
                        }

                    }
                    else
                    {
                        // op == PData.Cnst_In.Node.Name
                        return MkCSharpObjectCreationExpression(IdentifierName("PrtBoolValue"),
                                                                MkCSharpInvocationExpression(MkCSharpDot(MkCSharpCastExpression("PrtMapValue", arg2), "Contains"), arg1));
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
                    var accessExpr = MkCSharpElementAccessExpression(MkCSharpDot(MkCSharpCastExpression("PrtTupleValue", arg), "fieldValues"), fieldIndex);
                    if (lhsStack.Count > 0 && lhsStack.Peek())
                    {
                        return accessExpr;
                    }
                    else
                    {
                        return MkCSharpInvocationExpression(MkCSharpDot(accessExpr, "Clone"));
                    }
                }
            }

            SyntaxNode FoldDefault(FuncTerm ft, List<SyntaxNode> children)
            {
                var typeArg = (FuncTerm)GetArgByIndex(ft, 0);
                return MkCSharpInvocationExpression(
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
                    return MkCSharpInvocationExpression(
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
                var tupType = LookupType(ft);
                children.Insert(0, pToCSharp.typeContext.PTypeToCSharpExpr(tupType));
                return MkCSharpObjectCreationExpression(IdentifierName("PrtTupleValue"), children.ToArray());
            }

            SyntaxNode FoldNamedTuple(FuncTerm ft, List<SyntaxNode> children)
            {
                var tupType = LookupType(ft);
                children.Insert(0, pToCSharp.typeContext.PTypeToCSharpExpr(tupType));
                return MkCSharpObjectCreationExpression(IdentifierName("PrtNamedTupleValue"), children.ToArray());
            }

            private ExpressionSyntax MkPayload(SyntaxNode tupTypeExpr, List<SyntaxNode> args)
            {
                if (args.Count == 0)
                {
                    return IdentifierName("@null");
                }
                else if (args.Count == 1)
                {
                    return (ExpressionSyntax)args[0];
                }
                else
                {
                    SyntaxNode[] pars = new SyntaxNode[args.Count+1];
                    pars[0] = tupTypeExpr;
                    for (int i = 0; i < args.Count; i++)
                    {
                        pars[i+1] = args[i];
                    }
                    return MkCSharpObjectCreationExpression(IdentifierName("PrtTupleValue"), pars);
                }
            }

            SyntaxNode FoldGoto(FuncTerm ft, List<SyntaxNode> children)
            {
                var qualifiedStateName = (FuncTerm)GetArgByIndex(ft, 0);
                var stateName = GetNameFromQualifiedName(owner.machineName, qualifiedStateName);
                var stateExpr = IdentifierName(stateName);
                MachineInfo machineInfo = pToCSharp.allMachines[owner.machineName];
                string stateEntryActionName = machineInfo.stateNameToStateInfo[stateName].entryActionName;
                FunInfo entryFunInfo = pToCSharp.allStaticFuns.ContainsKey(stateEntryActionName)
                                        ? pToCSharp.allStaticFuns[stateEntryActionName]
                                        : machineInfo.funNameToFunInfo[stateEntryActionName];
                var payloadVar = MkPayload(pToCSharp.typeContext.PTypeToCSharpExpr(entryFunInfo.PayloadType), children);
                var traceStmt = MkCSharpPrint(string.Format("<GotoLog> Machine {0}-{{0}} goes to {{1}}\\n", owner.machineName), MkCSharpDot("parent", "instanceNumber"), MkCSharpDot(stateExpr, "name"));
                var assignStmt1 = MkCSharpSimpleAssignmentExpressionStatement(MkCSharpDot("parent", "currentTrigger"), IdentifierName("@null"));
                var assignStmt2 = MkCSharpSimpleAssignmentExpressionStatement(MkCSharpDot("parent", "currentPayload"), payloadVar);
                var assignStmt3 = MkCSharpSimpleAssignmentExpressionStatement(MkCSharpDot("parent", "destOfGoto"), stateExpr);
                var createRetCtxt = ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("parent", "PrtFunContGoto")));
                return Block(traceStmt, assignStmt1, assignStmt2, assignStmt3, createRetCtxt, ReturnStatement());
            }

            SyntaxNode FoldRaise(FuncTerm ft, List<SyntaxNode> children)
            {
                var eventExpr = (ExpressionSyntax)children[0];
                children.RemoveAt(0);
                var eventPayloadTypeExpr = MkCSharpDot(eventExpr, "evt", "payloadType");
                var payloadVar = MkPayload(eventPayloadTypeExpr, children);
                var equalsExpr = MkCSharpInvocationExpression(MkCSharpDot(eventExpr, "Equals"), IdentifierName("@null"));
                var assertStmt = MkCSharpAssert(MkCSharpNot(equalsExpr), pToCSharp.SpanToString(pToCSharp.LookupSpan(ft), "Raised event must be non-null"));
                var traceStmt = MkCSharpPrint(string.Format("<RaiseLog> Machine {0}-{{0}} raised Event {{1}}\\n", owner.machineName), MkCSharpDot("parent", "instanceNumber"), MkCSharpDot(MkCSharpCastExpression("PrtEventValue", eventExpr), "evt", "name"));
                var assignStmt1 = MkCSharpSimpleAssignmentExpressionStatement(MkCSharpDot("parent", "currentTrigger"), eventExpr);
                var assignStmt2 = MkCSharpSimpleAssignmentExpressionStatement(MkCSharpDot("parent", "currentPayload"), payloadVar);
                var returnStmt = ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("parent", "PrtFunContRaise")));
                return Block(assertStmt, traceStmt, assignStmt1, assignStmt2, returnStmt, ReturnStatement());
            }

            SyntaxNode FoldSend(FuncTerm ft, List<SyntaxNode> args)
            {
                var targetExpr = MkCSharpCastExpression("PrtMachineValue", args[0]);
                ExpressionSyntax eventExpr = MkCSharpCastExpression("PrtEventValue", args[1]);
                args.RemoveRange(0, 2);
                ExpressionSyntax tupleTypeExpr = MkCSharpDot(eventExpr, "evt", "payloadType");
                ExpressionSyntax payloadExpr = MkPayload(tupleTypeExpr, args);
                var invocationArgs = new ExpressionSyntax[]
                {
                    eventExpr, payloadExpr, IdentifierName("parent")
                };
                StatementSyntax enqueueEventStmt = ExpressionStatement(
                    MkCSharpInvocationExpression(
                        MkCSharpDot(MkCSharpDot(targetExpr, "mach"), "PrtEnqueueEvent"),
                        invocationArgs));

                invocationArgs = new ExpressionSyntax[]
                {
                    ThisExpression(),
                    MkCSharpDot("currFun", "locals"),
                    MkCSharpDot("currFun", "returnToLocation")
                };
                StatementSyntax contStmt = ExpressionStatement(
                    MkCSharpInvocationExpression(MkCSharpDot("parent", "PrtFunContSend"), invocationArgs));

                var afterLabel = GetLabelFromLabelId(GetFreshLabelId());
                StatementSyntax afterStmt = MkCSharpEmptyLabeledStatement(afterLabel);
                return Block(enqueueEventStmt, contStmt, ReturnStatement(), afterStmt);
            }

            SyntaxNode FoldAnnounce(FuncTerm ft, List<SyntaxNode> args)
            {
                ExpressionSyntax eventExpr = (ExpressionSyntax)MkCSharpCastExpression("PrtEventValue", args[0]);
                args.RemoveAt(0);
                ExpressionSyntax tupleTypeExpr = (ExpressionSyntax)MkCSharpDot(eventExpr, "payloadType");
                ExpressionSyntax payloadExpr = (ExpressionSyntax)MkPayload(tupleTypeExpr, args);
                var invocationArgs = new ExpressionSyntax[]
                {
                    eventExpr, payloadExpr, (ExpressionSyntax)IdentifierName("parent")
                };
                StatementSyntax announceEventStmt = ExpressionStatement(
                    MkCSharpInvocationExpression(MkCSharpDot("application", "Announce"), invocationArgs));
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
                    return MkCSharpAssert(MkCSharpDot((ExpressionSyntax)it.Current, "bl"), errorMsg);
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
                var calleeInfo = pToCSharp.allStaticFuns.ContainsKey(calleeName) ? pToCSharp.allStaticFuns[calleeName] : pToCSharp.allMachines[owner.machineName].funNameToFunInfo[calleeName];

                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                List<ExpressionSyntax> paramList = new List<ExpressionSyntax>();
                int parameterCount = 0;
                List<StatementSyntax> processOutput = new List<StatementSyntax>();
                foreach (var child in children)
                {
                    if (parameterCount == calleeInfo.parameterNames.Count)
                    {
                        // output variable
                        processOutput.Add(MkCSharpSimpleAssignmentExpressionStatement(child, MkCSharpDot("parent", "continuation", "retVal")));
                        break;
                    }
                    var calleeArg = calleeInfo.parameterNames[parameterCount];
                    var calleeArgInfo = calleeInfo.localNameToInfo[calleeArg];
                    paramList.Add((ExpressionSyntax)child);
                    if (isSwapParameter[parameterCount])
                    {
                        processOutput.Add(
                            MkCSharpSimpleAssignmentExpressionStatement(
                                child, 
                                MkCSharpElementAccessExpression(MkCSharpDot("parent", "continuation", "retLocals"), MkCSharpNumericLiteralExpression(calleeArgInfo.index))));
                    }
                    parameterCount++;
                }

                var beforeLabelId = GetFreshLabelId();
                var beforeLabel = GetLabelFromLabelId(beforeLabelId);
                stmtList.Add(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("parent", "PrtPushFunStackFrame"), IdentifierName(calleeName), MkCSharpInvocationExpression(MkCSharpDot(calleeName, "CreateLocals"), paramList.ToArray()))));
                stmtList.Add(MkCSharpEmptyLabeledStatement(beforeLabel));
                stmtList.Add(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot(calleeName, "Execute"), IdentifierName("application"), IdentifierName("parent"))));
                var elseStmt = Block(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("parent", "PrtPushFunStackFrame"), IdentifierName(calleeName), MkCSharpDot("currFun", "locals"), MkCSharpNumericLiteralExpression(beforeLabelId))),
                                     ReturnStatement());
                stmtList.Add(IfStatement(
                                     MkCSharpEq(MkCSharpDot("parent", "continuation", "reason"), IdentifierName("PrtContinuationReason.Return")),
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
                    stmtList.Add(MkCSharpSimpleAssignmentExpressionStatement(MkCSharpDot("parent", "currentTrigger"), IdentifierName("@null")));
                    stmtList.Add(MkCSharpSimpleAssignmentExpressionStatement(MkCSharpDot("parent", "currentPayload"), IdentifierName("@null")));
                    stmtList.Add(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("parent", "PrtPopState"), MkCSharpTrueLiteralExpression())));
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
                                            MkCSharpInvocationExpression(
                                                MkCSharpDot(MkCSharpCastExpression("PrtTupleValue", dest), "Update"),
                                                MkCSharpNumericLiteralExpression(fieldIndex),
                                                MkCSharpInvocationExpression(MkCSharpDot(src, "Clone"))));
                            }
                            else if (assignType == "XFER")
                            {
                                return ExpressionStatement(
                                            MkCSharpInvocationExpression(
                                                MkCSharpDot(MkCSharpCastExpression("PrtTupleValue", dest), "Update"),
                                                MkCSharpNumericLiteralExpression(fieldIndex),
                                                src));
                            }
                            else
                            {
                                // assignType = "SWAP" 
                                return MkCSharpSimpleAssignmentExpressionStatement(
                                            src,
                                            MkCSharpInvocationExpression(
                                                MkCSharpDot(MkCSharpCastExpression("PrtTupleValue", dest), "UpdateAndReturnOldValue"),
                                                MkCSharpNumericLiteralExpression(fieldIndex),
                                                src));
                            }
                        }
                        else if (index == null)
                        {
                            if (assignType == "NONE")
                            {
                                List<StatementSyntax> stmtList = new List<StatementSyntax>();
                                src = (ExpressionSyntax)TranslatePossibleNondet(src, stmtList);
                                stmtList.Add(MkCSharpSimpleAssignmentExpressionStatement(dest, MkCSharpInvocationExpression(MkCSharpDot(src, "Clone"))));
                                return Block(stmtList);
                            }
                            else if (assignType == "XFER")
                            {
                                return MkCSharpSimpleAssignmentExpressionStatement(dest, src);
                            }
                            else
                            {
                                // assignType == "SWAP"
                                return Block(
                                    MkCSharpSimpleAssignmentExpressionStatement(IdentifierName("swap"), dest),
                                    MkCSharpSimpleAssignmentExpressionStatement(dest, src),
                                    MkCSharpSimpleAssignmentExpressionStatement(src, IdentifierName("swap")));
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
                                                MkCSharpInvocationExpression(
                                                    MkCSharpDot(MkCSharpCastExpression("PrtSeqValue", dest), "Update"),
                                                    index,
                                                    MkCSharpInvocationExpression(MkCSharpDot(src, "Clone"))));
                                }
                                else if (assignType == "XFER")
                                {
                                    return ExpressionStatement(
                                                MkCSharpInvocationExpression(
                                                    MkCSharpDot(MkCSharpCastExpression("PrtSeqValue", dest), "Update"),
                                                    index,
                                                    src));
                                }
                                else
                                {
                                    // assignType == "SWAP"
                                    return MkCSharpSimpleAssignmentExpressionStatement(
                                                src,
                                                MkCSharpInvocationExpression(
                                                    MkCSharpDot(MkCSharpCastExpression("PrtSeqValue", dest), "UpdateAndReturnOldValue"),
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
                                                MkCSharpInvocationExpression(
                                                    MkCSharpDot(MkCSharpCastExpression("PrtMapValue", dest), "Update"),
                                                    index,
                                                    MkCSharpInvocationExpression(MkCSharpDot(src, "Clone"))));
                                }
                                else if (assignType == "XFER")
                                {
                                    return ExpressionStatement(
                                                MkCSharpInvocationExpression(
                                                    MkCSharpDot(MkCSharpCastExpression("PrtMapValue", dest), "Update"),
                                                    index,
                                                    src));
                                }
                                else
                                {
                                    // assignType == "SWAP"
                                    return MkCSharpSimpleAssignmentExpressionStatement(
                                                src,
                                                MkCSharpInvocationExpression(
                                                    MkCSharpDot(MkCSharpCastExpression("PrtMapValue", dest), "UpdateAndReturnOldValue"),
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
                            return ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot(MkCSharpCastExpression("PrtSeqValue", dest), "Remove"), src));
                        }
                        else
                        {
                            return ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot(MkCSharpCastExpression("PrtMapValue", dest), "Remove"), src));
                        }
                    }
                    else
                    {
                        // op == PData.Cnst_Insert.Node.Name
                        return ExpressionStatement(
                                    MkCSharpInvocationExpression(
                                        MkCSharpDot(MkCSharpCastExpression("PrtSeqValue", dest), "Insert"),
                                        MkCSharpElementAccessExpression(MkCSharpDot(MkCSharpCastExpression("PrtTupleValue", src), "fieldValues"), 0),
                                        MkCSharpElementAccessExpression(MkCSharpDot(MkCSharpCastExpression("PrtTupleValue", src), "fieldValues"), 1)));
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
                        stmtList.Add(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("parent", "PrtFunContReturn"), MkCSharpDot("currFun", "locals"))));
                    }
                    else
                    {
                        var returnExpr = (ExpressionSyntax)TranslatePossibleNondet(it.Current, stmtList);
                        stmtList.Add(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("parent", "PrtFunContReturnVal"), returnExpr, MkCSharpDot("currFun", "locals"))));
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
                    var condExpr = MkCSharpDot(MkCSharpCastExpression("PrtBoolValue", TranslatePossibleNondet(it.Current, stmtList)), "bl");
                    it.MoveNext();
                    var loopStart = pToCSharp.GetUnique(funName + "_loop_start");
                    var loopEnd = pToCSharp.GetUnique(funName + "_loop_end");
                    var body = it.Current;
                    stmtList.Add(MkCSharpEmptyLabeledStatement(loopStart));
                    stmtList.Add(IfStatement(PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, condExpr), MkCSharpGoto(loopEnd)));
                    stmtList.Add((StatementSyntax)body);
                    stmtList.Add(MkCSharpGoto(loopStart));
                    stmtList.Add(MkCSharpEmptyLabeledStatement(loopEnd));
                    return Block(stmtList);
                }
            }

            SyntaxNode FoldIte(FuncTerm ft, List<SyntaxNode> children)
            {
                using (var it = children.GetEnumerator())
                {
                    List<StatementSyntax> stmtList = new List<StatementSyntax>();
                    it.MoveNext();
                    var condExpr = MkCSharpDot(MkCSharpCastExpression("PrtBoolValue", TranslatePossibleNondet(it.Current, stmtList)), "bl");
                    it.MoveNext();
                    var thenStmt = it.Current;
                    it.MoveNext();
                    var elseStmt = it.Current;

                    var ifName = pToCSharp.GetUnique(funName + "_if");
                    var elseLabel = ifName + "_else";
                    var afterLabel = ifName + "_end";
                    stmtList.Add(IfStatement(PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, condExpr), MkCSharpGoto(elseLabel)));
                    stmtList.Add((StatementSyntax)thenStmt);
                    stmtList.Add(MkCSharpGoto(afterLabel));
                    stmtList.Add(MkCSharpEmptyLabeledStatement(elseLabel));
                    stmtList.Add((StatementSyntax)elseStmt);
                    stmtList.Add(MkCSharpEmptyLabeledStatement(afterLabel));
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
                stmtList.Add(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("parent", "PrtFunContNondet"), ThisExpression(), MkCSharpDot("currFun", "locals"), MkCSharpNumericLiteralExpression(afterLabelId))));
                stmtList.Add(ReturnStatement());
                stmtList.Add(MkCSharpEmptyLabeledStatement(afterLabel));
                return MkCSharpObjectCreationExpression(IdentifierName("PrtBoolValue"), MkCSharpInvocationExpression(MkCSharpDot("parent", "continuation", "ReturnAndResetNondet")));
            }
            #endregion

            public SyntaxNode MkFunStackFrameClass()
            {
                SyntaxList<MemberDeclarationSyntax> members = new SyntaxList<MemberDeclarationSyntax>();
                string frameClassName = funName + "_StackFrame";
                //public F1_Class_StackFrame(PrtFun fun, List<PrtValue> locs) : base(fun, locs) {}
                var pars = new List<SyntaxNode> { MkCSharpParameter(Identifier("locals"), MkCSharpGenericListType(IdentifierName("PrtValue"))),
                                                  MkCSharpParameter(Identifier("retLoc"), PredefinedType(Token(SyntaxKind.IntKeyword))) };
                SyntaxTokenList modifiers = new SyntaxTokenList();
                modifiers = modifiers.Add(Token(SyntaxKind.PublicKeyword));
                members = members.Add(MkCSharpConstructor(Identifier(frameClassName),
                                                          modifiers,
                                                          new List<SyntaxNode>() {
                                                              MkCSharpParameter(Identifier("fun"), (TypeSyntax) IdentifierName("PrtFun")),
                                                              MkCSharpParameter(Identifier("locs"), MkCSharpGenericListType(IdentifierName("PrtValue"))) },
                                                          MkCSharpConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                                                              MkCSharpArgumentList(IdentifierName("fun"), IdentifierName("locs"))),
                                                          new List<StatementSyntax>()));

                //public F2_Class_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base(fun, locs, retLocation) {}
                members = members.Add(MkCSharpConstructor(Identifier(frameClassName),
                                                         modifiers,
                                                         new List<SyntaxNode>() {
                                                              MkCSharpParameter(Identifier("fun"), (TypeSyntax) IdentifierName("PrtFun")),
                                                              MkCSharpParameter(Identifier("locs"), MkCSharpGenericListType(IdentifierName("PrtValue"))),
                                                              MkCSharpParameter(Identifier("retLocation"), PredefinedType(Token(SyntaxKind.IntKeyword))) },
                                                         MkCSharpConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                                                             MkCSharpArgumentList(IdentifierName("fun"), IdentifierName("locs"), IdentifierName("retLocation"))),
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
                members = members.Add((MemberDeclarationSyntax)MkCSharpMethodDeclaration(IdentifierName("PrtFunStackFrame"),
                                Identifier("Clone"),
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
                           (ExpressionSyntax)MkCSharpElementAccessExpression(
                               IdentifierName("locals"), ind)));
                    var setBody = SingletonList<StatementSyntax>((StatementSyntax)MkCSharpSimpleAssignmentExpressionStatement(
                            (ExpressionSyntax)MkCSharpElementAccessExpression(
                                IdentifierName("locals"), ind),
                            IdentifierName("value")));
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
                                IdentifierName(owner.machineName))
                            .WithVariables(
                                SingletonSeparatedList<VariableDeclaratorSyntax>(
                                    VariableDeclarator(
                                        Identifier("parent"))
                                    .WithInitializer(
                                        EqualsValueClause(
                                                MkCSharpCastExpression(owner.machineName, IdentifierName("_parent")))))))
                        .NormalizeWhitespace());
                }
                funStmts.Add(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName("PrtFunStackFrame"))
                        .WithVariables(
                            SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(
                                    Identifier("currFun"))
                                .WithInitializer(
                                    EqualsValueClause(
                                        InvocationExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("parent"),
                                                IdentifierName("PrtPopFunStackFrame"))))))))
                    .NormalizeWhitespace());

                funStmts.Add(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName("PrtImplMachine"))
                        .WithVariables(
                            SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(
                                    Identifier("createdMachine")))))
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
                                            LiteralExpression(
                                                SyntaxKind.NullLiteralExpression))))))
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
                    .NormalizeWhitespace();

                //foreach (var item in args)
                var stmt2 =
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
                        Block(stmt1, stmt2, stmt3))
                    .NormalizeWhitespace();

                return createLocalsMethodDecl;
            }
            public SyntaxNode MkCreateFunStackFrameMethod()
            {
                var body = SingletonList<StatementSyntax>(
                            ReturnStatement((ExpressionSyntax)MkCSharpObjectCreationExpression(
                                IdentifierName(funName + "_StackFrame"),
                                new SyntaxNode[] { ThisExpression(), IdentifierName("locals"), IdentifierName("retLoc") })));
                var pars = new List<SyntaxNode> { MkCSharpParameter(Identifier("locals"), MkCSharpGenericListType(IdentifierName("PrtValue"))),
                                                  MkCSharpParameter(Identifier("retLoc"), PredefinedType(Token(SyntaxKind.IntKeyword))) };
                return MkCSharpMethodDeclaration(IdentifierName("PrtFunStackFrame"), Identifier("CreateFunStackFrame"),
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
                    whereToAdd = (this.owner == null) ? PToCSharp.members : owner.machineMembers;

                    whereToAdd.Add(MkFuncClass());
                    //Variable declaration:
                    whereToAdd.Add(
                        FieldDeclaration(
                            VariableDeclaration(
                                IdentifierName(funClassName))
                            .WithVariables(
                                SingletonSeparatedList<VariableDeclaratorSyntax>(
                                    VariableDeclarator(
                                        Identifier(funName)))))
                        .WithModifiers(
                            TokenList(
                                new[]{
                                        Token(SyntaxKind.PublicKeyword),
                                        Token(SyntaxKind.StaticKeyword)}))
                        .NormalizeWhitespace());

                    //Add function variable instantiation to:
                    //PToCsharp class for global static functions;
                    //Main constructor for other functions
                    if (this.owner == null)
                    {
                        PToCSharp.members.Add(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName(funName),
                                    ObjectCreationExpression(
                                        IdentifierName(funClassName))
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
                                        IdentifierName(funClassName))
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
                MkFunctionDecl funDecl = new MkFunctionDecl(pair.Key, pair.Value, null, this);
                funDecl.AddFunClass();
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
            //stmt1: var machine = new Machine(this, machineMaxQueueSize, true);
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
                //TODO(question): 10 is the default maxQueueSize for Main machine
                //TODO: create "PrtImplMachine.DefaultMaxBuffer"
                fields.Add(generator.LocalDeclarationStatement(generator.IdentifierName("var"), "machine",
                               generator.ObjectCreationExpression(generator.IdentifierName(machineName),
                               new List<SyntaxNode>() { generator.IdentifierName("application"), generator.LiteralExpression(10),
                                                            generator.LiteralExpression(machineInfo.maxQueueSizeAssumed) })));
            }

            //stmt2: machine.currentPayload = payload;
            fields.Add(MkCSharpSimpleAssignmentExpressionStatement(
                MkCSharpDot("machine", "currentPayload"),
                IdentifierName("payload")));

            //stmt3: AddImplMachineToStateImpl(machine);
            fields.Add(generator.InvocationExpression(MkCSharpDot("application", "AddImplMachineToStateImpl"),
                                 new List<SyntaxNode>() { generator.IdentifierName("machine") }));

            //stmt4: return machine;
            fields.Add(generator.ReturnStatement(generator.IdentifierName("machine")));

            //public PrtImplMachine CreateMainMachine() {stmt1; stmt2; stmt3; stmt4; };
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
            //stmt1: var machine = new Machine(this);
            fields.Add(generator.LocalDeclarationStatement(generator.IdentifierName("var"), "machine",
                           generator.ObjectCreationExpression(generator.IdentifierName(machineName),
                           new List<SyntaxNode>() { generator.IdentifierName("application") })));

            //stmt2: AddSpecMachineToStateImpl(machine);
            fields.Add(generator.InvocationExpression(MkCSharpDot("application", "AddSpecMachineToStateImpl"),
                                 new List<SyntaxNode>() { generator.IdentifierName("machine") }));

            //public void CreateMainMachine() {stmt1; stmt2; };
            var methodPars = new SyntaxNode[] {
                    generator.ParameterDeclaration("application", generator.IdentifierName("StateImpl")) };
            var makeCreateMachineDecl = generator.MethodDeclaration(string.Format("CreateMachine_{0}", machineName), methodPars,
              null, null,
              Accessibility.Public, DeclarationModifiers.Static,
              statements: fields);
            members.Add(makeCreateMachineDecl);
        }

        private void MkMachineClasses()
        {
            //Debug only:
            Console.WriteLine("Number of machines: {0}", allMachines.Count());
            foreach (var pair in allMachines)
            {
                MkMachineClass mkMachine;
                //Debug only:
                Console.WriteLine("Next machine: name: {0}", pair.Key);
                mkMachine = new MkMachineClass(this, pair.Key, pair.Value);
                SyntaxNode node = mkMachine.MkClass();
                members.Add(node);
            }
        }
        internal class MkMachineClass
        {
            public PToCSharp translator;
            public string machineName;
            public MachineInfo machineInfo;
            public List<SyntaxNode> machineMembers = new List<SyntaxNode>();
            public List<StatementSyntax> mainConstructorFields = new List<StatementSyntax>();
            //keeps track of already encountered function names:
            public HashSet<string> processedFuns = new HashSet<string>();
            private int transition_count = 1;
            public MkMachineClass(PToCSharp ptoCsharp, string name, MachineInfo info)
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
            //public enum FunContext { EntryFun, ExitFun, Transitions, Dos } 
            public FunInfo GetFunInfo(string funName)
            {
                if (translator.allStaticFuns.ContainsKey(funName))
                {
                    return translator.allStaticFuns[funName];
                }
                else
                {
                    return machineInfo.funNameToFunInfo[funName];
                }
            }
            public SyntaxNode MkClass()
            {
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
                                                //TODO(bug): replace with init state name from machineInfo, + "_State"
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
                               (ExpressionSyntax)MkCSharpElementAccessExpression(
                                   IdentifierName("fields"), ind)));
                    var setBody = SingletonList<StatementSyntax>((StatementSyntax)MkCSharpSimpleAssignmentExpressionStatement(
                            (ExpressionSyntax)MkCSharpElementAccessExpression(
                                IdentifierName("fields"), ind),
                            IdentifierName("value")));
                    AccessorDeclarationSyntax[] accessorList = new AccessorDeclarationSyntax[]
                            { MkCSharpAccessor("get", getBody), MkCSharpAccessor("set", setBody)};
                    machineMembers.Add(MkCSharpPropertyDecl("PrtValue", pair.Key, modifiers, accessorList));
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
                    var defaultValue = MkCSharpInvocationExpression(IdentifierName("PrtValue.PrtMkDefaultValue"), translator.typeContext.PTypeToCSharpExpr(varInfo.type));
                    constructorStmtList.Add(ExpressionStatement(MkCSharpInvocationExpression(MkCSharpDot("fields", "Add"), defaultValue)));
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
                                                        IdentifierName(EventName(trigger))),
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
              accessibility: Accessibility.Public,
              baseType: generator.IdentifierName("StateImpl"),
              members: members);

            // Declare a namespace
            //Generate namespace name: <output file name, no ".cs"> 
            //int index = this.csharpFileName.LastIndexOf(".");
            //string namespaceName = this.csharpFileName.Substring(0, index);
            //namespace is "P.Program" for all P programs:
            var programNameSpaceDeclaration = generator.NamespaceDeclaration("P.Program", applicationcClassDeclaration);

            // Get a CompilationUnit (code file) for the generated code
            result = generator.CompilationUnit(
                        generator.NamespaceImportDeclaration("P.Runtime"),
                        generator.NamespaceImportDeclaration("System"),
                        generator.NamespaceImportDeclaration("System.Collections.Generic"),
                        programNameSpaceDeclaration).
                            NormalizeWhitespace();
        }

        private void EmitCSharpOutput(string fileName)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(fileName);
            file.WriteLine("#pragma warning disable CS0162, CS0164, CS0168");
            file.WriteLine(result);
            file.Close();
        }

        #endregion
    }
}
