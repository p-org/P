using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using System.IO;
namespace PatchIDL
{
    class Program
    {
        static void Main(string[] args)
        {
            string typesFile = "./PrtTypes_IDL.h";
            string valuesFile = "./PrtValues_IDL.h";
            if (File.Exists(typesFile))
            {

                StreamReader sr = new StreamReader(File.Open(typesFile, FileMode.Open));
                string fileContent = sr.ReadToEnd();
                sr.Close();
                fileContent = fileContent.Replace("typedef struct PRT_FORGNTYPE", "/*typedef struct PRT_FORGNTYPE");
                fileContent = fileContent.Replace("PRT_FORGNTYPE;", "PRT_FORGNTYPE;*/");
                StreamWriter sw = new StreamWriter(File.Open(typesFile, FileMode.Create));
                sw.WriteLine(fileContent);
                sw.Close();
            }
            else
            {
                Console.WriteLine("File {0} does not exist", typesFile);
                Environment.Exit(-1);
            }

            if (File.Exists(valuesFile))
            {

                StreamReader sr = new StreamReader(File.Open(valuesFile, FileMode.Open));
                string fileContent = sr.ReadToEnd();
                sr.Close();
                fileContent = fileContent.Replace("typedef struct PRT_FORGNVALUE", "/*typedef struct PRT_FORGNVALUE");
                fileContent = fileContent.Replace("PRT_FORGNVALUE;", "PRT_FORGNVALUE;*/");
                StreamWriter sw = new StreamWriter(File.Open(valuesFile, FileMode.Create));
                sw.WriteLine(fileContent);
                sw.Close();
            }
            else
            {
                Console.WriteLine("File {0} does not exist", valuesFile);
                Environment.Exit(-1);
            }

        }
    }
}
