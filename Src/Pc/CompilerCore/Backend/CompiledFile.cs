using System.IO;

namespace Microsoft.Pc.Backend
{
    public class CompiledFile
    {
        public CompiledFile(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; }
        public StringWriter Stream { get; } = new StringWriter();
        public string Contents => Stream.GetStringBuilder().ToString();
    }
}