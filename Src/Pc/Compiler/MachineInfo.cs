using System.Collections.Generic;
using Microsoft.Formula.API;

namespace Microsoft.Pc
{
    internal class MachineInfo
    {
        public bool IsReal { get { return type == "REAL"; } }
        public bool IsSpec { get { return type == "SPEC"; } }

        public string type;
        public int maxQueueSize;
        public bool maxQueueSizeAssumed;
        public string initStateName;
        public Dictionary<string, StateInfo> stateNameToStateInfo;
        public Dictionary<string, VariableInfo> localVariableToVarInfo;
        public List<string> observesEvents;
        public List<string> receiveSet;
        public List<string> sendsSet;
        public Dictionary<string, FunInfo> funNameToFunInfo;
        public SpecType specType;

        public MachineInfo()
        {
            type = "REAL";
            maxQueueSize = -1;
            maxQueueSizeAssumed = false;
            initStateName = null;
            stateNameToStateInfo = new Dictionary<string, StateInfo>();
            localVariableToVarInfo = new Dictionary<string, VariableInfo>();
            observesEvents = new List<string>();
            receiveSet = new List<string>();
            sendsSet = new List<string>();
            funNameToFunInfo = new Dictionary<string, FunInfo>();
            specType = SpecType.SAFETY;
            funNameToFunInfo["ignore"] = new FunInfo(false, null, PToZing.PTypeNull, null, Factory.Instance.AddArg(Factory.Instance.MkFuncTerm(PData.Con_NulStmt), PData.Cnst_Skip).Node);
        }
    }
}