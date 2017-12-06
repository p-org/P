using System.IO;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker;

namespace Microsoft.Pc.Antlr
{
    public class PParserErrorListener : IAntlrErrorListener<IToken>
    {
        private readonly ITranslationErrorHandler handler;
        private readonly FileInfo inputFile;

        public PParserErrorListener(FileInfo inputFile, ITranslationErrorHandler handler)
        {
            this.inputFile = inputFile;
            this.handler = handler;
        }

        public void SyntaxError(
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            throw handler.ParseFailure(inputFile, $"line {line}:{charPositionInLine} {msg}");
        }
    }
}