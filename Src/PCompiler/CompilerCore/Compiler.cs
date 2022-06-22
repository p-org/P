using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Plang.Compiler.Backend;
using Plang.Compiler.TypeChecker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler
{
    public class Compiler : ICompiler
    {
        public void Compile(ICompilationJob job)
        {

            job.Output.WriteInfo($"----------------------------------------");
            job.Output.WriteInfo($"Parsing ...");
            // Run parser on every input file
            PParser.ProgramContext[] trees = job.InputFiles.Select(file =>
            {
                PParser.ProgramContext tree = Parse(job, new FileInfo(file));
                job.LocationResolver.RegisterRoot(tree, new FileInfo(file));
                return tree;
            }).ToArray();

            job.Output.WriteInfo($"Type checking ...");
            // Run typechecker and produce AST
            Scope scope = Analyzer.AnalyzeCompilationUnit(job.Handler, trees);

            // Convert functions to lowered SSA form with explicit cloning
            foreach (Function fun in scope.GetAllMethods())
            {
                IRTransformer.SimplifyMethod(fun);
            }

            job.Output.WriteInfo($"Code generation ...");
            // Run the selected backend on the project and write the files.
            IEnumerable<CompiledFile> compiledFiles = job.Backend.GenerateCode(job, scope);
            foreach (CompiledFile file in compiledFiles)
            {
                job.Output.WriteInfo($"Generated {file.FileName}.");
                job.Output.WriteFile(file);
            }

            // Not every backend has a compilation stage following code generation.
            // For those that do, execute that stage.
            if (job.Backend.HasCompilationStage)
            {
                job.Output.WriteInfo($"----------------------------------------");
                job.Output.WriteInfo($"Compiling {job.ProjectName}...");
                job.Backend.Compile(job);
            }
            job.Output.WriteInfo($"----------------------------------------");
        }

        private static PParser.ProgramContext Parse(ICompilationJob job, FileInfo inputFile)
        {
            string fileText = File.ReadAllText(inputFile.FullName);
            AntlrInputStream fileStream = new AntlrInputStream(fileText);
            PLexer lexer = new PLexer(fileStream);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            PParser parser = new PParser(tokens);
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
            ProcessStartInfo psi = new ProcessStartInfo(exeName)
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

            Process proc = new Process { StartInfo = psi };
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
