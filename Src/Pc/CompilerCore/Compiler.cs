using System;
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
        public void Compile(ICompilationJob job)
        {
            // Run parser on every input file
            var trees = job.InputFiles.Select(file =>
            {
                var tree = Parse(job, file);
                job.LocationResolver.RegisterRoot(tree, file);
                return tree;
            }).ToArray();

            // Run typechecker and produce AST
            var scope = Analyzer.AnalyzeCompilationUnit(job.Handler, trees);

            // Convert functions to lowered SSA form with explicit cloning
            foreach (var fun in scope.GetAllMethods()) IRTransformer.SimplifyMethod(fun);

            // Run the selected backend on the project and write the files.
            var compiledFiles = job.Backend.GenerateCode(job, scope);
            foreach (var file in compiledFiles)
            {
                job.Output.WriteMessage($"Writing {file.FileName}...", SeverityKind.Info);
                job.Output.WriteFile(file);
            }
        }

        private static PParser.ProgramContext Parse(ICompilationJob job, FileInfo inputFile)
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
    }
}