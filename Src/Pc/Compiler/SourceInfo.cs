using Microsoft.Formula.API;

namespace Microsoft.Pc
{
    public class SourceInfo
    {
        public Span entrySpan;
        public Span exitSpan;

        public SourceInfo(Span entrySpan, Span exitSpan)
        {
            this.entrySpan = entrySpan;
            this.exitSpan = exitSpan;
        }
    }
}