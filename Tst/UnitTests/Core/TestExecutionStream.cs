using System;
using System.Collections.Generic;
using System.IO;
using Plang.Compiler;
using Plang.Compiler.Backend;

namespace UnitTests.Core
{
    public class TestExecutionStream : ICompilerOutput
    {
        private readonly DirectoryInfo outputDirectory;
        private readonly List<FileInfo> outputFiles = new List<FileInfo>();

        public TestExecutionStream(DirectoryInfo outputDirectory)
        {
            this.outputDirectory = outputDirectory;
        }

        public IEnumerable<FileInfo> OutputFiles => outputFiles;

        public void WriteMessage(string msg, SeverityKind severity)
        {
            if (severity != SeverityKind.Info)
            {
                Console.Write($"{severity}: ");
            }

            Console.WriteLine(msg);
        }

        public void WriteFile(CompiledFile file)
        {
            var fileName = Path.Combine(outputDirectory.FullName, file.FileName);
            File.WriteAllText(fileName, file.Contents);
            outputFiles.Add(new FileInfo(fileName));
        }

        void ICompilerOutput.WriteError(string msg)
        {
            Console.WriteLine(msg);
        }

        void ICompilerOutput.WriteInfo(string msg)
        {
            Console.WriteLine(msg);
        }

        void ICompilerOutput.WriteWarning(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}