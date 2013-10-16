using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using Microsoft.Formula.API.ASTQueries;
using Microsoft.Formula.API.Nodes;
using Microsoft.Formula.API.Plugins;
using Microsoft.Formula.Compiler;
using Microsoft.Formula.API;

namespace P2Formula
{
    class Program
    {
        static string progname()
        {
            return Environment.CommandLine.Split(' ')[0];
        }
        static void usage()
        {
            Console.WriteLine("Usage: " + progname() + " input.p output.4ml [path_to_domain] [/modelName:<name>]");
            Environment.Exit(-1);
        }
        static void Main(string[] args)
        {
            string inpFile = null, outputFile = null, modelName = null, domPath = null;

            int pos_args = 0;
            foreach (string arg in args)
            {
                if (arg.StartsWith("/"))
                {
                    var colonIndex = arg.IndexOf(':');
                    string argVal, argName;
                    if (colonIndex >= 0)
                    {
                        argVal = arg.Substring(colonIndex + 1);
                        argName = arg.Substring(0, colonIndex);
                    }
                    else
                    {
                        argVal = null;
                        argName = arg;
                    }

                    switch (argName)
                    {
                        case "/modelName":
                            modelName = argVal;
                            break;
                        default:
                            usage();
                            break;
                    }
                }
                else
                {
                    if (pos_args == 0)
                        inpFile = arg;
                    else if (pos_args == 1)
                        outputFile = arg;
                    else if (pos_args == 2)
                        domPath = arg;
                    else
                        usage();

                    pos_args++;
                }
            }

            if (pos_args < 2)
                usage();

            if (modelName == null)
                modelName = inpFile.EndsWith(".p") ? inpFile.Substring(0, inpFile.Length - 2) : inpFile;

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
            catch (System.IO.FileNotFoundException e)
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


            PParser.PFormulaBuilder builder = new PParser.PFormulaBuilder(sem, "PData", domPath);

            var r = builder.build(parser.program, modelName);

            if (builder.errors.Count > 0)
            {
                foreach (PParser.BuilderError err in builder.errors)
                    Console.WriteLine(args[0] + ":" + err.line + " Error building Formula AST: " + err.msg);
                Environment.Exit(-1);

            }
            
            try {
                StreamWriter sw = new StreamWriter(outputFile);
                r.Print(sw);
                sw.Close();
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.WriteLine("Couldn't find output directory: " +  Path.GetDirectoryName(outputFile));
                Environment.Exit(-1);
            }
        }
    }
}
