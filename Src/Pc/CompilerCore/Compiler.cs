using System;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.Backend;
using Microsoft.Pc.TypeChecker;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc
{
    public class Compiler : ICompiler
    {
        public bool Compile(ICompilerOutput output, CommandLineOptions options)
        {
            if (options.InputFileNames.Count == 0)
            {
                output.WriteMessage("No input files specified.", SeverityKind.Error);
                return false;
            }

            try
            {
                // Compilation job details
                var inputFiles = options.InputFileNames.Select(name => new FileInfo(name)).ToArray();
                var trees = new PParser.ProgramContext[inputFiles.Length];
                var originalFiles = new ParseTreeProperty<FileInfo>();
                ILocationResolver locationResolver = new DefaultLocationResolver(originalFiles);
                ITranslationErrorHandler handler = new DefaultTranslationErrorHandler(locationResolver, output);

                // Run parser on every input file
                for (var i = 0; i < inputFiles.Length; i++)
                {
                    FileInfo inputFile = inputFiles[i];
                    trees[i] = Parse(handler, inputFile);
                    originalFiles.Put(trees[i], inputFile);
                }

                // Run typechecker and produce AST
                Scope scope = Analyzer.AnalyzeCompilationUnit(handler, trees);

                // Convert functions to lowered SSA form with explicit cloning
                foreach (Function fun in scope.GetAllMethods())
                {
                    IRTransformer.SimplifyMethod(fun);
                }

                // Run the selected backend on the project and write the files.
                ICodeGenerator backend = TargetLanguage.GetCodeGenerator(options.OutputLanguage);
                string projectName = options.ProjectName ?? Path.GetFileNameWithoutExtension(inputFiles[0].Name);
                foreach (CompiledFile compiledFile in backend.GenerateCode(handler, output, projectName, scope))
                {
                    output.WriteMessage($"Writing {compiledFile.FileName}...", SeverityKind.Info);
                    output.WriteFile(compiledFile);
                }

                return true;
            }
            catch (TranslationException e)
            {
                output.WriteMessage(e.Message, SeverityKind.Error);
                return false;
            }
        }

        public bool Link(ICompilerOutput output, CommandLineOptions options)
        {
            output.WriteMessage("Linking not yet implemented in Antlr toolchain.", SeverityKind.Warning);
            return true;
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
