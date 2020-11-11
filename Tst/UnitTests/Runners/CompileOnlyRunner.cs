using Plang.Compiler;
using Plang.Compiler.Backend;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnitTests.Core;

namespace UnitTests.Runners
{
    /// <inheritdoc />
    /// <summary>
    ///     Only run the P compiler in memory. Don't touch the disk.
    /// </summary>
    public class CompileOnlyRunner : ICompilerTestRunner
    {
        private readonly CompilerOutput compilerOutput;
        private readonly IReadOnlyList<FileInfo> inputFiles;

        /// <summary>
        ///     Box a new compile runner
        /// </summary>
        /// <param name="compilerOutput"></param>
        /// <param name="inputFiles">The P source files to compile</param>
        public CompileOnlyRunner(CompilerOutput compilerOutput, IReadOnlyList<FileInfo> inputFiles)
        {
            this.inputFiles = inputFiles;
            this.compilerOutput = compilerOutput;
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
            Compiler compiler = new Compiler();
            StringWriter stdoutWriter = new StringWriter();
            StringWriter stderrWriter = new StringWriter();
            TestCaseOutputStream outputStream = new TestCaseOutputStream(stdoutWriter, stderrWriter);

            CompilationJob job = new CompilationJob(outputStream, scratchDirectory, compilerOutput, inputFiles, Path.GetFileNameWithoutExtension(inputFiles.First().FullName));

            try
            {
                compiler.Compile(job);
            }
            catch (TranslationException)
            {
                stdout = stdoutWriter.ToString().Trim();
                stderr = stderrWriter.ToString().Trim();
                return 1;
            }
            catch (Exception exception)
            {
                job.Output.WriteMessage(exception.Message, SeverityKind.Error);
                throw new CompilerTestException(TestCaseError.TranslationFailed, exception.Message);
            }
            stdout = stdoutWriter.ToString().Trim();
            stderr = stderrWriter.ToString().Trim();
            return 0;
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
                int nameLength = file.FileName.Length;
                int headerWidth = Math.Max(40, nameLength + 4);
                string hdash = new string('=', headerWidth);
                stdout.WriteLine(hdash);
                int prePadding = (headerWidth - nameLength) / 2 - 1;
                int postPadding = headerWidth - prePadding - nameLength - 2;
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