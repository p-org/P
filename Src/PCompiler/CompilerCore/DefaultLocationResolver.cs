using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;

namespace Plang.Compiler
{
    public class DefaultLocationResolver : ILocationResolver
    {
        private readonly ParseTreeProperty<FileInfo> originalFiles = new ParseTreeProperty<FileInfo>();

        public SourceLocation GetLocation(ParserRuleContext decl)
        {
            if (decl == null || decl.Equals(ParserRuleContext.EmptyContext))
            {
                return new SourceLocation
                {
                    Line = -1,
                    Column = -1,
                    File = null
                };
            }

            return new SourceLocation
            {
                Line = decl.Start.Line,
                Column = decl.Start.Column + 1,
                File = originalFiles.Get(GetRoot(decl))
            };
        }

        public SourceLocation GetLocation(IParseTree ctx, IToken tok)
        {
            if (ctx == null || tok == null)
            {
                return new SourceLocation
                {
                    Line = -1,
                    Column = -1,
                    File = null
                };
            }

            return new SourceLocation
            {
                Line = tok.Line,
                Column = tok.Column + 1,
                File = originalFiles.Get(GetRoot(ctx))
            };
        }

        public SourceLocation GetLocation(IPAST node)
        {
            return GetLocation(node.SourceLocation);
        }

        public void RegisterRoot(ParserRuleContext root, FileInfo inputFile)
        {
            originalFiles.Put(root, inputFile);
        }

        private static IParseTree GetRoot(IParseTree node)
        {
            while (node?.Parent != null)
            {
                node = node.Parent;
            }

            return node;
        }
    }
}