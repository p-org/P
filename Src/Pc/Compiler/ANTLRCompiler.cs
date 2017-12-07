using System;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Tree;
using Microsoft.Formula.API;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker;

namespace Microsoft.Pc
{
    public class AntlrCompiler : ICompiler
    {
        public bool Compile(ICompilerOutput log, CommandLineOptions options)
        {
            try
            {
                var inputFiles = options.inputFileNames.Select(name => new FileInfo(name)).ToArray();
                var trees = new PParser.ProgramContext[inputFiles.Length];
                var originalFiles = new ParseTreeProperty<FileInfo>();
                ITranslationErrorHandler handler = new DefaultTranslationErrorHandler(originalFiles);

                for (var i = 0; i < inputFiles.Length; i++)
                {
                    FileInfo inputFile = inputFiles[i];
                    trees[i] = Parse(handler, inputFile);
                    originalFiles.Put(trees[i], inputFile);
                }

                Analyzer.AnalyzeCompilationUnit(handler, trees);
                log.WriteMessage("Program valid. Code generation not implemented.", SeverityKind.Info);
                return true;
            }
            catch (TranslationException e)
            {
                log.WriteMessage(e.Message, SeverityKind.Error);
                return false;
            }
        }

        private static PParser.ProgramContext Parse(ITranslationErrorHandler handler, FileInfo inputFile)
        {
            var fileStream = new AntlrFileStream(inputFile.FullName);
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

        public bool Link(ICompilerOutput log, CommandLineOptions options)
        {
            log.WriteMessage("Linking not yet implemented in Antlr toolchain.", SeverityKind.Info);
            return true;
        }
    }
}
