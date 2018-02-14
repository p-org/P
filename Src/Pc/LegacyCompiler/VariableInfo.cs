using Microsoft.Formula.API.Nodes;

namespace Microsoft.Pc
{
    internal class VariableInfo
    {
        public FuncTerm type;

        public VariableInfo(FuncTerm type)
        {
            this.type = type;
        }
    }
}