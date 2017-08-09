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
            string testDirName = "C:\\VanillaPLanguage\\P\\Tst\\RegressionTests\\Feature1SMLevelDecls";
            DirectoryInfo testDir = new DirectoryInfo(testDirName);
			string[] allFolders = System.IO.Directory.GetDirectories(testDirName, "*", System.IO.SearchOption.AllDirectories);
			List <string> folders = new List<string>();
			foreach (var cand in allFolders)
			{
				string[] names = cand.Split(Path.DirectorySeparatorChar);
				var bottomFolder = names.Last();
                //result: 0: something wrong (not assigned); 1: success; -1: failure
                int result = 0;

                if (bottomFolder == "Prt")
				{
					Console.WriteLine("found Prt folder {0}", cand);
					var folderInfo = new DirectoryInfo(cand);
					string ptDirPath = Path.Combine(folderInfo.Parent.FullName, "Pt");
                    Console.WriteLine("adding Pt folder {0}", ptDirPath);
                    //Console.WriteLine("ptDirPath: {0}", ptDirPath);
                    if (!(Directory.Exists(ptDirPath)))
                    {
                        Console.WriteLine("Creating Pt folder and populating it");
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
                                Console.WriteLine("New testconfig.txt for ptDirPath {0}:\n {1}", ptDirPath, newString);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Pt folder exists; comparing Zing and PTester outputs under {0}", cand);
                        //Compare acceptors under \Zing and \Pt:
                        string zingAcceptor = File.ReadAllText(Path.Combine(folderInfo.Parent.FullName, "Zing", "acc_0.txt"));
                        string ptAcceptor = File.ReadAllText(Path.Combine(folderInfo.Parent.FullName, "Pt", "acc_0.txt"));
                        //Case 1: Zing passes
                        //Zing acceptor's last line is: "EXIT: 0"
                        //PTester acceptor's last line: "EXIT: 0"
                        if (zingAcceptor.Contains("EXIT: 0"))
                        {
                            //Console.WriteLine("Zing acceptor contains EXIT: 0");
                            if (ptAcceptor.Contains("EXIT: 0"))
                            {
                                Console.WriteLine("Both Zing and Pt return pass result: success");
                                result = 1;
                            }
                            else
                            {
                                Console.WriteLine("Zing passes, but PTester doesn't; look manually under {0}", cand);
                                result = -1;
                            }
                        }
                        else
                        {
                            //Case 2: Zing fails:
                            if (zingAcceptor.Contains("Error:"))
                            {
                                //Console.WriteLine("Zing acceptor contains Error");
                                if (ptAcceptor.Contains("ERROR:") || ptAcceptor.Contains("EXIT: 1"))
                                {
                                    //  Case 2.1: user assert fails:
                                    //  Zing acceptor example:
                                    //      Error:                  --find this line
                                    //      P Assertion failed:   
                                    //      Expression: assert(tmp_0.bl,)
                                    //      Comment: gotoStmt1.p(21, 3, 21, 9): error PC1001: Assert failed   --find "error PC1001" and line number (21) in the last line
                                    //  Pt acceptor example:
                                    //      OUT: ERROR: gotoStmt1.p(21,3,21,9): error PC1001: Assert failed   --find "error PC1001" and line number (21) in the 3rd line from the end
                                    //      OUT:
                                    //      EXIT: -1
                                    Console.WriteLine("Both Zing and Pt report error");
                                    if (zingAcceptor.Contains("error PC1001: Assert failed"))
                                    {
                                        Console.WriteLine("User assert failure, case 2.1");
                                        var lines = Regex.Split(zingAcceptor, "\r\n|\r|\n");
                                        string errorLineZing = null;
                                        string errorLinePt = null;
                                        foreach (var line in lines)
                                        {
                                            if (line.Contains("Comment: "))
                                            {
                                                var openBracketPos = line.IndexOf("(");
                                                var firstCommaPos = line.IndexOf(",");
                                                errorLineZing = line.Substring(openBracketPos + 1, firstCommaPos - openBracketPos - 1);
                                                Console.WriteLine("Error line for Zing: {0}", errorLineZing);
                                            }
                                        }
                                        var linesPt = Regex.Split(ptAcceptor, "\r\n|\r|\n");
                                        foreach (var line in linesPt)
                                        {
                                            if (line.Contains("OUT: ERROR:"))
                                            {
                                                var openBracketPos = line.IndexOf("(");
                                                var firstCommaPos = line.IndexOf(",");
                                                errorLinePt = line.Substring(openBracketPos + 1, firstCommaPos - openBracketPos - 1);
                                                Console.WriteLine("Error line for Pt: {0}", errorLinePt);
                                            }
                                        }
                                        if (errorLinePt == null)
                                        {
                                            Console.WriteLine("PTester does not report line number for assert failure; assuming success");
                                            errorLinePt = errorLineZing;
                                        }
                                            if (!(errorLineZing == null) && !(errorLinePt == null) &&
                                            (Convert.ToUInt32(errorLineZing) == Convert.ToUInt32(errorLineZing)))
                                        {
                                            Console.WriteLine("error lines for Zing and Pt are the same or no line number for PTester, success");
                                            result = 1;
                                        }
                                        else
                                        {
                                            Console.WriteLine("error lines for Zing and Pt are different, failure, look manually under {0}", cand);
                                            result = -1;
                                        }
                                    }
                                    else
                                    {
                                        //Console.WriteLine("Other failure in Zing, case 2.2");
                                        //  Case 2.2: P semantics violation:
                                        //  Zing acceptor example (in fact, the only assertion failure detecte by Zing for RegressionTests suite):
                                        //      Error:                                     --find this line
                                        //      P Assertion failed:
                                        //      Expression: assert(false)
                                        //      Comment: Unhandled event exception by machine Main
                                        //  Pt acceptor example1 (other cases could be possible - check after all \Pt folders are populated - TODO):
                                        //      Last lines of the acceptor:
                                        //      OUT: ERROR: Exception of type 'P.Runtime.PrtInvalidPopStatement' was thrown.   --find "ERROR: Exception" (optional)
                                        //      OUT:
                                        //      EXIT: 1   --find this line
                                        //  Pt acceptor example2:
                                        //      OUT: exiting with PRT_STATUS_EVENT_UNHANDLED
                                        //      OUT:
                                        //      EXIT: 1

                                        if (zingAcceptor.Contains("P Assertion failed:"))
                                        {
                                            Console.WriteLine("Zing acceptor contains *P Assertion failed:*");
                                            if (ptAcceptor.Contains("ERROR: Exception"))
                                            {
                                                Console.WriteLine("Pt acceptor contains *ERROR: Exception*, success");
                                                result = 1;
                                            }
                                            else
                                            {
                                                if (ptAcceptor.Contains("PRT_STATUS_EVENT_UNHANDLED") && zingAcceptor.Contains("Unhandled event exception"))
                                                {
                                                    Console.WriteLine("Both Zing and PTester report *Unhandled event exception*");
                                                    result = 1;
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Pt acceptor does not contain *ERROR: Exception*or *PRT_STATUS_EVENT_UNHANDLED*, look manually under {0}", cand);
                                                    result = -1;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("Unknown case, look manually under {0}", cand);
                                            result = -1;
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Unknown case: neither error or assert failure in Zing, look manually under {0}", cand);
                                    result = -1;
                                }

                            }
                        }
                        Console.WriteLine("result for this test is {0}", result);

                    }             //for if (!(Directory.Exists(ptDirPath))), else branch
                }
			}
			Console.WriteLine("End");
            Console.ReadLine();

		}
	}
}
