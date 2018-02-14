using Microsoft.Formula.API.Nodes;

namespace Microsoft.Pc
{
    internal class LocalVariableInfo : VariableInfo
    {
        public int index;

        public LocalVariableInfo(FuncTerm type, int index) : base(type)
        {
            this.index = index;
        }
    }
}