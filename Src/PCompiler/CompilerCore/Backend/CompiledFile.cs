using System.IO;

namespace Plang.Compiler.Backend
{
    public class CompiledFile
    {
        public CompiledFile(string fileName, string dir = "")
        {
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
                fileName = Path.Combine(dir, fileName);
            }
            // save the file name
            FileName = fileName;
        }

        public string FileName { get; }
        public StringWriter Stream { get; } = new StringWriter();
        public string Contents => Stream.GetStringBuilder().ToString();
    }
}