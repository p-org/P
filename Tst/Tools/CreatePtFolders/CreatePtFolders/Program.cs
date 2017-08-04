using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace CreatePtFolders
{
	class Program
	{
		static void Main(string[] args)
		{
			//enumerate subdirs under C:\VanillaPLanguage\P\Tst\RegressionTests:
			//string testDirName = "C:\\VanillaPLanguage\\P\\Tst\\RegressionTests";
            string testDirName = "C:\\VanillaPLanguage\\P\\Tst\\Temp";
            DirectoryInfo testDir = new DirectoryInfo(testDirName);
			string[] allFolders = System.IO.Directory.GetDirectories(testDirName, "*", System.IO.SearchOption.AllDirectories);
			List <string> folders = new List<string>();
			foreach (var cand in allFolders)
			{
				string[] names = cand.Split(Path.DirectorySeparatorChar);
				var bottomFolder = names.Last();
				if (bottomFolder == "Prt")
				{
					Console.WriteLine("found Prt folder {0}", cand);
					var folderInfo = new DirectoryInfo(cand);
					string ptDirPath = Path.Combine(folderInfo.Parent.FullName, "Pt");
                    Console.WriteLine("adding Pt folder {0}", ptDirPath);
                    //Console.WriteLine("ptDirPath: {0}", ptDirPath);
                    if (!(Directory.Exists(ptDirPath)))
                    {
                        Directory.CreateDirectory(ptDirPath);
                        foreach (var file in folderInfo.EnumerateFiles())
                        {
                            if (file.Name == "acc_0.txt")
                            {
                                //Console.WriteLine("file.Name is {0}", file.Name);
                                string acceptorPath = Path.Combine(folderInfo.FullName, "acc_0.txt");
                                //Console.WriteLine("acceptorPath is {0}", acceptorPath);
                                File.Copy(acceptorPath, Path.Combine(Path.Combine(ptDirPath, "acc_0.txt")));
                            }
                            if (file.Name == "testconfig.txt")
                            {
                                //replace "runtime" with "ptester":
                                string line = File.ReadAllText(Path.Combine(folderInfo.FullName, file.Name));
                                string newString = line.Replace("runtime", "ptester");
                                File.WriteAllText(Path.Combine(Path.Combine(ptDirPath, "testconfig.txt")), newString);
                                Console.WriteLine("New testconfig.txt for prtDirPath: {0}", newString);
                            }
                        }
                    }
                    else
                    {
                        //Compare acceptors under \Zing and \Pt:
                        string zingAcceptor = File.ReadAllText(Path.Combine(folderInfo.FullName, "Zing", "acc_0.txt"));
                        string ptAcceptor = File.ReadAllText(Path.Combine(folderInfo.FullName, "Pt", "acc_0.txt"));
                    }
				}
			}
			Console.WriteLine("End");

		}
	}
}
