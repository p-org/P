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
        #region FileOperations
        List<string> FilesToBeMerged;
        public MergeFiles()
        {
            FilesToBeMerged = new List<string>();
        }

        public void AddDirectory(string pathtodirectory)
        {
            foreach (var f in Directory.GetFiles(pathtodirectory))
            {
                FilesToBeMerged.Add(f);
            }
        }

        public void AddFile(string pathtofile)
        {
            FilesToBeMerged.Add(pathtofile);
        }

        public void PrintAll()
        {
            foreach(var f in FilesToBeMerged)
            {
                Console.WriteLine(f);
            }
        }
        #endregion

        #region Merge all files
        Dictionary<string, List<string>> Machines;
        public void MergeAll()
        {
            string concat = "\n";
            Machines = new Dictionary<string, List<string>>();
            foreach (var file in FilesToBeMerged)
            {
                var f = File.OpenRead(file);
                var strRd = new StreamReader(f);
                var Code = strRd.ReadToEnd();
                concat = concat + "\n\n" + Code;
            }

            GetMachines(concat);
        }

        public Dictionary<string, List<string>> GetMachines(string code)
        {
            Dictionary<string, KeyValuePair<string, List<string>>> CodeList = new Dictionary<string, KeyValuePair<string, List<string>>>();

            //Parse the main machine
            Regex rMachName_MainMachine = new Regex(@"main[\s\w]*machine[\s]*([\w]*)");
            Match m1 = rMachName_MainMachine.Match(code);
            while (m1.Success)
            {
                string machineType = m1.Groups[0].ToString();
                string machineName = m1.Groups[1].ToString();
                string pattern = @"\\begin{" + machineName + @"}([\s\S]*)\\end{" + machineName + "}";
                Regex rMachBody = new Regex(pattern);
                Match m2 = rMachBody.Match(code);
                string machineBody = m2.Groups[1].ToString();
                if(CodeList.ContainsKey(machineName))
                {
                   (CodeList.Value.Add(machineBody);
                }
                m1 = m1.NextMatch();
            }
            
            return null;
        }
        #endregion
    }



    class HackyMerge
    {
        
        static void Main(string[] args)
        {

            MergeFiles MF = new MergeFiles();

            Console.WriteLine("HackyMerge");

            //Input to the program is set of files to be merged
            if(args.Length == 0)
            {
                Console.WriteLine("Provide files to be merged ! Bye!");
                return;
            }
            else
            {
                #region get all files
                foreach (var s in args)
                {
                    var path = Path.GetFullPath(s);
                    if(Directory.Exists(path))
                    {
                        MF.AddDirectory(path);
                    }
                    else
                    {
                        if (File.Exists(path) && (Path.GetExtension(path) == ".p"))
                        {
                            MF.AddFile(path);
                        }
                        else
                        {
                            Console.WriteLine("Provide a valid P file as input. Invalid -> {0}", path);
                        }
                    }
                }
                Console.WriteLine("Merging Files ....");
                MF.PrintAll();
                #endregion

            }

            //Perform the merge operation.
            MF.MergeAll();

        }
    }
}
