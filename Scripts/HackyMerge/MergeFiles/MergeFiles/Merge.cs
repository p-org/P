using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace MergeFiles
{
    class MergeFiles
    {
        public class CodeBlock
        {
            public string machineName;
            public string currentCode;
            public CodeBlock()
            {
                machineName = "";
                currentCode = "";
            }
        }

        public List<CodeBlock> FindCodeToReplace(string file)
        {
            var sr = new StreamReader(File.OpenRead(file));
            var entirecode = sr.ReadToEnd();
            List<CodeBlock> codeBlockList = new List<CodeBlock>();
            //Parse the machine
            Regex rMachName_MainMachine = new Regex(@"machine[\s]*([\w]+)[\s]*\\b");
            Match m1 = rMachName_MainMachine.Match(entirecode);
            while (m1.Success)
            {
                string machineType = m1.Groups[0].ToString();
                string machineName = m1.Groups[1].ToString();
                string pattern = @"\\begin{" +machineName + @"}([\s\S]*)\\end{" +machineName+ "}";
                Regex rMachBody = new Regex(pattern);
                Match m2 = rMachBody.Match(entirecode);
                string machineBody = m2.Groups[1].ToString();
                var cd = new CodeBlock();
                cd.machineName = machineName;
                cd.currentCode = machineBody;
                codeBlockList.Add(cd);
                m1 = m1.NextMatch();
            }

            return codeBlockList;
        }
        #region FileOperations
        string mainFile;
        string modelFile;
        string outFile;
        #endregion
        public string FindGlobalFunction(string file)
        {
            var sr = new StreamReader(File.OpenRead(file));
            var entirecode = sr.ReadToEnd();
            Regex rMachName_MainMachine = new Regex(@"GlobalFunctions[\s]*{([\s\S]+)}");
            Match m1 = rMachName_MainMachine.Match(entirecode);
            return m1.Groups[1].ToString();

        }
        public MergeFiles(string main, string model, string outF)
        {
            mainFile = main; modelFile = model; outFile = outF;
        }
        public void Merge()
        {
            var MainCodeBlock = FindCodeToReplace(mainFile);
            var ModelCodeBlock = FindCodeToReplace(modelFile);
            var GlobalCode = FindGlobalFunction(modelFile);
            var outDump = new StreamWriter(File.Open(outFile, FileMode.Create));
            var sr = new StreamReader(File.OpenRead(mainFile));
            var entirecode = sr.ReadToEnd();
            foreach (var cb in MainCodeBlock)
            {
                var codeMain = MainCodeBlock.Where(c => c.machineName == cb.machineName).First();
                CodeBlock codeModel;
                if(ModelCodeBlock.Where(c => c.machineName == cb.machineName).Count() >0)
                {
                    codeModel = ModelCodeBlock.Where(c => c.machineName == cb.machineName).First();
                }
                else
                {
                    codeModel = new CodeBlock();
                    
                }
                //find start 
                var start = entirecode.IndexOf("\\begin{" + codeMain.machineName + "}");
                string endString = @"\end{" + codeMain.machineName + "}";
                var end = entirecode.IndexOf(endString) + endString.Length;
                var oldCode = entirecode.Substring(start, end - start);
                entirecode =  entirecode.Replace(oldCode, "{\n" + codeMain.currentCode + codeModel.currentCode + GlobalCode + "\n}");
            }

            outDump.WriteLine(entirecode);
            outDump.Close();
        }

    }

    
    

    class HackyMerge
    {
        
        static void Main(string[] args)
        {
            var inputfiles = new List<string>();
            bool performMerge = false;
            string mainFile = "", modelFile = "", MergedFile = "";
            if(args.Length == 0)
            {
                Console.WriteLine("Provide arguments ! Bye!");
                return;
            }
            else
            {
                #region parse params

                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];

                    if (arg[0] == '-' || arg[0] == '/')
                    {
                        string option = arg.TrimStart('/', '-').ToLower();
                        string param = string.Empty;

                        int sepIndex = option.IndexOf(':');

                        if (sepIndex > 0)
                        {
                            param = option.Substring(sepIndex + 1);
                            option = option.Substring(0, sepIndex);
                        }
                        else if (sepIndex == 0)
                        {
                            Console.WriteLine("Error");
                            return;
                        }

                        switch(option)
                        {
                            case "concate":
                                {
                                    inputfiles = param.Split('+').ToList();
                                }
                                break;
                            case "out":
                                {
                                    var outFile = Path.GetFullPath(param);
                                    var outF = File.OpenWrite(outFile);
                                    StreamWriter sw = new StreamWriter(outF);
                                    var listoffiles = inputfiles.Select(f => Path.GetFullPath(f)).ToList();
                                    foreach(var file in listoffiles)
                                    {
                                        StreamReader sr = new StreamReader(File.OpenRead(file));
                                        sw.WriteLine(sr.ReadToEnd());
                                        sr.Close();
                                    }
                                    sw.Close();
                                    outF.Close();
                                }
                                break;
                            case "merge":
                                {
                                    performMerge = true;
                                    MergedFile = param;
                                }
                                break;
                            case "main":
                                mainFile = param;
                                break;
                            case "model":
                                modelFile = param;
                                break;
                            case "h":
                                Console.WriteLine("-model:file          model file to be merged");
                                Console.WriteLine("-main:file           main file to be merged");
                                Console.WriteLine("-merge:out_file      perform merge operation with output dumped in out_file");
                                Console.WriteLine("-concate:file,file   concatinate the comma separated files");
                                Console.WriteLine("-out:out_file        dump output in the out_file");
                                return;
                            default:
                                Console.WriteLine("Provide Correct Parameters");
                                return;
                        }
                    }
                }
                #endregion
            }

            if(performMerge)
            {
                MergeFiles MF = new MergeFiles(mainFile, modelFile, MergedFile);
                MF.Merge();
            }
           

        }
    }
}
