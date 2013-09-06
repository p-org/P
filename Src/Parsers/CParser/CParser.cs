using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Formula.API;
using Microsoft.Formula.API.Plugins;
using Microsoft.Formula.Compiler;
using Microsoft.Formula.API.Nodes;
using System.IO;


namespace CParser
{

    public class Parser : IQuoteParser
    {
        //Configuration Settings per instance of parser
        #region Configuration Settings
        public class configurationSettings
        {
            public bool isStructDeclaration;
            public bool isGlobalVarDeclaration;
            public bool isGlobalFuncDeclaration;
            public bool isStatementList;
            public bool isTypeDef;
            public bool isEnumDeclaration;
            public bool isUnionDeclaration;
            public bool isInitializer;
            public bool isExpression;

            public configurationSettings()
            {
                isStructDeclaration = false;
                isGlobalVarDeclaration = false;
                isGlobalFuncDeclaration = false;
                isStatementList = false;
                isTypeDef = false;
                isEnumDeclaration = false;
                isUnionDeclaration = false;
                isInitializer = false;
                isExpression = false;
            }
        }

        public configurationSettings configSettings;

        public const string SyntaxKindSetting = "syntaxKind";
        public const string PrefixSetting = "prefix";
        public const string LineDirectivesSetting = "lineDirectives";

        public const string StructDeclValue = "structDecl";
        public const string EnumDeclValue = "enumDecl";
        public const string UnionDeclValue = "unionDecl";
        public const string TypeDefValue = "typeDef";

        public const string GlobalVarDeclValue = "globalVar";
        public const string GlobalFunDeclValue = "globalFun";
        public const string StatementListValue = "statements";

        public const string InitValue = "initializer";
        public const string ExprValue = "expression";

        private Dictionary<string, Action> configSetters =
            new Dictionary<string, Action>();
        #endregion

        public string CollectionName
        {
            get;
            private set;
        }

        public string InstanceName
        {
            get;
            private set;
        }

        public IQuoteParser CreateInstance(
                            AST<Node> module,
                            string collectionName,
                            string instanceName)
        {
            var parser = new Parser();
            parser.CollectionName = collectionName;
            parser.InstanceName = instanceName;
            return parser;
        }

        public bool Render(
                Configuration config,
                TextWriter writer,
                AST<Node> ast,
                out List<Flag> flags)
        {
            Cnst settingValue;
            bool useLineDir = false;
            if (config.TryGetSetting(CollectionName, InstanceName, LineDirectivesSetting, out settingValue))
            {
                if (settingValue.CnstKind == CnstKind.String)
                {
                    if (settingValue.GetStringValue() == "TRUE")
                    {
                        useLineDir = true;
                    }
                    else if (settingValue.GetStringValue() != "FALSE")
                    {
                        flags = new List<Flag>();
                        var flag = new Flag(
                            SeverityKind.Error,
                            settingValue,
                            Constants.BadSetting.ToString(
                                SyntaxKindSetting,
                                settingValue.GetStringValue(),
                                string.Format("Expected setting {0} to be TRUE or FALSE", LineDirectivesSetting)),
                            Constants.BadSetting.Code);
                        flags.Add(flag);
                        return false;
                    }
                }
                else
                {
                    flags = new List<Flag>();
                    var flag = new Flag(
                        SeverityKind.Error,
                        settingValue,
                        Constants.BadSetting.ToString(
                            SyntaxKindSetting, 
                            settingValue.GetNumericValue(), 
                            string.Format("Expected setting {0} to be TRUE or FALSE", LineDirectivesSetting)),
                        Constants.BadSetting.Code);
                    flags.Add(flag);
                    return false;
                }
            }

            var cwriter = new CTextWriter(writer, useLineDir);
            return CRenderer.Render(ast, cwriter, out flags);
        }

        public bool IsValidSetting(AST<Id> setting, AST<Cnst> value, out List<Flag> flags)
        {
            throw new NotImplementedException();
        }

        public string UnquotePrefix
        {
            get;
            private set;
        }

        public AST<Microsoft.Formula.API.Nodes.Domain> SuggestedDataModel
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<Tuple<string, CnstKind>> SuggestedSettings
        {
            get { throw new NotImplementedException(); }
        }

        public bool Parse(
            Configuration config,
            System.IO.Stream quoteStream, 
            SourcePositioner positioner,
            out AST<Microsoft.Formula.API.Nodes.Node> results, 
            out List<Flag> flags)
        {
            flags = new List<Flag>();
            throw new NotImplementedException();
        }

        public string Description
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<Tuple<Microsoft.Formula.API.Nodes.Id, Microsoft.Formula.API.Nodes.Cnst>> Settings
        {
            get { throw new NotImplementedException(); }
        }

        public bool TrySet(Microsoft.Formula.API.Nodes.Id settingName, Microsoft.Formula.API.Nodes.Cnst cnst, out List<Flag> flags)
        {
            throw new NotImplementedException();
        }

        public bool TryGet(Microsoft.Formula.API.Nodes.Id settingName, out Microsoft.Formula.API.Nodes.Cnst result, out List<Flag> flags)
        {
            throw new NotImplementedException();
        }

        public ProgramName Base
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public Parser()
        {
            configSettings = new configurationSettings();
            UnquotePrefix = string.Format("PREFIX_{0}_", System.Guid.NewGuid().ToString("N"));
            configSetters[StructDeclValue] = () =>
                {
                    configSettings.isStructDeclaration = true;
                    configSettings.isGlobalVarDeclaration = false;
                    configSettings.isGlobalFuncDeclaration = false;
                    configSettings.isStatementList = false;
                    configSettings.isTypeDef = false;
                    configSettings.isEnumDeclaration = false;
                    configSettings.isUnionDeclaration = false;
                    configSettings.isInitializer = false;
                    configSettings.isExpression = false;
                };

            configSetters[EnumDeclValue] = () =>
                {
                    configSettings.isStructDeclaration = false;
                    configSettings.isGlobalVarDeclaration = false;
                    configSettings.isGlobalFuncDeclaration = false;
                    configSettings.isStatementList = false;
                    configSettings.isTypeDef = false;
                    configSettings.isEnumDeclaration = true;
                    configSettings.isUnionDeclaration = false;
                    configSettings.isInitializer = false;
                    configSettings.isExpression = false;
                };

            configSetters[UnionDeclValue] = () =>
                {
                    configSettings.isStructDeclaration = false;
                    configSettings.isGlobalVarDeclaration = false;
                    configSettings.isGlobalFuncDeclaration = false;
                    configSettings.isStatementList = false;
                    configSettings.isTypeDef = false;
                    configSettings.isEnumDeclaration = false;
                    configSettings.isUnionDeclaration = true;
                    configSettings.isInitializer = false;
                    configSettings.isExpression = false;
                };


            configSetters[TypeDefValue] = () =>
                {
                    configSettings.isStructDeclaration = false;
                    configSettings.isGlobalVarDeclaration = false;
                    configSettings.isGlobalFuncDeclaration = false;
                    configSettings.isStatementList = false;
                    configSettings.isTypeDef = true;
                    configSettings.isEnumDeclaration = false;
                    configSettings.isUnionDeclaration = false;
                    configSettings.isInitializer = false;
                    configSettings.isExpression = false;
                };

            configSetters[GlobalVarDeclValue] = () =>
                {
                    configSettings.isStructDeclaration = false;
                    configSettings.isGlobalVarDeclaration = true;
                    configSettings.isGlobalFuncDeclaration = false;
                    configSettings.isStatementList = false;
                    configSettings.isTypeDef = false;
                    configSettings.isEnumDeclaration = false;
                    configSettings.isUnionDeclaration = false;
                    configSettings.isInitializer = false;
                    configSettings.isExpression = false;
                };

            configSetters[GlobalFunDeclValue] = () =>
                {
                    configSettings.isStructDeclaration = false;
                    configSettings.isGlobalVarDeclaration = false;
                    configSettings.isGlobalFuncDeclaration = true;
                    configSettings.isStatementList = false;
                    configSettings.isTypeDef = false;
                    configSettings.isEnumDeclaration = false;
                    configSettings.isUnionDeclaration = false;
                    configSettings.isInitializer = false;
                    configSettings.isExpression = false;
                };

            configSetters[StatementListValue] = () =>
                {
                    configSettings.isStructDeclaration = false;
                    configSettings.isGlobalVarDeclaration = false;
                    configSettings.isGlobalFuncDeclaration = false;
                    configSettings.isStatementList = true;
                    configSettings.isTypeDef = false;
                    configSettings.isEnumDeclaration = false;
                    configSettings.isUnionDeclaration = false;
                    configSettings.isInitializer = false;
                    configSettings.isExpression = false;
                };

            configSetters[InitValue] = () =>
                {
                    configSettings.isStructDeclaration = false;
                    configSettings.isGlobalVarDeclaration = false;
                    configSettings.isGlobalFuncDeclaration = false;
                    configSettings.isStatementList = false;
                    configSettings.isTypeDef = false;
                    configSettings.isEnumDeclaration = false;
                    configSettings.isUnionDeclaration = false;
                    configSettings.isInitializer = true;
                    configSettings.isExpression = false;
                };

            configSetters[ExprValue] = () =>
            {
                configSettings.isStructDeclaration = false;
                configSettings.isGlobalVarDeclaration = false;
                configSettings.isGlobalFuncDeclaration = false;
                configSettings.isStatementList = false;
                configSettings.isTypeDef = false;
                configSettings.isEnumDeclaration = false;
                configSettings.isUnionDeclaration = false;
                configSettings.isInitializer = false;
                configSettings.isExpression = true;
            };
        }
    }

}
