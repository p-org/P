using Microsoft.Formula.API.Nodes;

namespace Microsoft.Pc
{
    internal class EventInfo
    {
        public int maxInstances;  // -1 represents no bound
        public bool maxInstancesAssumed;
        public FuncTerm payloadType;

        public EventInfo(FuncTerm payloadType)
        {
            this.payloadType = payloadType;
            this.maxInstances = -1;
        }

        public EventInfo(int maxInstances, bool maxInstancesAssumed, FuncTerm payloadType)
        {
            this.maxInstances = maxInstances;
            this.maxInstancesAssumed = maxInstancesAssumed;
            this.payloadType = payloadType;
        }
    }
}