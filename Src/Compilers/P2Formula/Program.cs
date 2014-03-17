using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;

using Microsoft.Formula.API.ASTQueries;
using Microsoft.Formula.API.Nodes;
using Microsoft.Formula.API.Plugins;
using Microsoft.Formula.Compiler;
using Microsoft.Formula.API;

namespace PCompiler
{
    public class P2FormulaEntry
    {
        public static AST<Model> Compile(string inpFile, string domainPath)
        {
            var canceller = new CancellationTokenSource();
            PParser.PParser parser = null;
            bool res = true;

            try
            {
                var f = new FileStream(inpFile, FileMode.Open, FileAccess.Read);
                var scanner = new PParser.PScanner(f);
                parser = new PParser.PParser(scanner);
                res = parser.Parse(canceller.Token);
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.WriteLine("Couldn't find file: " + inpFile);
                Environment.Exit(-1);
            }

            if (!res || parser.errors.Count > 0)
            {
                foreach (PParser.ParserError err in parser.errors)
                    Console.WriteLine(inpFile + ":" + err.line + "," + err.col + " Parsing Error: " + err.msg);
                Environment.Exit(-1);
            }

            PParser.SemanticPass sem = new PParser.SemanticPass(parser.program);
            if (sem.errors.Count > 0)
            {
                foreach (PParser.SemanticError err in sem.errors)
                    Console.WriteLine(inpFile + ":" + err.line + " Semantic Error: " + err.msg);
                Environment.Exit(-1);
            }

            PParser.CheckUndeclaredIds undeclIds = new PParser.CheckUndeclaredIds(parser.program, sem);

            if (undeclIds.errors.Count > 0)
            {
                foreach (PParser.SemanticError err in undeclIds.errors)
                    Console.WriteLine(inpFile + ":" + err.line + " Semantic Error: " + err.msg);
                Environment.Exit(-1);
            }

            PParser.SideEffectfulExpressionsPass sideEffPass = new PParser.SideEffectfulExpressionsPass(parser.program);

            if (sideEffPass.errors.Count > 0)
            {
                foreach (PParser.SemanticError err in sideEffPass.errors)
                    Console.WriteLine(inpFile + ":" + err.line + " Semantic Error: " + err.msg);
                Environment.Exit(-1);
            }


            PParser.PFormulaBuilder builder = new PParser.PFormulaBuilder(sem, "PData", domainPath + "\\PData.4ml");
            var inpFileWithoutDir = System.IO.Path.GetFileName(inpFile);
            var modelName = MkValidIdentifier(inpFileWithoutDir.EndsWith(".p") ? inpFileWithoutDir.Substring(0, inpFileWithoutDir.Length - 2) : inpFileWithoutDir);
            var r = builder.build(parser.program, modelName);
            if (builder.errors.Count > 0)
            {
                foreach (PParser.BuilderError err in builder.errors)
                    Console.WriteLine(err.line + " Error building Formula AST: " + err.msg);
                Environment.Exit(-1);
            }
            return (AST<Model>)r;
        }

        private static string MkValidIdentifier(string name)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            char c;
            var idname = string.Empty;
            for (int i = 0; i < name.Length; ++i)
            {
                c = name[i];
                if (Char.IsWhiteSpace(c))
                {
                    continue;
                }
                else if (c == '_' || char.IsLetterOrDigit(c))
                {
                    idname += c;
                }
                else
                {
                    idname += "_";
                }
            }

            if (char.IsDigit(idname[0]))
            {
                idname = "_" + idname;
            }

            return idname;
        }
    }
}
