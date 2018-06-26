using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Pc;
using Microsoft.Pc.Backend;

namespace UnitTests.Core
{
    public class TestExecutionStream : ICompilerOutput
    {
        private readonly DirectoryInfo outputDirectory;
        private readonly List<FileInfo> outputFiles = new List<FileInfo>();

        public IEnumerable<FileInfo> OutputFiles => outputFiles;

        public TestExecutionStream(DirectoryInfo outputDirectory)
        {
            this.outputDirectory = outputDirectory;
        }

        public void WriteMessage(string msg, SeverityKind severity)
        {
            Console.WriteLine(msg);
        }

        public void WriteFile(CompiledFile file)
        {
            string fileName = Path.Combine(outputDirectory.FullName, file.FileName);
            File.WriteAllText(fileName, file.Contents);
            outputFiles.Add(new FileInfo(fileName));
        }
    }
}