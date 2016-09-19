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
        //public const string SM_EVENT = "SM_EVENT";
        //public static AST<Node> SmEvent = Factory.Instance.MkCnst("SM_EVENT");
        //public const string SM_TRANSITION = "SM_TRANSITION";
        //public static AST<Node> SmTransition = Factory.Instance.MkCnst("SM_TRANSITION");
        //public const string SM_STATE = "SM_STATE";
        //public static AST<Node> SmState = Factory.Instance.MkCnst("SM_STATE");
        //public const string SM_HANDLE = "SM_HANDLE";
        //public static AST<Node> SmHandle = Factory.Instance.MkCnst("SM_HANDLE");
        //public static string SM_EVENT_SET = "SM_EVENT_SET";
        //public static AST<Node> SmEventSet = Factory.Instance.MkCnst("SM_EVENT_SET");
        //public const string PRT_VALUE = "PRT_VALUE";
        //public static AST<Node> PrtValue = Factory.Instance.MkCnst("PRT_VALUE");
        //public static AST<Node> PrtCastValue = MkZingDot("PRT_VALUE", "PrtCastValue");
        //public static AST<Node> PrtMkDefaultValue = MkZingDot("PRT_VALUE", "PrtMkDefaultValue");
        //public static AST<Node> PrtCloneValue = MkZingDot("PRT_VALUE", "PrtCloneValue");
        //public static AST<Node> PrtIsEqualValue = MkZingDot("PRT_VALUE", "PrtIsEqualValue");

        //private static HashSet<string> ReservedNames = new HashSet<string>(
        //    new string[] { "Main", "SM_NULL", "MachineId", "SM_EVENT", "SM_TRANSITION", "SM_STATE", "SM_HANDLE", "EVENT_NODE", "EVENT_BUFFER", "StateStack", "StackFrame", "Continuation", 
        //                   "FairScheduler", "FairChoice", "FairCycle", "PRT_FIELD_NAME", "PRT_TYPE", "PRT_VALUE", "PRT_SEQ", "PRT_MAP",
        //                   "Machine", "Event", "State", "ActionOrFun", "ContinuationReason", "GateStatus", "StateTemperature", "PRT_TYPE_KIND", 
        //                    "SM_EVENT_ARRAY", "SM_TRANSITION_ARRAY", "ActionOrFun_ARRAY", "PRT_TYPE_ARRAY", "PRT_FIELD_NAME_ARRAY", "PRT_VALUE_ARRAY" });

        //private static string ZingMachineClassName(string machineName)
        //{
        //    return string.Format("MACHINE_{0}", machineName);
        //}

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
                //SyntaxNode payloadType_1 = generator.TypeExpression(SpecialType.System_String);
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
                //Tried: 
                SyntaxNode eventField_1 = null;
                if (pair.Key == "null")
                {
                    //Still generates @null for the event name
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
                var mkMachineClass = new MkMachineClass(pair.Key, pair.Value);
                if (pair.Key == "Main")
                {
                    SyntaxNode node = mkMachineClass.MkMainMachineClass();
                    members.Add(node);
                }
                else if ((pair.Value).IsReal)
                {
                    SyntaxNode node = mkMachineClass.MkRealMachineClass();
                    members.Add(node);
                }
                else
                {
                    //monitor machine
                    SyntaxNode node = mkMachineClass.MkSpecMachineClass();
                    members.Add(node);
                }
            }
        }
        internal class MkMachineClass
        {
            static int anonFunCount = 0;
            public string machName;
            public MachineInfo machInfo;
            static List<SyntaxNode> machineMembers = new List<SyntaxNode>();
            public MkMachineClass(string name, MachineInfo info) {
                machName = name;
                machInfo = info;
            }
            public SyntaxNode MkAnonExecuteMethod()
            {
                List<SyntaxNode> executeStmts = new List<SyntaxNode>(); 
                //TODO(expand): trueStmts will be non-empty in the general case (example: PingPong.cs)
                List<SyntaxNode> trueStmts = new List<SyntaxNode>();
                //TODO(question): can falseStmts be different?
                List<SyntaxNode> falseStmts = new List<SyntaxNode>();

                //parent.PrtFunContReturn(null);
                List<SyntaxNode> pars = new List<SyntaxNode>();
                pars.Add(generator.NullLiteralExpression());
                falseStmts.Add(generator.InvocationExpression(
                    generator.MemberAccessExpression(generator.IdentifierName("parent"), generator.IdentifierName("PrtFunContReturn")),
                    pars));

                //PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                executeStmts.Add(generator.LocalDeclarationStatement(generator.IdentifierName("PrtFunStackFrame"), "currFun",
                            generator.InvocationExpression(generator.MemberAccessExpression(generator.IdentifierName("parent"),
                                    generator.IdentifierName("PrtPopFunStackFrame")))));
                //if ((currFun.returnTolocation) == (0))
                executeStmts.Add(generator.IfStatement(
                    generator.ValueEqualsExpression(generator.MemberAccessExpression(
                            generator.IdentifierName("currFun"), generator.IdentifierName("returnTolocation")),
                                generator.LiteralExpression(0)),
                    trueStatements: trueStmts,
                    falseStatements: falseStmts));
                
                var methodPars = new SyntaxNode[] {
                    generator.ParameterDeclaration("application", generator.IdentifierName("StateImpl")),
                    generator.ParameterDeclaration("parent", generator.IdentifierName("PrtMachine"))};
                var mkExecuteMethodDeclDecl = generator.MethodDeclaration("Execute", methodPars,
                   null, SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                   Accessibility.Public,
                   DeclarationModifiers.Override,
                   statements: executeStmts);
                return mkExecuteMethodDeclDecl;
            }
            public SyntaxNode MkAnonCreateLocalsMethod()
            {
                return null;
            }
            public SyntaxNode MkAnonEntryFuncClassForState(string stateName, StateInfo stateInfo)
            {
                List<SyntaxNode> anonMembers = new List<SyntaxNode>();
                string funName = stateInfo.entryActionName;
                if (!funName.StartsWith("AnonFun"))
                {
                    Debug.Assert(false);
                }

                List<SyntaxNode> anonClassMembers = new List<SyntaxNode>();
                //Name property:
                anonClassMembers.Add(
                  generator.PropertyDeclaration("Name",
                  generator.TypeExpression(SpecialType.System_String), Accessibility.Public, DeclarationModifiers.Override,
                  getAccessorStatements: new SyntaxNode[]
                     { generator.ReturnStatement(generator.IdentifierName("Main_Init_Entry")) }));

                //IsAnonFun property:
                anonClassMembers.Add(
                  generator.PropertyDeclaration("IsAnonFun",
                  generator.TypeExpression(SpecialType.System_Boolean), Accessibility.Public, DeclarationModifiers.Override,
                  getAccessorStatements: new SyntaxNode[]
                     { generator.ReturnStatement(generator.LiteralExpression(true)) }));

                //Execute method:
                anonClassMembers.Add(MkAnonExecuteMethod());

                //CreateLocals method:
                //anonClassMembers.Add(MkAnonCreateLocalsMethod());

                var AnonClassDecl = generator.ClassDeclaration(
                  funName, typeParameters: null,
                  accessibility: Accessibility.Public,
                  baseType: generator.IdentifierName("PrtFun"),
                  members: anonClassMembers);

                return AnonClassDecl;
            }
            public SyntaxNode MkAnonExitFuncClassForState(string stateName, StateInfo stateInfo)
            {
                return null;
            }
            public SyntaxNode MkMainMachineClass()
            {           
                var startStateProperty = generator.PropertyDeclaration("StartState",
                  generator.IdentifierName("PrtState"), Accessibility.Public, DeclarationModifiers.Override,
                  getAccessorStatements: new SyntaxNode[]
                  { generator.ReturnStatement(generator.IdentifierName(machInfo.initStateName)) });
                machineMembers.Add(startStateProperty);

                var makeSkeletonMethodBody = generator.ReturnStatement(generator.ObjectCreationExpression(generator.IdentifierName("Main")));
                var makeSkeletonMethodDecl = generator.MethodDeclaration("MakeSkeleton", null,
                  null, generator.IdentifierName("PrtImplMachine"),
                  Accessibility.Public,
                  DeclarationModifiers.Override,
                  new SyntaxNode[] { makeSkeletonMethodBody });
                machineMembers.Add(makeSkeletonMethodDecl);

                //(this.GetType())
                var returnMethodPars = new SyntaxNode[]
                {
                generator.InvocationExpression(generator.MemberAccessExpression(generator.ThisExpression(), generator.IdentifierName("GetType")))
                };
                //return app.NextMachineInstanceNumber(this.GetType());
                var makeNextInstanceNumberMethodBody = generator.ReturnStatement(generator.InvocationExpression(generator.MemberAccessExpression(
                     generator.IdentifierName("app"), generator.IdentifierName("NextMachineInstanceNumber")), returnMethodPars));
                //ReturnStatement(generator.InvocationExpression(generator.QualifiedName(generator.IdentifierName("app"), 
                //generator.IdentifierName("NextMachineInstanceNumber")), returnMethodPars));
                //(StateImpl app)
                var methodPars = new SyntaxNode[] { generator.ParameterDeclaration("app", generator.IdentifierName("StateImpl")) };
                var makeNextInstanceNumberMethodDecl = generator.MethodDeclaration("NextInstanceNumber", methodPars,
                  //null, generator.IdentifierName("int"),
                  null, generator.TypeExpression(SpecialType.System_Int32),
                  Accessibility.Public,
                  DeclarationModifiers.Override,
                  new SyntaxNode[] { makeNextInstanceNumberMethodBody });
                machineMembers.Add(makeNextInstanceNumberMethodDecl);

                var nameProperty = generator.PropertyDeclaration("Name",
                  generator.TypeExpression(SpecialType.System_String), Accessibility.Public, DeclarationModifiers.Override,
                  getAccessorStatements: new SyntaxNode[]
                  { generator.ReturnStatement(generator.IdentifierName("\"Main\"")) });
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

                //classes for anon functions for each state of Main
                foreach (var pair in machInfo.stateNameToStateInfo)
                {
                    machineMembers.Add(MkAnonEntryFuncClassForState(pair.Key, pair.Value));
                    //TODO(expand):
                    //machineMembers.Add(MkAnonExitFuncClassForState(pair.Key, pair.Value));
                }
                //TODO: continue here with the Main machine class

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
            var programNameSpaceDeclaration = generator.NamespaceDeclaration("MyPProgram", applicationcClassDeclaration);

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
                Console.WriteLine(writer);
                
                System.IO.StreamWriter file = new System.IO.StreamWriter(fileName);
                file.WriteLine(result);

                file.Close();

            }
        }

        #endregion
    }
    
}
