using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Plang.Compiler.Backend;
using Plang.Compiler.TypeChecker;

namespace Plang.Compiler
{
    public class Compiler : ICompiler
    {
        public int Compile(ICompilerConfiguration job)
        {
            job.Output.WriteInfo($"Parsing ...");

            // Run parser on every input file
            PParser.ProgramContext[] trees = null;
            try
            {
                trees = job.InputPFiles.Select(file =>
                {
                    var tree = Parse(job, new FileInfo(file));
                    job.LocationResolver.RegisterRoot(tree, new FileInfo(file));
                    return tree;
                }).ToArray();
            }
            catch (TranslationException e)
            {
                job.Output.WriteError("[Parser Error:]\n" + e.Message);
                Environment.ExitCode = 1;
                return Environment.ExitCode;
            }

            job.Output.WriteInfo($"Type checking ...");
            // Run type checker and produce AST
            Scope scope = null;
            try
            {
                scope = Analyzer.AnalyzeCompilationUnit(job, trees);
            }
            catch (TranslationException e)
            {
                job.Output.WriteError("[Error:]\n" + e.Message);
                Environment.ExitCode = 1;
                return Environment.ExitCode;
            }

            // Convert functions to lowered SSA form with explicit cloning
            foreach (var fun in scope.GetAllMethods())
            {
                IRTransformer.SimplifyMethod(fun);
            }

            DirectoryInfo parentDirectory = job.OutputDirectory;
            foreach (var entry in job.OutputLanguages.Distinct())
            {
                job.OutputDirectory = Directory.CreateDirectory(Path.Combine(parentDirectory.FullName, entry.ToString()));
                job.Output = new DefaultCompilerOutput(job.OutputDirectory);
                job.Backend = TargetLanguage.GetCodeGenerator(entry);

                job.Output.WriteInfo($"----------------------------------------");
                job.Output.WriteInfo($"Code generation for {entry}...");

                // Run the selected backend on the project and write the files.
                var compiledFiles = job.Backend.GenerateCode(job, scope);
                foreach (var file in compiledFiles)
                {
                    job.Output.WriteInfo($"Generated {file.FileName}.");
                    job.Output.WriteFile(file);
                }

                // Not every backend has a compilation stage following code generation.
                // For those that do, execute that stage.
                if (job.Backend.HasCompilationStage)
                {
                    job.Output.WriteInfo($"Compiling generated code...");
                    try
                    {
                        job.Backend.Compile(job);
                    }
                    catch (TranslationException e)
                    {
                        job.Output.WriteError($"[{entry} Compiling Generated Code:]\n" + e.Message);
                        job.Output.WriteError("[THIS SHOULD NOT HAVE HAPPENED, please report it to the P team or create a GitHub issue]\n" + e.Message);
                        Environment.ExitCode = 2;
                        return Environment.ExitCode;
                    }
                }
                else
                {
                    job.Output.WriteInfo($"Build succeeded.");
                }
            }

            job.Output.WriteInfo($"----------------------------------------");
            job.Output.WriteInfo($"Compilation succeeded.");

            Environment.ExitCode = 0;
            return Environment.ExitCode;
        }

        private static PParser.ProgramContext Parse(ICompilerConfiguration job, FileInfo inputFile)
        {
            var fileText = File.ReadAllText(inputFile.FullName);
            var fileStream = new AntlrInputStream(fileText);
            var lexer = new PLexer(fileStream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new PParser(tokens);
            parser.RemoveErrorListeners();

            // As currently implemented, P can be parsed by SLL. However, if extensions to the
            // language are later added, this will remain robust. There is a performance penalty
            // when a file doesn't parse (it is parsed twice), but most of the time we expect
            // programs to compile and for code generation to take about as long as parsing.
            try
            {
                // Stage 1: use fast SLL parsing strategy
                parser.Interpreter.PredictionMode = PredictionMode.Sll;
                parser.ErrorHandler = new BailErrorStrategy();
                return parser.program();
            }
            catch (Exception e) when (e is RecognitionException || e is OperationCanceledException)
            {
                // Stage 2: use slower LL(*) parsing strategy
                job.Output.WriteMessage("Reverting to LL(*) parsing strategy.", SeverityKind.Warning);
                tokens.Reset();
                parser.AddErrorListener(new PParserErrorListener(inputFile, job.Handler));
                parser.Interpreter.PredictionMode = PredictionMode.Ll;
                parser.ErrorHandler = new DefaultErrorStrategy();
                return parser.program();
            }
        }

        /// <inheritdoc />
        /// <summary>
        ///     This error listener converts Antlr parse errors into translation exceptions via the
        ///     active error handler.
        /// </summary>
        private class PParserErrorListener : IAntlrErrorListener<IToken>
        {
            private readonly ITranslationErrorHandler handler;
            private readonly FileInfo inputFile;

            public PParserErrorListener(FileInfo inputFile, ITranslationErrorHandler handler)
            {
                this.inputFile = inputFile;
                this.handler = handler;
            }

            public void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
                string msg, RecognitionException e)
            {
                throw handler.ParseFailure(inputFile, $"line {line}:{charPositionInLine} {msg}");
            }
        }

        public static int RunWithOutput(string activeDirectory,
            out string stdout,
            out string stderr, string exeName,
            params string[] argumentList)
        {
            var psi = new ProcessStartInfo(exeName)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WorkingDirectory = activeDirectory,
                Arguments = string.Join(" ", argumentList)
            };

            string mStdout = "", mStderr = "";

            var proc = new Process { StartInfo = psi };
            proc.OutputDataReceived += (s, e) => { mStdout += $"{e.Data}\n"; };
            proc.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    mStderr += $"{e.Data}\n";
                }
            };

            proc.Start();
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();
            proc.WaitForExit();
            stdout = mStdout;
            stderr = mStderr;
            return proc.ExitCode;
        }
    }
}