using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Formula.API;
using Microsoft.Formula.API.Nodes;

namespace Microsoft.Pc
{
    internal class FunInfo
    {
        public bool isAnonymous;
        public bool isFunProto;
        public List<string> parameterNames;
        // if isAnonymous is true, 
        //    parameterNames is the list of environment variables
        //    parameterNames[0] is the payload parameter
        public Dictionary<string, LocalVariableInfo> localNameToInfo;
        public List<string> localNames;
        public AST<FuncTerm> returnType;
        public Node body;
        public int numFairChoices;
        public Dictionary<AST<Node>, FuncTerm> typeInfo;
        public int maxNumLocals;
        public HashSet<Node> invokeSchedulerFuns;
        public HashSet<Node> invokePluginFuns;
        public HashSet<string> printArgs;

        //for fun proto
        public FunInfo(FuncTerm parameters, AST<FuncTerm> returnType)
        {
            this.isFunProto = true;
            this.returnType = returnType;
            this.parameterNames = new List<string>();
            this.localNameToInfo = new Dictionary<string, LocalVariableInfo>();
            this.localNames = new List<string>();

            int paramIndex = 0;
            while (parameters != null)
            {
                var ft = (FuncTerm)PTranslation.GetArgByIndex(parameters, 0);
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    var varName = ((Cnst)enumerator.Current).GetStringValue();
                    enumerator.MoveNext();
                    var varType = (FuncTerm)enumerator.Current;
                    localNameToInfo[varName] = new LocalVariableInfo(varType, paramIndex);
                    parameterNames.Add(varName);
                }
                parameters = PTranslation.GetArgByIndex(parameters, 1) as FuncTerm;
                paramIndex++;
            }

        }
        // if isAnonymous is true, parameters is actually envVars
        public FunInfo(bool isAnonymous, FuncTerm parameters, AST<FuncTerm> returnType, FuncTerm locals, Node body)
        {
            this.isAnonymous = isAnonymous;
            this.returnType = returnType;
            this.body = body;

            this.parameterNames = new List<string>();
            this.localNameToInfo = new Dictionary<string, LocalVariableInfo>();
            this.localNames = new List<string>();
            this.numFairChoices = 0;
            this.typeInfo = new Dictionary<AST<Node>, FuncTerm>();
            this.maxNumLocals = 0;
            this.invokeSchedulerFuns = new HashSet<Node>();
            this.invokePluginFuns = new HashSet<Node>();
            this.printArgs = new HashSet<string>();

            int paramIndex = 0;
            while (parameters != null)
            {
                var ft = (FuncTerm)PTranslation.GetArgByIndex(parameters, 0);
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    var varName = ((Cnst)enumerator.Current).GetStringValue();
                    enumerator.MoveNext();
                    var varType = (FuncTerm)enumerator.Current;
                    localNameToInfo[varName] = new LocalVariableInfo(varType, paramIndex);
                    parameterNames.Add(varName);
                }
                parameters = PTranslation.GetArgByIndex(parameters, 1) as FuncTerm;
                paramIndex++;
            }

            int localIndex = paramIndex;
            while (locals != null)
            {
                var ft = (FuncTerm)PToZing.GetArgByIndex(locals, 0);
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    var varName = ((Cnst)enumerator.Current).GetStringValue();
                    enumerator.MoveNext();
                    var varType = (FuncTerm)enumerator.Current;
                    localNameToInfo[varName] = new LocalVariableInfo(varType, localIndex);
                    localNames.Add(varName);
                }
                locals = PToZing.GetArgByIndex(locals, 1) as FuncTerm;
                localIndex++;
            }
        }

        public string PayloadVarName
        {
            get
            {
                Debug.Assert(isAnonymous);
                return parameterNames.Last();
            }
        }

        public FuncTerm PayloadType
        {
            get
            {
                Debug.Assert(isAnonymous);
                return localNameToInfo[PayloadVarName].type;
            }
        }
    }
}