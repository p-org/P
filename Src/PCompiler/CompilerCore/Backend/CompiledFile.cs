using System.Globalization;
using System.IO;

namespace Plang.Compiler.Backend
{
    public class CompiledFile
    {
        public CompiledFile(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; }
        // Invariant culture so generated numeric literals don't depend on the host locale.
        public StringWriter Stream { get; } = new StringWriter(CultureInfo.InvariantCulture);
        public string Contents => Stream.GetStringBuilder().ToString();
    }
}