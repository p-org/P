using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker
{
    internal static class RuleContextExtensions
    {
        public static T GetParent<T>(this RuleContext ctx) where T : RuleContext { return GetParent<T>(ctx, 0); }

        public static T GetParent<T>(this RuleContext ctx, int i) where T : RuleContext
        {
            for (RuleContext current = ctx.Parent; current != null && i >= 0; current = current.Parent)
            {
                if (current is T parent && i-- == 0)
                {
                    return parent;
                }
            }

            return null;
        }
    }
}
