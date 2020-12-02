using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Plang.Compiler.Backend;
using Plang.Compiler.Backend.CSharp;
using Plang.Compiler.TypeChecker;
using System;
using System.IO;
using System.Linq;

namespace Plang.Compiler
{
    public class Compiler : ICompiler
    {
        public void Compile(ICompilationJob job)
        {

            job.Output.WriteInfo($"----------------------------------------");
            job.Output.WriteInfo($"Parsing ..");
            // Run parser on every input file
            PParser.ProgramContext[] trees = job.InputFiles.Select(file =>
            {
                PParser.ProgramContext tree = Parse(job, file);
                job.LocationResolver.RegisterRoot(tree, file);
                return tree;
            }).ToArray();

            job.Output.WriteInfo($"Type checking ...");
            // Run typechecker and produce AST
            Scope scope = Analyzer.AnalyzeCompilationUnit(job.Handler, trees);

            // Convert functions to lowered SSA form with explicit cloning
            foreach (TypeChecker.AST.Declarations.Function fun in scope.GetAllMethods())
            {
                IRTransformer.SimplifyMethod(fun);
            }
            job.Output.WriteInfo($"Code generation ....");
            // Run the selected backend on the project and write the files.
            System.Collections.Generic.IEnumerable<CompiledFile> compiledFiles = job.Backend.GenerateCode(job, scope);
            foreach (CompiledFile file in compiledFiles)
            {
                job.Output.WriteInfo($"Generated {file.FileName}");
                job.Output.WriteFile(file);
            }
            job.Output.WriteInfo($"----------------------------------------");

            // Compiling the generated C# code
            // TODO: This is a special case right now but needs to be factored in after the Java code path is available
            if(job.OutputLanguage == CompilerOutput.CSharp)
            {
                job.Output.WriteInfo($"Compiling {job.ProjectName}.csproj ..\n");
                CSharpCodeCompiler.Compile(job);
                job.Output.WriteInfo($"----------------------------------------");

            }
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
    }
}