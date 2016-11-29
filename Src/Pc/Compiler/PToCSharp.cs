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
        //TODO(replace): these are Zing-related; used here for compilation only
        public static SyntaxNode PrtMkDefaultValue = MkCSharpDot("PRT_VALUE", "PrtMkDefaultValue");
        public static SyntaxNode PrtCloneValue = MkCSharpDot("PRT_VALUE", "PrtCloneValue");
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
        //Possibly, all types-realted stuff should be moved to PToCSharp (similar to Zing compiler)
        //and  TypeTranslationContext to be instantiated in the PToCSharp constructor
        //After that, ConstructType in MkFunctionDecl can be replaced with TypeTranslationContext.ConstructType
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

            //These fields were used in Zing to represent strings as ints
            //private List<AST<Node>> fieldNameInitialization;
            //private Dictionary<string, AST<FuncTerm>> fieldNameToZingExpr;

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
                //fieldNameInitialization = new List<AST<Node>>();
                //fieldNameToZingExpr = new Dictionary<string, AST<FuncTerm>>(
                typeDeclaration = new List<FieldDeclarationSyntax>();
                typeInitialization = new List<StatementSyntax>();
                pTypeToCSharpExpr = new Dictionary<AST<Node>, SyntaxNode>();
            }

            //public AST<Node> InitializeFieldNamesAndTypes()
            //{
            //throw new NotImplementedException();
            //return MkCSharpSeq(MkCSharpSeq(), MkCSharpSeq(typeInitialization));
            //} 

            public IEnumerable<AST<Node>> MainVarDecls()
            {
                throw new NotImplementedException();
                //List<AST<Node>> varDecls = new List<AST<Node>>();
                //for (int i = 0; i < fieldCount; i++)
                //{
                //    varDecls.Add(MkCSharpVarDecl(string.Format("field_{0}_PRT_FIELD_NAME", i), Factory.Instance.MkCnst("PRT_FIELD_NAME"), ZingData.Cnst_Static));
                //}
                //for (int i = 0; i < typeCount; i++)
                //{
                //    varDecls.Add(MkCSharpVarDecl(string.Format("type_{0}_PRT_TYPE", i), Factory.Instance.MkCnst("PRT_TYPE"), ZingData.Cnst_Static));
                //}
                //return varDecls;
            }

            private AST<FuncTerm> GetField(string fieldName)
            {
                throw new NotImplementedException();
                //if (fieldNameToZingExpr.ContainsKey(fieldName))
                //return fieldNameToZingExpr[fieldName];
                //var retVal = MkCSharpDot("Main", string.Format("field_{0}_PRT_FIELD_NAME", fieldCount));
                //AddFieldNameInitialization(MkCSharpAssign(retVal, MkCSharpNew(Factory.Instance.MkCnst("PRT_FIELD_NAME"), ZingData.Cnst_Nil)));
                //fieldCount++;
                ////fieldNameToZingExpr[fieldName] = retVal;
                //return retVal;
            }

            private SyntaxNode GetType(string typeName)
            {
                //var retVal = MkCSharpDot("Main", typeName);
                var retVal = MkCSharpIdentifierName(typeName);
                typeCount++;
                return retVal;
            }

            private void AddFieldNameInitialization(AST<Node> n)
            {
                //fieldNameInitialization.Add(n);
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
                Console.WriteLine("typeKind in ConstructType: {0}", typeKind);

                if (typeKind == "BaseType")
                {
                    var primitiveType = ((Id)GetArgByIndex(type, 0)).Name;
                    if (primitiveType == "NULL")
                    {
                        var tmpVar = GetType("typeNull");
                        //type initialization:
                        //Main.typeNull = new PrNullType();
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
                    //throw new NotImplementedException();
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
                    //public static SyntaxNode MkCSharpArrayCreationExpression(TypeSyntax type, SyntaxNodeOrToken[] initializer)
                    AddTypeInitialization(MkCSharpSimpleAssignmentExpressionStatement(tmpVar, MkCSharpObjectCreationExpression(IdentifierName("PrtTupleType"),
                        MkCSharpArrayCreationExpression("PrtType", initializer.ToArray()))));
                    AddTypeDeclaration(MkCSharpFieldDeclaration(IdentifierName("PrtTupleType"), typeName, Token(SyntaxKind.PublicKeyword),
                                                                   Token(SyntaxKind.StaticKeyword)));
                    //TODO(confirm): what for is this code in PToZing.cs?
                    //Looks like it is not needed now
                    //for (int i = 0; i < memberTypes.Count; i++)
                    //{
                    //    AddTypeInitialization(MkZingCallStmt(MkZingCall(MkZingDot("PRT_TYPE", "PrtSetFieldType"), tupleType, Factory.Instance.MkCnst(i), memberTypes[i])));
                    //}
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
        //This is based on PToZing.TypeTranslationContext.ConstructType
        //and will eventually be replaced with smth similar
        private string ConstructType(FuncTerm type)
        {
            string typeKind = ((Id)type.Function).Name;
            if (typeKind == "BaseType")
            {
                var primitiveType = ((Id)GetArgByIndex(type, 0)).Name;
                if (primitiveType == "NULL")
                {
                    return "null";
                }
                else if (primitiveType == "BOOL")
                {
                    return "bool";
                }
                else if (primitiveType == "INT")
                {
                    return "int";
                }
                else if (primitiveType == "EVENT")
                {
                    return "event";
                }
                else if (primitiveType == "MACHINE")
                {
                    return "machine";
                }
                else
                {
                    //TODO(question): how to enable Debug.Assert?
                    Debug.Assert(primitiveType == "ANY", "Illegal BaseType");
                    return "any";
                }
            }
            else if (typeKind == "NameType")
            {
                return "NameType: not implemented yet";
            }
            else if (typeKind == "TupType")
            {
                return "TupType: not implemented yet";
            }
            else if (typeKind == "NmdTupType")
            {
                return "NmdTupType: not implemented";
            }
            else if (typeKind == "SeqType")
            {
                return "SeqType: not implemented";
            }
            else
            {
                // typeKind == "MapType"
                return "MapType: not implemented";
            }
        }
        //TODO(expand): write this method: consider all possible types: null, bool, int, event, machine, any
        private string GetTypeCreationExpr(string type)
        {
            if (type == "null")
            {
                return "new PrtNullType()";
            }
            else if (type == "bool")
            {
                return "new PrtBoolType()";
            }
            else if (type == "int")
            {
                return "new PrtIntType()";
            }
            else if (type == "event")
            {
                return "new PrtEventType()";
            }
            else if (type == "machine")
            {
                return "new PrtMachineType()";
            }
            else if (type == "any")
            {
                return "new PrtAnyType()";
            }
            else
            {
                return "enum, tuple, seq or map type not implemented yet";
            }
        }

        //public static AST<FuncTerm> MkCSharpAssign(AST<Node> lhs, AST<Node> rhs)
        //TODO(question): what's SimpleAssignmentExpression?
        public static SyntaxNode MkCSharpSimpleAssignmentExpressionStatement(SyntaxNode lhs, SyntaxNode rhs)
        {
            return ExpressionStatement(
                     AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        (ExpressionSyntax)lhs, (ExpressionSyntax)rhs))
                   .NormalizeWhitespace();
        }
        public static SyntaxNode MkCSharpCall(SyntaxNode methodExpr, params SyntaxNode[] args)
        {
            throw new NotImplementedException();
        }
        private static SyntaxNode MkCSharpReturn(SyntaxNode rVal)
        {
            throw new NotImplementedException();
        }
        public static SyntaxNode MkCSharpCallStmt(SyntaxNode callExpr, params SyntaxNode[] attrs)
        {
            throw new NotImplementedException();
        }
        public static SyntaxNode MkCSharpStringLiteralExpression(string name)
        {
            return LiteralExpression(SyntaxKind.StringLiteralExpression,
                Literal(name));
        }
        public static SyntaxNode MkCSharpFalseLiteralExpression()
        {
            return LiteralExpression(SyntaxKind.FalseLiteralExpression);

        }
        public static SyntaxNode MkCSharpTrueLiteralExpression()
        {
            return LiteralExpression(SyntaxKind.TrueLiteralExpression);

        }
        public static SyntaxNode MkCSharpNumericLiteralExpression(int arg)
        {
            return LiteralExpression(SyntaxKind.NumericLiteralExpression,
                Literal(arg));
        }
        public static SyntaxNode MkCSharpIdentifierName(string name)
        {
            //throw new NotImplementedException();
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
        //public static List<SyntaxNodeOrToken> MkCSharpParameterList(SyntaxNodeOrToken[] args)
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
        //TODO: write MkCSharpArgumentList: ArgumentSyntax elems separated by CommaTokens 
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
        public static SyntaxNode MkCSharpVarDecl(string varName, SyntaxNode varType, params SyntaxNode[] attrs)
        {
            throw new NotImplementedException();
        }
        //TODO(expand to any number of pars):
        public static SyntaxNode MkCSharpDot(string first, params string[] names)
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
        public static SyntaxNode MkCSharpDot(ExpressionSyntax first, string second)
        {
            return MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ParenthesizedExpression(first),
                        (SimpleNameSyntax)MkCSharpIdentifierName(second));
        }
        public static SyntaxNode MkCSharpElementAccessExpression(string name, int index)
        {
            return ElementAccessExpression(
                      IdentifierName(name))
                   .WithArgumentList(
                          BracketedArgumentList(
                             SingletonSeparatedList<ArgumentSyntax>(
                                 Argument(
                                    LiteralExpression(
                                       SyntaxKind.NumericLiteralExpression,
                                          Literal(index))))));
        }
        public static SyntaxNode MkCSharpCastExpression(string type, SyntaxNode expr)
        {
            return CastExpression(
                        IdentifierName(type),
                        ParenthesizedExpression((ExpressionSyntax)expr));
        }
        //OmittedArraySizeExpression case only:
        public static SyntaxNode MkCSharpArrayCreationExpression(string type, SyntaxNodeOrToken[] initializer)
        {
            //SyntaxNodeOrToken[] temp = new SyntaxNodeOrToken[initializer.Count()];
            //temp = initializer;
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
        //TODO(expand to any number of parameters)
        public static SyntaxNode MkCSharpObjectCreationExpression(SyntaxNode type, params SyntaxNode[] names)
        {
            //List<ArgumentSyntax> hd = new List<ArgumentSyntax>();
            List<SyntaxNode> hd = new List<SyntaxNode>();
            if (names.Length == 0)
            {
                return ObjectCreationExpression(
                    (TypeSyntax)type)
                   .WithArgumentList(
                     ArgumentList());
            }
            //TODO(fix): merge this case with the general case
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
        public static SyntaxNode MkCSharpInvocationExpression(SyntaxNode first, params ArgumentSyntax[] pars)
        {
            var args = MkCSharpArgumentList(pars);
            return InvocationExpression((ExpressionSyntax)first)
                    .WithArgumentList(
                         ArgumentList(
                                SeparatedList<ArgumentSyntax>(args)));
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
        public static SyntaxNode MkCSharpIfThen(SyntaxNode cond, SyntaxNode then)
        {
            throw new NotImplementedException();
        }

        public static SyntaxNode MkCSharpIfThenElse(SyntaxNode cond, SyntaxNode thenstmt, SyntaxNode elsestmt)
        {
            throw new NotImplementedException();
        }
        public static SyntaxNode MkCSharpGoto(string labelName)
        {
            throw new NotImplementedException();
        }
        public static SyntaxNode MkCSharpBlock(string label, SyntaxNode body)
        {
            throw new NotImplementedException();
        }

        public static SyntaxNode MkCSharpBlocks(params SyntaxNode[] blocks)
        {
            throw new NotImplementedException();
        }

        public static SyntaxNode MkCSharpBlocks(List<SyntaxNode> blocks)
        {
            throw new NotImplementedException();
        }
        private static SyntaxNode MkCSharpNew(SyntaxNode constructedType, SyntaxNode optionalSize)
        {
            throw new NotImplementedException();
        }
        public static SyntaxNode MkCSharpAssert(SyntaxNode condition)
        {
            throw new NotImplementedException(); ;
        }

        private static SyntaxNode MkCSharpAssert(SyntaxNode condition, string msg)
        {
            throw new NotImplementedException();
        }
        public static SyntaxNode MkCSharpEq(SyntaxNode e1, SyntaxNode e2)
        {
            throw new NotImplementedException();
        }
        public static SyntaxNode MkCSharpAdd(SyntaxNode a, SyntaxNode b)
        {
            throw new NotImplementedException();
        }
        private static SyntaxNode MkCSharpIndex(SyntaxNode baseExp, SyntaxNode indExp)
        {
            throw new NotImplementedException();
        }
        public static SyntaxNode MkCSharpSeq(params SyntaxNode[] stmts)
        {
            throw new NotImplementedException();
           
        }
        public static SyntaxNode MkCSharpSeq(List<SyntaxNode> stmts)
        {
            throw new NotImplementedException();
        }
        public static SyntaxNode MkCSharpEvent(string eventName)
        {
            throw new NotImplementedException();
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
            //TODO(expand): add inits for all fields
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

            //These fields are copied from ZingFoldContext (not needed for C#)
            private PToCSharp pToCSharp;
            //sideEffectsStack is used for non-atomic stmts: Seq, ITE, While in the translation of expressions.
            //Translation of a P expression should result in a zing expression so that translated expressions can be 
            //composed; but compilation of some P expressions results in a complicated piece of code containing statements as well.  
            //These statements are emitted to the side-effect stack and popped later.  
            //We should be able to do without this stack in C# code gen because C# method calls can be nested inside expressions
            public Stack<List<AST<Node>>> sideEffectsStack;
            //locals contain temp vars for storing intermediate results, for example, in translation of expressions
            public List<Tuple<SyntaxNode, string>> locals;
            public Stack<bool> lhsStack;
            //labels are used for "continuations" in send, new, nondet, receive
            private Dictionary<string, int> labels;

            public MkFunctionDecl(string funName, FunInfo funInfo, bool isGlobalStatic, MkMachineClass owner, PToCSharp pToCSharp)
            {
                this.funName = funName;
                this.funType = funName + "_Class";
                this.funInfo = funInfo;
                this.isGlobalStatic = isGlobalStatic;
                this.owner = owner;
                this.pToCSharp = pToCSharp;
                //GenerateTypeInfo(pToCSharp.modelWithTypes);
            }
            
            public int LabelToId(string l)
            {
                return labels[l];
            }

            public string GetFreshLabel()
            {
                var l = pToCSharp.GetUnique(funName);
                labels[l] = labels.Count + 1;
                return l;
            }

            public SyntaxNode EmitLabelPrelude()
            {
               // var prelude = new List<AST<Node>>();
               // var tmpVar = GetTmpVar(Factory.Instance.MkCnst("StackFrame"), "retTo");

               // //prelude.Add(PToCSharp.MkCSharpAssign(tmpVar, PToCSharp.MkCSharpCall(MkCSharpDot("entryCtxt", "PopReturnTo"))));
               // //prelude.Add(PToCSharp.MkCSharpAssign(MkCSharpIdentifierName("locals"), MkCSharpDot(tmpVar, "locals")));
               // //prelude.Add(PToCSharp.MkCSharpIfThen(PToCSharp.MkCSharpEq(MkCSharpDot(tmpVar, "pc"), Factory.Instance.MkCnst(0)), MkCSharpGoto("start")));

               // foreach (var l in labels.Keys)
               // {
               //     prelude.Add(PToCSharp.MkCSharpIfThen(PToCSharp.MkCSharpEq(MkCSharpDot(tmpVar, "pc"), Factory.Instance.MkCnst(labels[l])), MkCSharpGoto(l)));
               // }

               // //TODO(Zing to CSharp): convert to CSharpData
               ///prelude.Add(MkCSharpAssert(ZingData.Cnst_False, "Internal error"));

               // return PToCSharp.MkCSharpSeq(prelude);
                throw new NotImplementedException();
            }

            public void AddSideEffect(SyntaxNode stmt)
            {
                //this.sideEffectsStack.Peek().Add(stmt);
            }

            public void PushSideEffectStack()
            {
                this.sideEffectsStack.Push(new List<AST<Node>>());
            }

            // type must be CSharp type
            public SyntaxNode GetTmpVar(SyntaxNode type, string baseName)
            {
                var tmpVarName = pToCSharp.GetUnique(baseName);
                var tmpVar = MkCSharpIdentifierName(tmpVarName);
                this.locals.Add(new Tuple<SyntaxNode, string>(type, tmpVarName));
                return tmpVar;
            }

            public AST<Node> EmitCSharpSideEffects(AST<Node> stmt)
            {
                throw new NotImplementedException();
                //Debug.Assert(sideEffectsStack.Count > 0);
                //var sideEffects = sideEffectsStack.Pop();

                //if (sideEffects.Count > 0)
                //{
                //    sideEffects.Add(stmt);
                //    return PToCSharp.MkCSharpSeq(sideEffects);
                //}
                //else
                //{
                //    return stmt;
                //}
            }

            public IEnumerable<AST<Node>> EmitLocals()
            {
                throw new NotImplementedException();
                //return locals.Select(loc => PToCSharp.MkCSharpVarDecl(loc.Item2, loc.Item1));
            }

            private FuncTerm LookupType(Node node)
            {
                //return entityInfo.typeInfo[Factory.Instance.ToAST(node)];
                return funInfo.typeInfo[Factory.Instance.ToAST(node)];
            }
            #region FoldUnfod
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
                        PushSideEffectStack();
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
                        PushSideEffectStack();
                        yield return it.Current;
                        it.MoveNext();
                        PushSideEffectStack();
                        yield return it.Current;
                    }
                }
                else if (funName == PData.Con_While.Node.Name)
                {
                    using (var it = ft.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        PushSideEffectStack();
                        yield return it.Current;
                        it.MoveNext();
                        PushSideEffectStack();
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
            //Eliminating ZingTranslationInfo: using AST<Node> instead
            //TODO(questoin): two options to use for the generic Roslyn type that Fold returns:
            //SyntaxNode and CSharpSyntaxNode.
            //Second parameter of Fold should return List<SyntaxNode> or List<CSharpSyntaxNode>
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
                //List<AST<Node>> eventStmts = new List<AST<Node>>();
                //List<AST<Node>> funStmts = new List<AST<Node>>();

                //for (int i = 0; i < eventNames.Count; i++)
                //{
                //    var beforeLabel = GetFreshLabel();
                //    var eventName = eventNames[i];
                //    var funName = funNames[i];
                //    var calleeInfo = pToCSharp.allStaticFuns.ContainsKey(funName) ? pToCSharp.allStaticFuns[funName] : pToCSharp.allMachines[owner.machName].funNameToFunInfo[funName];
                //    Debug.Assert(calleeInfo.isAnonymous);
                //    List<SyntaxNode> ifStmts = new List<SyntaxNode>();
                //    ifStmts.Add(MkCSharpSimpleAssignmentExpressionStatement(MkCSharpIndex(MkCSharpIdentifierName("locals"), Factory.Instance.MkCnst(calleeInfo.localNameToInfo[calleeInfo.PayloadVarName].index)), MkCSharpCall(PrtCloneValue, MkCSharpDot("myHandle", "currentArg"))));
                //    foreach (var calleeLocal in calleeInfo.localNames)
                //    {
                //        var calleeLocalInfo = calleeInfo.localNameToInfo[calleeLocal];
                //        ifStmts.Add(MkCSharpSimpleAssignmentExpressionStatement(MkCSharpIndex(MkCSharpIdentifierName("locals"), Factory.Instance.MkCnst(calleeLocalInfo.index)), MkCSharpCall(PrtMkDefaultValue, pToCSharp.typeContext.PTypeToZingExpr(calleeLocalInfo.type))));
                //    }
                //    ifStmts.Add(MkCSharpCallStmt(MkCSharpCall(MkCSharpDot("entryCtxt", "PushReturnTo"), Factory.Instance.MkCnst(0), MkCSharpIdentifierName("locals"))));
                //    ifStmts.Add(MkCSharpGoto(beforeLabel));
                //    eventStmts.Add(MkCSharpIfThen(MkCSharpEq(MkCSharpDot("myHandle", "currentEvent"), MkCSharpEvent(eventName)), MkCSharpSeq(ifStmts)));
                //    if (pToCSharp.allStaticFuns.ContainsKey(funName))
                //    {
                //        funStmts.Add(MkCSharpBlock(beforeLabel, MkCSharpCallStmt(MkCSharpCall(MkCSharpDot("Main", funName), MkCSharpIdentifierName("myHandle"), MkCSharpIdentifierName("entryCtxt")))));
                //    }
                //    else
                //    {
                //        funStmts.Add(MkCSharpBlock(beforeLabel, MkCSharpCallStmt(MkCSharpCall(MkCSharpIdentifierName(funName), MkCSharpIdentifierName("entryCtxt")))));
                //    }
                //    funStmts.Add(MkCSharpIfThenElse(
                //                         MkCSharpEq(MkCSharpDot("entryCtxt", "reason"), MkCSharpDot("ContinuationReason", "Return")),
                //                         MkCSharpGoto(afterAfterLabel),
                //                         MkCSharpSeq(MkCSharpCallStmt(MkCSharpCall(MkCSharpDot("entryCtxt", "PushReturnTo"), Factory.Instance.MkCnst(LabelToId(beforeLabel)), MkCSharpIdentifierName("locals"))),
                //                                   MkCSharpReturn(ZingData.Cnst_Nil))));
                //}
                //List<AST<Node>> stmts = new List<AST<Node>>();
                //stmts.AddRange(eventStmts);
                //stmts.Add(MkCSharpAssert(ZingData.Cnst_False));
                //stmts.AddRange(funStmts);
                //return stmts;
            }
            SyntaxNode FoldReceive(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
                //List<AST<Node>> stmts = new List<AST<Node>>();
                //List<string> eventNames = new List<string>();
                //List<string> funNames = new List<string>();
                //var cases = GetArgByIndex(ft, 0) as FuncTerm;
                //while (cases != null)
                //{
                //    Node evt = GetArgByIndex(cases, 0);
                //    string eventName = null;
                //    if (evt is Cnst)
                //    {
                //        eventName = (evt as Cnst).GetStringValue();
                //    }
                //    else if ((evt as Id).Name == "NULL")
                //    {
                //        eventName = NullEvent;
                //    }
                //    else
                //    {
                //        eventName = HaltEvent;
                //    }
                //    eventNames.Add(eventName);
                //    stmts.Add(MkCSharpAssign(MkCSharpDot("myHandle", "receiveSet"), MkCSharpAdd(MkCSharpDot("myHandle", "receiveSet"), MkCSharpEvent(eventName))));
                //    var fun = GetArgByIndex(cases, 1);
                //    string funName = pToCSharp.anonFunToName[Factory.Instance.ToAST(fun)];
                //    funNames.Add(funName);
                //    cases = GetArgByIndex(cases, 2) as FuncTerm;
                //}
                //var afterLabel = GetFreshLabel();
                //stmts.Add(MkCSharpCallStmt(MkCSharpCall(MkCSharpDot("entryCtxt", "Receive"), Factory.Instance.MkCnst(LabelToId(afterLabel)), MkCSharpIdentifierName("locals"))));
                //stmts.Add(MkCSharpReturn(ZingData.Cnst_Nil));
                //stmts.Add(MkCSharpBlock(afterLabel, ZingData.Cnst_Nil));
                //var afterAfterLabel = GetFreshLabel();
                //stmts.AddRange(CaseFunCallHelper(eventNames, funNames, afterAfterLabel));
                //stmts.Add(MkCSharpBlock(afterAfterLabel, ZingData.Cnst_Nil));
                //AST<Node> node = MkCSharpSeq(stmts);
                //return node;
            }
            //In the context of expressions only; no children
            SyntaxNode FoldName(FuncTerm ft, List<SyntaxNode> children)
            {
                return null;
                var n = GetArgByIndex(ft, 0);
                if (n.NodeKind == NodeKind.Cnst)
                {
                    //var tmpVar = GetTmpVar(PrtValue, "tmp");
                    //AddSideEffect(MkCSharpSimpleAssignmentExpressionStatement(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeInt.Node))));
                    //AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetInt"), tmpVar, Factory.Instance.ToAST(n))));
                    //return new ZingTranslationInfo(tmpVar);
                    //return MkCSharpNumericLiteralExpression((Factory.Instance.ToAST(n))
                }

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
                throw new NotImplementedException();
            }
            SyntaxNode FoldUnApp(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            SyntaxNode FoldBinApp(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
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
            //SyntaxNode FoldRaise(FuncTerm ft, List<SyntaxNode> children)
            //{
            //    throw new NotImplementedException();
            //}
            //SyntaxNode FoldSend(FuncTerm ft, List<SyntaxNode> children)
            //{
            //    //code to be generated:
            //    //Line 1 (template everything except event and <payload value>): 
            //    //parent.PrtEnqueueEvent(event, <payload value>, parent);
            //    //Example:parent.PrtEnqueueEvent(dummy, PrtValue.NullValue, parent);
            //    //public override void PrtEnqueueEvent(PrtValue e, PrtValue arg, PrtMachine source)
            //    //event: children[1]
            //    //<payload value>: compute from children[2-children.Count()]

            //    //Line 2 (template everything): 
            //    //parent.PrtFunContSend(this, currFun.locals, currFun.returnTolocation);
            //    //TODO(question):check that the last parameter is correct
            //    //Example: parent.PrtFunContSend(this, currFun.locals, 1);
            //    //public void PrtFunContSend(PrtFun fun, List<PrtValue> locals, int ret)

            //    //List<AST<Node>> args = new List<AST<Node>>(children.Select(x => x));                  
            //    ExpressionSyntax eventExpr;
            //    ExpressionSyntax payloadExpr;
            //    var enqueueEvent =
            //        ExpressionStatement(
            //            InvocationExpression(
            //                MemberAccessExpression(
            //                    SyntaxKind.SimpleMemberAccessExpression,
            //                    IdentifierName("parent"),
            //                    IdentifierName("PrtEnqueueEvent")))
            //            .WithArgumentList(
            //                ArgumentList(
            //                    SeparatedList<ArgumentSyntax>(
            //                        new SyntaxNodeOrToken[]{
            //                            Argument(
            //                                //TODO: replace with real expr
            //                                IdentifierName("eventExpr")),
            //                            Token(SyntaxKind.CommaToken),
            //                            Argument(
            //                                //TODO: replace with real expr
            //                                IdentifierName("payloadExpr")),
            //                            Token(SyntaxKind.CommaToken),
            //                            Argument(
            //                                IdentifierName("parent"))}))))
            //        .NormalizeWhitespace();

            //    var contSend =
            //        ExpressionStatement(
            //            InvocationExpression(
            //                MemberAccessExpression(
            //                    SyntaxKind.SimpleMemberAccessExpression,
            //                    IdentifierName("parent"),
            //                    IdentifierName("PrtFunContSend")))
            //            .WithArgumentList(
            //                ArgumentList(
            //                    SeparatedList<ArgumentSyntax>(
            //                        new SyntaxNodeOrToken[]{
            //                            Argument(
            //                                ThisExpression()),
            //                            Token(SyntaxKind.CommaToken),
            //                            Argument(
            //                                MemberAccessExpression(
            //                                    SyntaxKind.SimpleMemberAccessExpression,
            //                                    IdentifierName("currFun"),
            //                                    IdentifierName("locals"))),
            //                            Token(SyntaxKind.CommaToken),
            //                            Argument(
            //                                MemberAccessExpression(
            //                                    SyntaxKind.SimpleMemberAccessExpression,
            //                                    IdentifierName("currFun"),
            //                                    IdentifierName("returnTolocation")))}))))
            //        .NormalizeWhitespace();


            //    throw new NotImplementedException();
            //}
            SyntaxNode FoldAnnounce(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            SyntaxNode FoldNullStmt(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            SyntaxNode FoldAssert(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            SyntaxNode FoldFunStmt(FuncTerm ft, List<SyntaxNode> children)
            {
                //throw new NotImplementedException();
                return FoldFunApp(ft, children);
            }
            SyntaxNode FoldNulStmt(FuncTerm ft, List<SyntaxNode> children)
            {
                //throw new NotImplementedException();
                var op = ((Id)GetArgByIndex(ft, 0)).Name;
                if (op == PData.Cnst_Pop.Node.Name)
                {
                    //TODO(sideEffect: modify for C#)
                    //ctxt.AddSideEffect(MkZingAssign(MkZingDot("myHandle", "currentEvent"), MkZingIdentifier("null")));
                    //ctxt.AddSideEffect(MkZingAssign(MkZingDot("myHandle", "currentArg"), MkZingIdentifier("null")));
                    //ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Pop"))));
                    //ctxt.AddSideEffect(MkZingReturn(ZingData.Cnst_Nil));
                }
                //return (SyntaxNode) (ZingData.Cnst_Nil);
                return MkCSharpIdentifierName("NIL");
            }
            SyntaxNode FoldPrint(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            SyntaxNode FoldBinStmt(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            SyntaxNode FoldReturn(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            SyntaxNode FoldWhile(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            SyntaxNode FoldIte(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            SyntaxNode FoldSeq(FuncTerm ft, List<SyntaxNode> children)
            {
                throw new NotImplementedException();
            }
            #endregion

            //TODO(expand): stopped here:
            public List<StatementSyntax> MkFunctionBody()
            {
                //TODO(question): how to fix type of ch?
                var funBody = Factory.Instance.ToAST(funInfo.body).Compute<SyntaxNode>(
                    x => Unfold(x),
                    (x, ch) => Fold(x, ch.ToList()));
                return new List<StatementSyntax>();
            }
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
                    Console.WriteLine("Next local of function {0} is {1}", funName, varName);

                    int ind = pair.Value.index;
                    Console.WriteLine("Index of the next local {0} is {1}", varName, ind);
                    //Example: public PrtValue Par1 {get {return locals[0];} {set {locals[0] = value;}}

                    modifiers = new SyntaxTokenList();
                    modifiers = modifiers.Add(Token(SyntaxKind.PublicKeyword));
                    var getBody = SingletonList<StatementSyntax>(ReturnStatement(
                           (ExpressionSyntax) MkCSharpElementAccessExpression("locals", ind)));
                    var setBody = SingletonList<StatementSyntax>((StatementSyntax)MkCSharpSimpleAssignmentExpressionStatement(
                            (ExpressionSyntax)MkCSharpElementAccessExpression("locals", ind),
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
            //TODO(fix): replace this code with general case: Execute method for any function
            public SyntaxNode MkExecuteMethod()
            {
                //Line below is a template:
                List<StatementSyntax> funStmts = new List<StatementSyntax>();
                //PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
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

                //TODO(expand): generate a "case" stmt (using EmitLabelPrelude) 
                //for possibly multiple labels "Loc_XX"
                //stored in "labels" of the former ZingFoldContext
                //if (currFun.returnTolocation == 0) goto Loc_0; else goto Ret;
                funStmts.Add(
                    IfStatement(
                        BinaryExpression(
                            SyntaxKind.EqualsExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("currFun"),
                                IdentifierName("returnTolocation")),
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(0))),
                        GotoStatement(
                            SyntaxKind.GotoStatement,
                            IdentifierName("Loc_0")))
                    .WithElse(
                        ElseClause(
                            GotoStatement(
                                SyntaxKind.GotoStatement,
                                IdentifierName("Ret"))))
                    .NormalizeWhitespace());

                //Loc_0:
                funStmts.Add(
                    LabeledStatement(
                        MkCSharpIdentifier("Loc_0"),
                        ExpressionStatement(
                            IdentifierName(
                                MissingToken(SyntaxKind.IdentifierToken)))
                        .WithSemicolonToken(
                            MissingToken(SyntaxKind.SemicolonToken)))
                    .NormalizeWhitespace());

                //TODO(expand): insert compilation result for the general case of function body
                funStmts.AddRange(MkFunctionBody());

                //Ret: parent.PrtFunContReturn(null);
                funStmts.Add(
                    LabeledStatement(
                        MkCSharpIdentifier("Ret"),
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
                                                SyntaxKind.NullLiteralExpression)))))))
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
                                        MkCSharpIdentifier("parent"))
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
                                new [] { ThisExpression(), MkCSharpIdentifierName("locals"), MkCSharpIdentifierName("retLoc") })));
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
                               (ExpressionSyntax)MkCSharpElementAccessExpression("fields", ind)));
                    var setBody = SingletonList<StatementSyntax>((StatementSyntax)MkCSharpSimpleAssignmentExpressionStatement(
                            (ExpressionSyntax)MkCSharpElementAccessExpression("fields", ind),
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

                //TODO(expand): getters and setters
                
                //#region Functions

                //TODO(expand): generate functions declared in the machine (not state-specific)

                //classes for functions for each state of the machine
                //and variable declarations for those functions:
                foreach (var pair in machInfo.stateNameToStateInfo)
                {
                    //entry function of the state:
                    var funName = pair.Value.entryActionName;
                    FunInfo funInfo = GetFunInfo(funName);
                    //TODO(remove)
                    Console.WriteLine("Entry func name for state {0}: {1}", pair.Key, funName);
                    MkFunctionDecl funDecl = new MkFunctionDecl(funName, funInfo, false, this, translator);
                    funDecl.AddFunClass();

                    //exit function of the state: 
                    funName = pair.Value.exitFunName;
                    //TODO(remove)
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
                            //TODO(remove)
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
                        //TODO(remove)
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
                    //TODO(remove):
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
