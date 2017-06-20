using Microsoft.Formula.API;
using Microsoft.Formula.API.Nodes;

namespace Microsoft.Pc
{
    internal class ZingTranslationInfo
    {
        public AST<Node> node = null;

        public ZingTranslationInfo(AST<Node> n)
        {
            this.node = n;
        }
    }
}