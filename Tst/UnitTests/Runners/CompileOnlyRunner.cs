using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plang.Compiler;
using Plang.Compiler.Backend;
using UnitTests.Core;

namespace UnitTests.Runners
{
    /// <inheritdoc />
    /// <summary>
    ///     Only run the P compiler in memory. Don't touch the disk.
    /// </summary>
    public class CompileOnlyRunner : ICompilerTestRunner
    {
        private readonly IList<CompilerOutput> compilerOutputs;
        private readonly IList<string> inputFiles;

        /// <summary>
        ///     Box a new compile runner
        /// </summary>
        /// <param name="compilerOutputs"></param>
        /// <param name="inputFiles">The P source files to compile</param>
        public CompileOnlyRunner(IList<CompilerOutput> compilerOutputs, IList<string> inputFiles)
        {
            this.inputFiles = inputFiles;
            this.compilerOutputs = compilerOutputs;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Run the compiler test without attempting to build the result
        /// </summary>
        /// <param name="scratchDirectory">Unused. Caller is responsible for cleanup.</param>
        /// <param name="stdout">The output produced by the P compiler</param>
        /// <param name="stderr">The error output produced by the P compiler</param>
        /// <returns>0 if compilation successful, 1 if a Translation Exception is thrown, throws a
        ///     CompilerTestException if the compiler crashes.
        /// </returns>
        public int? RunTest(DirectoryInfo scratchDirectory, out string stdout, out string stderr)
        {
            var compiler = new Compiler();
            var stdoutWriter = new StringWriter();
            var stderrWriter = new StringWriter();
            var outputStream = new TestCaseOutputStream(stdoutWriter, stderrWriter);

            var job = new CompilerConfiguration(outputStream, scratchDirectory, compilerOutputs, inputFiles, Path.GetFileNameWithoutExtension(inputFiles.First()));

            try
            {
                var exitCode = compiler.Compile(job);
                stdout = stdoutWriter.ToString().Trim();
                stderr = stderrWriter.ToString().Trim();
                return exitCode;
            }
            catch (Exception exception)
            {
                job.Output.WriteMessage(exception.Message, SeverityKind.Error);
                throw new CompilerTestException(TestCaseError.TranslationFailed, exception.Message);
            }
        }

        private class TestCaseOutputStream : ICompilerOutput
        {
            private readonly TextWriter stderr;
            private readonly TextWriter stdout;

            public TestCaseOutputStream(TextWriter stdout, TextWriter stderr)
            {
                this.stdout = stdout;
                this.stderr = stderr;
            }

            public void WriteMessage(string msg, SeverityKind severity)
            {
                switch (severity)
                {
                    case SeverityKind.Info:
                        stdout.WriteLine(msg);
                        break;

                    case SeverityKind.Warning:
                    case SeverityKind.Error:
                        stderr.WriteLine(msg);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
                }
            }

            public void WriteFile(CompiledFile file)
            {
                var nameLength = file.FileName.Length;
                var headerWidth = Math.Max(40, nameLength + 4);
                var hdash = new string('=', headerWidth);
                stdout.WriteLine(hdash);
                var prePadding = (headerWidth - nameLength) / 2 - 1;
                var postPadding = headerWidth - prePadding - nameLength - 2;
                stdout.WriteLine($"={new string(' ', prePadding)}{file.FileName}{new string(' ', postPadding)}=");
                stdout.WriteLine(hdash);
                stdout.Write(file.Contents);
                if (!file.Contents.EndsWith(Environment.NewLine))
                {
                    stdout.WriteLine();
                }

                stdout.WriteLine(hdash);
                stdout.WriteLine();
            }

            void ICompilerOutput.WriteError(string msg)
            {
                stderr.WriteLine(msg);
            }

            void ICompilerOutput.WriteInfo(string msg)
            {
                stdout.WriteLine(msg);
            }

            void ICompilerOutput.WriteWarning(string msg)
            {
                stderr.WriteLine(msg);
            }
        }
    }
}