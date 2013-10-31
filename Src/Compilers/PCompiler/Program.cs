namespace PCompiler 
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class CompilerEntry
    {
        static void Main(string[] args)
        {
            string model = null;
            string outputPath = null;
            bool erase = true;
            bool kernelMode = false;
            bool emitHeaderComment = false;
            bool emitDebugC = false;
            bool eraseFairnessConstraints = false;

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
                        case "/eraseFairnessConstraints":
                            eraseFairnessConstraints = true;
                            break;
                        default:
                            goto error;
                    }
                }
                else
                {
                    if (model != null)
                        goto error;
                    model = arg;
                }
            }
            if (model == null)
                goto error;

            var comp = new Compiler(model, outputPath != null ? outputPath : Environment.CurrentDirectory, erase, kernelMode, emitHeaderComment, emitDebugC, eraseFairnessConstraints);
            var result = comp.Compile();
            if (!result)
            {
                Console.WriteLine("Compilation failed");
                System.Environment.Exit(-1);
            }
            return;

        error:
            Console.WriteLine("USAGE: pc.exe model.4ml [/doNotErase] [/kernelMode] [/debugC] [/outputDir:path]");
        }
    }
}