using System;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.Backend;
using Microsoft.Pc.TypeChecker;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc
{
    public class Compiler : ICompiler
    {
        public void Compile(ICompilationJob job)
        {
            // Compilation job details
            var trees = new PParser.ProgramContext[job.InputFiles.Count];

            // Run parser on every input file
            for (var i = 0; i < job.InputFiles.Count; i++)
            {
                FileInfo inputFile = job.InputFiles[i];
                trees[i] = Parse(job.Handler, inputFile);
                job.LocationResolver.RegisterRoot(trees[i], inputFile);
            }

            // Run typechecker and produce AST
            Scope scope = Analyzer.AnalyzeCompilationUnit(job.Handler, trees);

            // Convert functions to lowered SSA form with explicit cloning
            foreach (Function fun in scope.GetAllMethods())
            {
                IRTransformer.SimplifyMethod(fun);
            }

            // Run the selected backend on the project and write the files.
            var compiledFiles = job.Backend.GenerateCode(job, scope);
            foreach (CompiledFile file in compiledFiles)
            {
                job.Output.WriteMessage($"Writing {file.FileName}...", SeverityKind.Info);
                job.Output.WriteFile(file);
            }
        }

        private static PParser.ProgramContext Parse(ITranslationErrorHandler handler, FileInfo inputFile)
        {
            string fileText = File.ReadAllText(inputFile.FullName);
            var fileStream = new AntlrInputStream(fileText);
            var lexer = new PLexer(fileStream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new PParser(tokens);
            parser.RemoveErrorListeners();

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
                tokens.Reset();
                parser.AddErrorListener(new PParserErrorListener(inputFile, handler));
                parser.Interpreter.PredictionMode = PredictionMode.Ll;
                parser.ErrorHandler = new DefaultErrorStrategy();
                return parser.program();
            }
        }
    }
}
