using System;
using System.IO;
using System.Collections.Generic;
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
    class CompilerEntry
    {
        static void Main(string[] args)
        {
            string inpFile = null;
            string domainPath = null;
            string outputPath = null;
            bool erase = true;
            bool kernelMode = false;
            bool emitHeaderComment = false;
            bool emitDebugC = false;
            bool liveness = false;
            bool maceLiveness = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                string colonArg = null;
                if (arg.StartsWith("/"))
                {
                    var colonIndex = arg.IndexOf(':');
                    if (colonIndex >= 0)
                    {
                        arg = args[i].Substring(0, colonIndex);
                        colonArg = args[i].Substring(colonIndex + 1);
                    }

                    switch (arg)
                    {
                        case "/outputDir":
                            outputPath = colonArg;
                            break;
                        case "/doNotErase":
                            if (colonArg != null)
                                goto error;
                            erase = false;
                            break;
                        case "/debugC":
                            if (colonArg != null)
                                goto error;
                            emitDebugC = true;
                            break;
                        case "/kernelMode":
                            if (colonArg != null)
                                goto error;
                            kernelMode = true;
                            break;
                        case "/emitHeaderComment":
                            if (colonArg != null)
                                goto error;
                            emitHeaderComment = true;
                            break;
                        case "/liveness":
                            liveness = true;
                            break;
                        case "/maceLiveness":
                            maceLiveness = true;
                            break;
                        default:
                            goto error;
                    }
                }
                else
                {
                    if (inpFile == null)
                    {
                        inpFile = arg;
                    }
                    else if (domainPath == null)
                    {
                        domainPath = arg;
                    }
                    else
                    {
                        goto error;
                    }
                }
            }
            if (inpFile == null)
                goto error;
            if (liveness && maceLiveness)
                goto error;

            if (domainPath == null)
            {
                var runningLoc = new FileInfo(Assembly.GetExecutingAssembly().Location);
                domainPath = runningLoc.Directory.FullName;
            }

            if (outputPath == null)
            {
                outputPath = Environment.CurrentDirectory;
            }
            else
            {
                try
                {
                    var outInfo = new System.IO.DirectoryInfo(outputPath);
                    if (!outInfo.Exists)
                    {
                        Console.WriteLine("The output directory {0} does not exist", outputPath);
                        goto error;
                    }
                    outputPath = outInfo.FullName;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Bad output directory: {0}", e.Message);
                    goto error;
                }
            }

            var comp = new Compiler(inpFile, domainPath, outputPath, erase, kernelMode, emitHeaderComment, emitDebugC, liveness, maceLiveness);
            var result = comp.Compile();
            if (!result)
            {
                Console.WriteLine("Compilation failed");
                System.Environment.Exit(-1);
            }
            return;

        error:
            Console.WriteLine("USAGE: pcompiler.exe <pFile> [domainPath] [/doNotErase] [/kernelMode] [/debugC] [/liveness] [/maceLiveness] [/outputDir:path]");
        }
    }
}