using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Plang.Compiler.TypeChecker.AST;

namespace Plang.Compiler.TypeChecker
{
    public interface ILocationResolver
    {
        SourceLocation GetLocation(ParserRuleContext decl);

        SourceLocation GetLocation(IParseTree ctx, IToken tok);

        SourceLocation GetLocation(IPAST node);

        void RegisterRoot(ParserRuleContext root, FileInfo inputFile);
    }
}