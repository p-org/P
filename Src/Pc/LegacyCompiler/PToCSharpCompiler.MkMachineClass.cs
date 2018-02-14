using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.Pc
{
    partial class PToCSharpCompiler
    {
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
                        return SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName(stateName),
                                    SyntaxFactory.ObjectCreationExpression(
                                            SyntaxFactory.IdentifierName(stateType))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                    new SyntaxNodeOrToken[]{
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName("\"" + stateName + "\"")),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(entryFun)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(exitFun)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.LiteralExpression(
                                                                SyntaxKind.TrueLiteralExpression)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                SyntaxFactory.IdentifierName("StateTemperature"),
                                                                SyntaxFactory.IdentifierName("Warm")))})))))
                            .NormalizeWhitespace();
                    }
                    else if (temperature == StateTemperature.HOT)
                    {
                        return SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName(stateName),
                                    SyntaxFactory.ObjectCreationExpression(
                                            SyntaxFactory.IdentifierName(stateType))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                    new SyntaxNodeOrToken[]{
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName("\"" + stateName + "\"")),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(entryFun)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(exitFun)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.LiteralExpression(
                                                                SyntaxKind.TrueLiteralExpression)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                SyntaxFactory.IdentifierName("StateTemperature"),
                                                                SyntaxFactory.IdentifierName("Hot")))})))))
                            .NormalizeWhitespace();
                    }
                    else
                    {
                        return SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName(stateName),
                                    SyntaxFactory.ObjectCreationExpression(
                                            SyntaxFactory.IdentifierName(stateType))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                    new SyntaxNodeOrToken[]{
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName("\"" + stateName + "\"")),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(entryFun)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(exitFun)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.LiteralExpression(
                                                                SyntaxKind.TrueLiteralExpression)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                SyntaxFactory.IdentifierName("StateTemperature"),
                                                                SyntaxFactory.IdentifierName("Cold")))})))))
                            .NormalizeWhitespace();
                    }
                }
                else
                {
                    if (temperature == StateTemperature.WARM)
                    {
                        return SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName(stateName),
                                    SyntaxFactory.ObjectCreationExpression(
                                            SyntaxFactory.IdentifierName(stateType))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                    new SyntaxNodeOrToken[]{
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName("\"" + stateName + "\"")),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(entryFun)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(exitFun)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.LiteralExpression(
                                                                SyntaxKind.FalseLiteralExpression)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                SyntaxFactory.IdentifierName("StateTemperature"),
                                                                SyntaxFactory.IdentifierName("Warm")))})))))
                            .NormalizeWhitespace();
                    }
                    else if (temperature == StateTemperature.HOT)
                    {
                        return SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName(stateName),
                                    SyntaxFactory.ObjectCreationExpression(
                                            SyntaxFactory.IdentifierName(stateType))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                    new SyntaxNodeOrToken[]{
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName("\"" + stateName + "\"")),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(entryFun)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(exitFun)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.LiteralExpression(
                                                                SyntaxKind.FalseLiteralExpression)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                SyntaxFactory.IdentifierName("StateTemperature"),
                                                                SyntaxFactory.IdentifierName("Hot")))})))))
                            .NormalizeWhitespace();
                    }
                    else
                    {
                        return SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName(stateName),
                                    SyntaxFactory.ObjectCreationExpression(
                                            SyntaxFactory.IdentifierName(stateType))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                    new SyntaxNodeOrToken[]{
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName("\"" + stateName + "\"")),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(entryFun)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(exitFun)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.LiteralExpression(
                                                                SyntaxKind.FalseLiteralExpression)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                SyntaxFactory.IdentifierName("StateTemperature"),
                                                                SyntaxFactory.IdentifierName("Cold")))})))))
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
                    SyntaxFactory.PropertyDeclaration(
                            SyntaxFactory.IdentifierName("PrtState"),
                            SyntaxFactory.Identifier("StartState"))
                        .WithModifiers(
                            SyntaxFactory.TokenList(
                                new[]{
                                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                    SyntaxFactory.Token(SyntaxKind.OverrideKeyword)}))
                        .WithAccessorList(
                            SyntaxFactory.AccessorList(
                                SyntaxFactory.SingletonList<AccessorDeclarationSyntax>(
                                    SyntaxFactory.AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration,
                                        SyntaxFactory.Block(
                                            SyntaxFactory.SingletonList<StatementSyntax>(
                                                SyntaxFactory.ReturnStatement(
                                                    SyntaxFactory.IdentifierName(startState))))))))
                        .NormalizeWhitespace();
                machineMembers.Add(startStateProperty);

                //Add getters/setters for machine variables:
                int ind = 0;
                foreach (var pair in machineInfo.localVariableToVarInfo)
                {
                    SyntaxTokenList modifiers = new SyntaxTokenList();
                    modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
                    var getBody = SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ReturnStatement(
                                                                     (ExpressionSyntax)CSharpHelper.MkCSharpElementAccessExpression(
                                                                         SyntaxFactory.IdentifierName("fields"), ind)));
                    var setBody = SyntaxFactory.SingletonList<StatementSyntax>((StatementSyntax)CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                                                                     (ExpressionSyntax)CSharpHelper.MkCSharpElementAccessExpression(
                                                                         SyntaxFactory.IdentifierName("fields"), ind),
                                                                     SyntaxFactory.IdentifierName("value")));
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
                    SyntaxFactory.PropertyDeclaration(
                            SyntaxFactory.PredefinedType(
                                SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                            SyntaxFactory.Identifier("Name"))
                        .WithModifiers(
                            SyntaxFactory.TokenList(
                                new[]{
                                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                    SyntaxFactory.Token(SyntaxKind.OverrideKeyword)}))
                        .WithAccessorList(
                            SyntaxFactory.AccessorList(
                                SyntaxFactory.SingletonList<AccessorDeclarationSyntax>(
                                    SyntaxFactory.AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration,
                                        SyntaxFactory.Block(
                                            SyntaxFactory.SingletonList<StatementSyntax>(
                                                SyntaxFactory.ReturnStatement(
                                                    SyntaxFactory.IdentifierName("\"" + machineName + "\""))))))))
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
                    var defaultValue = CSharpHelper.MkCSharpInvocationExpression(SyntaxFactory.MemberAccessExpression(
                                                                                     SyntaxKind.SimpleMemberAccessExpression,
                                                                                     SyntaxFactory.IdentifierName("PrtValue"),
                                                                                     SyntaxFactory.IdentifierName("PrtMkDefaultValue")), translator.typeContext.PTypeToCSharpExpr(varInfo.type));
                    constructorStmtList.Add(SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("fields", "Add"), defaultValue)));
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
                        SyntaxFactory.ClassDeclaration(stateType)
                            .WithModifiers(
                                SyntaxFactory.TokenList(
                                    SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                            .WithBaseList(
                                SyntaxFactory.BaseList(
                                    SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                                        SyntaxFactory.SimpleBaseType(
                                            SyntaxFactory.IdentifierName("PrtState")))))
                            .WithMembers(
                                SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                    SyntaxFactory.ConstructorDeclaration(
                                            SyntaxFactory.Identifier(stateType))
                                        .WithModifiers(
                                            SyntaxFactory.TokenList(
                                                SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                        .WithParameterList(
                                            SyntaxFactory.ParameterList(
                                                SyntaxFactory.SeparatedList<ParameterSyntax>(
                                                    new SyntaxNodeOrToken[]{
                                                        SyntaxFactory.Parameter(
                                                                SyntaxFactory.Identifier("name"))
                                                            .WithType(
                                                                SyntaxFactory.PredefinedType(
                                                                    SyntaxFactory.Token(SyntaxKind.StringKeyword))),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Parameter(
                                                                SyntaxFactory.Identifier("entryFun"))
                                                            .WithType(
                                                                SyntaxFactory.IdentifierName("PrtFun")),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Parameter(
                                                                SyntaxFactory.Identifier("exitFun"))
                                                            .WithType(
                                                                SyntaxFactory.IdentifierName("PrtFun")),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Parameter(
                                                                SyntaxFactory.Identifier("hasNullTransition"))
                                                            .WithType(
                                                                SyntaxFactory.PredefinedType(
                                                                    SyntaxFactory.Token(SyntaxKind.BoolKeyword))),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Parameter(
                                                                SyntaxFactory.Identifier("temperature"))
                                                            .WithType(
                                                                SyntaxFactory.IdentifierName("StateTemperature"))})))
                                        .WithInitializer(
                                            SyntaxFactory.ConstructorInitializer(
                                                SyntaxKind.BaseConstructorInitializer,
                                                SyntaxFactory.ArgumentList(
                                                    SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                        new SyntaxNodeOrToken[]{
                                                            SyntaxFactory.Argument(
                                                                SyntaxFactory.IdentifierName("name")),
                                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                            SyntaxFactory.Argument(
                                                                SyntaxFactory.IdentifierName("entryFun")),
                                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                            SyntaxFactory.Argument(
                                                                SyntaxFactory.IdentifierName("exitFun")),
                                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                            SyntaxFactory.Argument(
                                                                SyntaxFactory.IdentifierName("hasNullTransition")),
                                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                            SyntaxFactory.Argument(
                                                                SyntaxFactory.IdentifierName("temperature"))}))))
                                        .WithBody(
                                            SyntaxFactory.Block())))
                            .NormalizeWhitespace();
                    machineMembers.Add(stateClassDecl);

                    //state declaration: 
                    //Example: public static Init Init_State;
                    var stateDeclaration =
                        SyntaxFactory.FieldDeclaration(
                                SyntaxFactory.VariableDeclaration(
                                        SyntaxFactory.IdentifierName(stateType))
                                    .WithVariables(
                                        SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                                            SyntaxFactory.VariableDeclarator(
                                                SyntaxFactory.Identifier(stateName)))))
                            .WithModifiers(
                                SyntaxFactory.TokenList(
                                    new[]{
                                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                        SyntaxFactory.Token(SyntaxKind.StaticKeyword)}))
                            .NormalizeWhitespace();
                    machineMembers.Add(stateDeclaration);

                    //Add DoDecls to the StateInfo:
                    foreach (var doFun in pair.Value.dos)
                    {
                        ExpressionSyntax doFunExpr = SyntaxFactory.IdentifierName(doFun.Value);
                        if (doFun.Value == "ignore")
                        {
                            doFunExpr = SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("PrtFun"),
                                SyntaxFactory.IdentifierName("IgnoreFun"));
                        }
                        mainConstructorFields.Add(
                            SyntaxFactory.ExpressionStatement(
                                    SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName(stateName),
                                                    SyntaxFactory.IdentifierName("dos")),
                                                SyntaxFactory.IdentifierName("Add")))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                    new SyntaxNodeOrToken[]{
                                                        SyntaxFactory.Argument(
                                                            translator.GetEventVar(doFun.Key)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(doFunExpr)}))))
                                .NormalizeWhitespace()
                        );
                    }
                    
                    //Add deferred events (if any) to the state:
                    foreach (var deferredEvent in pair.Value.deferredEvents)
                    {
                        mainConstructorFields.Add(
                            SyntaxFactory.ExpressionStatement(
                                    SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName(stateName),
                                                    SyntaxFactory.IdentifierName("deferredSet")),
                                                SyntaxFactory.IdentifierName("Add")))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                                    SyntaxFactory.Argument(
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
                                SyntaxFactory.LocalDeclarationStatement(
                                        SyntaxFactory.VariableDeclaration(
                                                SyntaxFactory.IdentifierName("PrtTransition"))
                                            .WithVariables(
                                                SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                                                    SyntaxFactory.VariableDeclarator(
                                                            SyntaxFactory.Identifier(transition_name))
                                                        .WithInitializer(
                                                            SyntaxFactory.EqualsValueClause(
                                                                SyntaxFactory.ObjectCreationExpression(
                                                                        SyntaxFactory.IdentifierName("PrtTransition"))
                                                                    .WithArgumentList(
                                                                        SyntaxFactory.ArgumentList(
                                                                            SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                                                new SyntaxNodeOrToken[]{
                                                                                    SyntaxFactory.Argument(
                                                                                        SyntaxFactory.MemberAccessExpression(
                                                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                                                            SyntaxFactory.IdentifierName("PrtFun"),
                                                                                            SyntaxFactory.IdentifierName("IgnoreFun"))),
                                                                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                                                    SyntaxFactory.Argument(
                                                                                        SyntaxFactory.IdentifierName(transition.Value.target)),
                                                                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                                                    SyntaxFactory.Argument(
                                                                                        SyntaxFactory.LiteralExpression(
                                                                                            SyntaxKind.TrueLiteralExpression))}))))))))
                                    .NormalizeWhitespace());
                        }
                        else
                        {
                            //goto transition:
                            mainConstructorFields.Add(
                                SyntaxFactory.LocalDeclarationStatement(
                                        SyntaxFactory.VariableDeclaration(
                                                SyntaxFactory.IdentifierName("PrtTransition"))
                                            .WithVariables(
                                                SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                                                    SyntaxFactory.VariableDeclarator(
                                                            SyntaxFactory.Identifier("transition" + "_" + transition_count))
                                                        .WithInitializer(
                                                            SyntaxFactory.EqualsValueClause(
                                                                SyntaxFactory.ObjectCreationExpression(
                                                                        SyntaxFactory.IdentifierName("PrtTransition"))
                                                                    .WithArgumentList(
                                                                        SyntaxFactory.ArgumentList(
                                                                            SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                                                new SyntaxNodeOrToken[]{
                                                                                    SyntaxFactory.Argument(
                                                                                        SyntaxFactory.IdentifierName(transition.Value.transFunName)),
                                                                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                                                    SyntaxFactory.Argument(
                                                                                        SyntaxFactory.IdentifierName(transition.Value.target)),
                                                                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                                                    SyntaxFactory.Argument(
                                                                                        SyntaxFactory.LiteralExpression(
                                                                                            SyntaxKind.FalseLiteralExpression))}))))))))
                                    .NormalizeWhitespace());
                        }
                        //Add transition to the state:
                        mainConstructorFields.Add(
                            SyntaxFactory.ExpressionStatement(
                                    SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName(stateName),
                                                    SyntaxFactory.IdentifierName("transitions")),
                                                SyntaxFactory.IdentifierName("Add")))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                    new SyntaxNodeOrToken[]{
                                                        SyntaxFactory.Argument(
                                                            translator.GetEventVar(trigger)),
                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(transition_name))}))))
                                .NormalizeWhitespace()
                        );
                        transition_count += 1;
                    }
                }

                //Constructor for the machine class:
                //static Machine()
                var mainConstructor =
                    SyntaxFactory.ConstructorDeclaration(
                            SyntaxFactory.Identifier(machineName))
                        .WithModifiers(
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                        .WithBody(
                            SyntaxFactory.Block(mainConstructorFields
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
    }
}