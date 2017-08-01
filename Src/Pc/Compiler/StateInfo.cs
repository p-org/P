using System.Collections.Generic;

namespace Microsoft.Pc
{
    internal class StateInfo
    {
        public string ownerName;
        public string entryActionName;
        public string exitFunName;
        public bool hasNullTransition;
        public Dictionary<string, TransitionInfo> transitions;
        public Dictionary<string, string> dos;
        public List<string> deferredEvents;
        public StateTemperature temperature;
        public string printedName;

        public bool IsHot => temperature == StateTemperature.HOT;

        public bool IsCold => temperature == StateTemperature.COLD;

        public bool IsWarm => temperature == StateTemperature.WARM;

        public StateInfo(string ownerName, string entryActionName, string exitFunName, StateTemperature temperature, string printedName)
        {
            this.ownerName = ownerName;
            this.entryActionName = entryActionName;
            this.exitFunName = exitFunName;
            this.hasNullTransition = false;
            this.transitions = new Dictionary<string, TransitionInfo>();
            this.dos = new Dictionary<string, string>();
            this.deferredEvents = new List<string>();
            this.temperature = temperature;
            this.printedName = printedName;
        }
    }
}