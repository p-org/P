using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilerProfileToCsv
{
    class Program
    {
        string filename;
        string outfile;
        TextWriter outStream;

        static int Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return 1;
            }
            p.Process();
            return 0;
        }

        bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "h":
                        case "?":
                        case "help":
                            return false;
                        default:
                            Console.WriteLine("### error: unexpected option: " + arg);
                            return false;
                    }
                }
                else
                {
                    if (filename == null)
                    {
                        filename = arg;
                    }
                    else if (outfile == null)
                    {
                        outfile = arg;
                    }
                    else
                    {
                        Console.WriteLine("### error: too many arguments");
                        return false;
                    }
                }
            }
            if (filename == null)
            {
                Console.WriteLine("### error: missing file name");
                return false;
            }
            if (!System.IO.File.Exists(filename))
            {
                Console.WriteLine("### error: file not found '{0}'", filename);
                return false;
            }
            if (outfile != null)
            {
                outStream = new StreamWriter(outfile);
            }
            else
            {
                outStream = Console.Out;
            }
            return true;
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: CompilerProfileToCsv filename");
            Console.WriteLine("Converts compiler profile output to a CSV file with columns equal to compiler phase and rows equal to files being compiled");
        }

        class PerfInfo
        {
            public string step;
            public string file;
            public TimeSpan duration;
        }


        void Process()
        {
            Dictionary<string, List<PerfInfo>> rows = new Dictionary<string, List<PerfInfo>>();

            using (StreamReader reader = new StreamReader(filename))
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    PerfInfo info = ParsePerfInfo(line);
                    if (info != null)
                    {
                        List<PerfInfo> list;
                        if (!rows.TryGetValue(info.file, out list))
                        {
                            list = new List<PerfInfo>();
                            rows[info.file] = list;
                        }
                        list.Add(info);
                    }
                    line = reader.ReadLine();
                }
            }

            foreach (string key in keys)
            {
                outStream.Write("," + key.Substring(9));
            }
            outStream.WriteLine();

            List<string> files = new List<string>(rows.Keys);
            files.Sort();
            foreach (string file in files)
            {
                List<PerfInfo> list = rows[file];
                outStream.Write(file);
                foreach (var item in list)
                {
                    outStream.Write("," + item.duration);
                }
                outStream.WriteLine();
            }

            if (outfile != null)
            {
                using (outStream)
                {
                    outStream.Close();
                }
            }
        }

        string[] keys = new string[] {
          "Compiler parsing",
          "Compiler installing",
          "Compiler analyzing",
          "Compiler error reporting",
          "Compiler generating C",
        };

        PerfInfo ParsePerfInfo(string line)
        {
            foreach (string key in keys)
            {
                if (line.Contains(key))
                {
                    string[] words = line.Trim().Split(' ');
                    if (words.Length > 3)
                    {
                        string file = words[words.Length - 1];
                        if (file.EndsWith("_p"))
                        {
                            // normalize!
                            file = file.Substring(0, file.Length - 2) + ".p";
                        }
                        return new PerfInfo() {
                            step = key,
                            file = file,
                            duration = TimeSpan.Parse(words[2])
                        };
                    }
                }
            }
            return null;
        }

    }
}
