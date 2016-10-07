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
    internal class CSharpTranslationInfo
    {
        public AST<Node> node = null;

        public CSharpTranslationInfo(AST<Node> n)
        {
            this.node = n;
        }
    }

    class PToCSharp : PTranslation
    {
        public PToCSharp(Compiler compiler, AST<Model> model, AST<Model> modelWithTypes)
            : base(compiler, model)
        {
            //this.typeContext = new TypeTranslationContext(this);
            //GenerateTypeInfo(modelWithTypes);
        }

        #region CSharpCompiler

        //for storing members of the Application class:
        static List <SyntaxNode> members = new List<SyntaxNode>();
        //final C# program:
        SyntaxNode result = null;
        static SyntaxGenerator generator;

        //utility methods:
        //This is based on PToZing.TypeTranslationContext.ConstructType
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
        public bool GenerateCSharp(string csharpFileName)
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
            //TODO(expand): create static class and static field for each static function in P program
            //TODO(question): where to get information about static functions defined outside of any machine?
            //in PTranslation class: 
            //public Dictionary<AST<Node>, string> anonFunToName;
            //generated static anon function name is: 
            //var funName = "AnonFunStatic" + anonFunCounterStatic;
            //In PToZing.cs:
            //string funName = anonFunToName[Factory.Instance.ToAST(fun)];
            //, where fun is a Node.
            //MkStaticFunctions(elements, workspace, generator);
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
                SyntaxNode payloadType = generator.IdentifierName(GetTypeCreationExpr(ConstructType((pair.Value).payloadType)));
                SyntaxNode maxInstances;
                if ((pair.Value).maxInstances == -1)
                {
                    String s = "PrtEvent.DefaultMaxInstances";
                    maxInstances = generator.IdentifierName(s);
                }
                else
                {
                    maxInstances = generator.LiteralExpression((pair.Value).maxInstances);
                }
               
                SyntaxNode doAssume;
                if ((pair.Value).maxInstancesAssumed)
                {
                    doAssume = generator.LiteralExpression(true);
                }
                else
                {
                    doAssume = generator.LiteralExpression(false);
                }
                string quotedEventName = "\"" + pair.Key + "\"";
                var eventCreationPars = new SyntaxNode[] { generator.IdentifierName(quotedEventName), payloadType, maxInstances, doAssume };
                var eventCreationExpr = generator.ObjectCreationExpression(generator.IdentifierName("PrtEvent"), eventCreationPars);
                var initExpr_1 = generator.ObjectCreationExpression(generator.IdentifierName("PrtEventValue"), eventCreationExpr);

                //"@null" is written to he output as pair.Key for the null event; possibly, this is done to disambiguate null as C# keyword
                //output with "@null" compiles
                SyntaxNode eventField_1 = null;
                if (pair.Key == NullEvent)
                {
                    eventField_1 = generator.FieldDeclaration(@"null",
                        generator.IdentifierName("PrtEventValue"),
                        Accessibility.Public,
                         DeclarationModifiers.Static,
                        initExpr_1);
                }
                else
                {
                    eventField_1 = generator.FieldDeclaration(pair.Key,
                        generator.IdentifierName("PrtEventValue"),
                        Accessibility.Public,
                         DeclarationModifiers.Static,
                        initExpr_1);
                }
                

                members.Add(eventField_1);
                
            }
        }
        private void MkOtherAppFields()
        {
            //CreateMainMachine method declaration:
            List<SyntaxNode> fields = new List<SyntaxNode>();
            //stmt1: var mainMachine = new Main(this, 10);
            fields.Add(generator.LocalDeclarationStatement(generator.IdentifierName("var"), "mainMachine", 
                                   generator.ObjectCreationExpression(generator.IdentifierName("Main"),
                                   new List<SyntaxNode>() { generator.ThisExpression(), generator.LiteralExpression(10) })));
                                   //new List<SyntaxNode>() { generator.IdentifierName("this"), generator.LiteralExpression(10) })));
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
            foreach (var pair in allMachines)
            {
                var mkMachine = new MkMachineClass(pair.Key, pair.Value);
                if (pair.Key == "Main")
                {
                    SyntaxNode node = mkMachine.MkMainMachineClass();
                    members.Add(node);
                }
                else if ((pair.Value).IsReal)
                {
                    SyntaxNode node = mkMachine.MkRealMachineClass();
                    members.Add(node);
                }
                else
                {
                    //monitor machine
                    SyntaxNode node = mkMachine.MkSpecMachineClass();
                    members.Add(node);
                }
            }
        }
        internal class MkMachineClass
        {
            public string machName;
            public MachineInfo machInfo;
            static List<SyntaxNode> machineMembers = new List<SyntaxNode>();
            List<StatementSyntax> mainConstructorFields = new List<StatementSyntax>();
            //keeps track of already encountered function names:
            private HashSet<string> processedFuns = new HashSet<string>();
            private static int transition_count = 1;
            public MkMachineClass(string name, MachineInfo info) {
                machName = name;
                machInfo = info;
            }
            //TODO(fix): replace this code with general case: Execute method for any function
            public SyntaxNode MkExecuteMethod()
            {
                //PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                var stmt1 =
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
                    .NormalizeWhitespace();

                //if (currFun.returnTolocation == 0) goto Loc_0; else goto Ret;
                var stmt2 =
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
                    .NormalizeWhitespace();

                //Loc_0:
                var stmt3 =
                    LabeledStatement(
                        Identifier("Loc_0"),
                        ExpressionStatement(
                            IdentifierName(
                                MissingToken(SyntaxKind.IdentifierToken)))
                        .WithSemicolonToken(
                            MissingToken(SyntaxKind.SemicolonToken)))
                    .NormalizeWhitespace();

                //Ret: parent.PrtFunContReturn(null);
                var stmt4 =
                    LabeledStatement(
                        Identifier("Ret"),
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
                    .NormalizeWhitespace();

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
                                        Identifier("parent"))
                                    .WithType(
                                        IdentifierName("PrtMachine"))})))
                    .WithBody(
                        Block(stmt1, stmt2, stmt3, stmt4))
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
            public SyntaxNode MkFuncClassForState(string funName, string funType)
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

                funMembers = funMembers.Add((MemberDeclarationSyntax) MkExecuteMethod());
                funMembers = funMembers.Add((MemberDeclarationSyntax) MkCreateLocalsMethod());

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
            
            public void AddFunClass(string funName, string funType)
            {
                //If this function name was already encountered, class declaration
                //has been generated earlier
                if (!processedFuns.Contains(funName))
                {
                    //Class declaration:
                    machineMembers.Add(MkFuncClassForState(funName, funType));
                    //Variable declaration:
                    machineMembers.Add(
                        FieldDeclaration(
                            VariableDeclaration(
                                IdentifierName(funType))
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
                    //Add function variable instantiation to Main constructor:
                    mainConstructorFields.Add(
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(funName),
                                ObjectCreationExpression(
                                    IdentifierName(funType))
                                .WithArgumentList(
                                    ArgumentList())))
                        .NormalizeWhitespace());

                    processedFuns.Add(funName);
                }
            }
            public SyntaxNode MkMainMachineClass()
            {
                //function instantiations for the constructor of Main:
                //List<ExpressionStatementSyntax> mainConstructorFields = new List<ExpressionStatementSyntax>();
                

                //StartState property (getter only, since there's no setter in the base class):
                string startState = machInfo.initStateName;
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

                
                var skeletonMethodBody = generator.ReturnStatement(generator.ObjectCreationExpression(generator.IdentifierName("Main")));
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
                                                LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    Literal("Main")))))))))
                    .NormalizeWhitespace();
                machineMembers.Add(nameProperty);

                //constructor for cloning
                var constructor_1 = generator.ConstructorDeclaration("Main", null, Accessibility.Public, baseConstructorArguments: new SyntaxNode[0]);
                machineMembers.Add(constructor_1);

                //Main constructor
                //TODO(expand): add inits for all fields
                var constructorPars = new SyntaxNode[]
                {
                generator.ParameterDeclaration("app", generator.IdentifierName("StateImpl")),
                generator.ParameterDeclaration("maxB", generator.TypeExpression(SpecialType.System_Int32))
                };
                var baseConstructorPars = new SyntaxNode[] { generator.IdentifierName("app"), generator.IdentifierName("maxB") };
                var constructor_2 = generator.ConstructorDeclaration("Main", constructorPars, Accessibility.Public, baseConstructorArguments: baseConstructorPars);
                machineMembers.Add(constructor_2);

                //TODO(expand): getters and setters
                
                //#region Functions

                //TODO(expand): generate functions declared in the Main machine (not state-specific)

                //classes for functions for each state of Main
                //and variable declarations for those functions:
                foreach (var pair in machInfo.stateNameToStateInfo)
                {
                    //entry function of the state:
                    var funName = pair.Value.entryActionName;
                    //TODO(remove)
                    Console.WriteLine("Entry func name for state {0}: {1}", pair.Key, funName);
                    var funType = funName + "_Class";
                    AddFunClass(funName, funType);

                    //exit function of the state: 
                    funName = pair.Value.exitFunName;
                    //TODO(remove)
                    Console.WriteLine("Exit func name for state {0}: {1}", pair.Key, funName);
                    funType = funName + "_Class";
                    AddFunClass(funName, funType);

                    //Functions in transitions:
                    foreach (var transition in pair.Value.transitions)
                    {
                        funName = transition.Value.transFunName;
                        funType = funName + "_Class";
                        if (!transition.Value.IsPush)
                        {
                            AddFunClass(funName, funType);
                        }
                    }

                    //(TODO: how to loop over functions in dos?) Functions in dos:
 
                    //TODO(question, expand): any other functions for the state? What about functions in transitions?

                }

                //State classes for all states of Main:
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

                    //state instantiation for Main constructor:
                    mainConstructorFields.Add(
                        ExpressionStatement(
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
                                                    IdentifierName(pair.Value.entryActionName)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(pair.Value.exitFunName)),
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
                        .NormalizeWhitespace());

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
                                                        IdentifierName(trigger)),
                                                    Token(SyntaxKind.CommaToken),
                                                    Argument(
                                                        IdentifierName(transition_name))}))))
                            .NormalizeWhitespace()
                            );
                        transition_count += 1;
                    }
                }

                //Constructor for Main:
                //static Main()
                var mainConstructor =
                    ConstructorDeclaration(
                        Identifier("Main"))
                            .WithModifiers(
                                TokenList(
                                    Token(SyntaxKind.StaticKeyword)))
                            .WithBody(
                                Block(mainConstructorFields
                                    ))
                    .NormalizeWhitespace();

                //SyntaxNode mainConstructor = generator.ConstructorDeclaration("Main", modifiers: DeclarationModifiers.Static,
                //    statements: mainConstructorFields);

                machineMembers.Add(mainConstructor);

                var mainMachineClassDecl = generator.ClassDeclaration(
                  "Main", typeParameters: null,
                  accessibility: Accessibility.Public,
                  baseType: generator.IdentifierName("PrtImplMachine"),
                  members: machineMembers);

                return mainMachineClassDecl;
            }
            public SyntaxNode MkRealMachineClass()
            {
                //TODO(expand)
                return null;
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
            //TODO(question): generate namespace name: <source name> + "_CSharp" or something
            var programNameSpaceDeclaration = generator.NamespaceDeclaration("MyPProgramGenerated", applicationcClassDeclaration);

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
