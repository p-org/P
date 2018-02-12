using System.Collections.Generic;
using Microsoft.Formula.API;

namespace Microsoft.Pc
{
    internal class MachineInfo
    {
        // TODO: remove "ignore" - violates separation of concerns
        public Dictionary<string, FunInfo> funNameToFunInfo =
            new Dictionary<string, FunInfo>
            {
                ["ignore"] = new FunInfo(
                    false,
                    null,
                    PTranslation.PTypeNull,
                    null,
                    Factory.Instance.AddArg(Factory.Instance.MkFuncTerm(PData.Con_NulStmt), PData.Cnst_Skip).Node)
            };

        public string initStateName = null;
        public Dictionary<string, VariableInfo> localVariableToVarInfo = new Dictionary<string, VariableInfo>();
        public int maxQueueSize = -1;
        public bool maxQueueSizeAssumed = false;
        public List<string> observesEvents = new List<string>();
        public List<string> receiveSet = new List<string>();
        public List<string> sendsSet = new List<string>();
        public SpecType specType = SpecType.SAFETY;
        public Dictionary<string, StateInfo> stateNameToStateInfo = new Dictionary<string, StateInfo>();

        public string type = "REAL";
        public bool IsReal => type == "REAL";
        public bool IsSpec => type == "SPEC";
    }
}