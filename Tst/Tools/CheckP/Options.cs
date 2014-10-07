namespace CheckP
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Threading;

    internal enum OptValueKind { Id, Integer, String }

    internal class Options
    {
        private static readonly char[] OptDelims = new char[] { ':' };

        private OptValueKind? kind = null;
        private string token = "";

        private LinkedList<Tuple<string, LinkedList<Tuple<OptValueKind, object>>>> options =
            new LinkedList<Tuple<string, LinkedList<Tuple<OptValueKind, object>>>>();

        public IEnumerable<Tuple<string, LinkedList<Tuple<OptValueKind, object>>>> OptionLists
        {
            get
            {
                return options;
            }
        }

        public void StartToken(OptValueKind? kind, char c = '\0')
        {
            this.kind = kind;
            token = "";
            if (c != '\0')
            {
                token += c;
            }
        }

        public void AppendToken(char c)
        {
            token += c;
        }

        public bool EndToken()
        {
            if (token == "" && kind == null)
            {
                return true;
            }
            else if (kind == null)
            {
                options.AddLast(
                    new Tuple<string, LinkedList<Tuple<OptValueKind, object>>>(
                        token,
                        new LinkedList<Tuple<OptValueKind, object>>()));
            }
            else if (kind == OptValueKind.Integer)
            {
                Contract.Assert(options.Count > 0);
                var opt = options.Last.Value.Item2;
                int i;
                if (!int.TryParse(token, out i))
                {
                    return false;
                }

                opt.AddLast(new Tuple<OptValueKind, object>((OptValueKind)kind, i));
            }
            else if (kind == OptValueKind.Id)
            {
                Contract.Assert(options.Count > 0);
                Contract.Assert(!string.IsNullOrEmpty(token));
                var opt = options.Last.Value.Item2;
                opt.AddLast(new Tuple<OptValueKind, object>((OptValueKind)kind, token));
            }
            else if (kind == OptValueKind.String)
            {
                Contract.Assert(options.Count > 0);
                var opt = options.Last.Value.Item2;
                opt.AddLast(new Tuple<OptValueKind, object>((OptValueKind)kind, token));
            }
            else
            {
                throw new NotImplementedException();
            }

            token = "";
            kind = null;
            return true;
        }

        public bool TryGetOptionsBesides(string[] options, out string[] besides)
        {
            var besidesList = new List<string>();
            foreach (var opt in this.options)
            {
                if (!options.Contains<string>(opt.Item1))
                {
                    besidesList.Add(opt.Item1);
                }
            }

            besides = besidesList.ToArray();
            return besides.Length > 0;
        }

        public bool TryGetOption(string option, out Tuple<OptValueKind, object>[] values)
        {
            var valList = new List<Tuple<OptValueKind, object>>();
            bool isSet = false;

            foreach (var opt in options)
            {
                if (opt.Item1 != option)
                {
                    continue;
                }

                isSet = true;
                if (opt.Item2 == null || opt.Item2.Count == 0)
                {
                    continue;
                }

                valList.AddRange(opt.Item2);
            }

            if (isSet)
            {
                values = valList.ToArray();
                return true;
            }
            else
            {
                values = null;
                return false;
            }
        }

        public bool LoadMore(string activeDirectory, string optFile)
        {
            try
            {
                using (var sr = new StreamReader(Path.Combine(activeDirectory, optFile)))
                {
                    string line;
                    int lineNum = 0;
                    while (!sr.EndOfStream)
                    {
                        ++lineNum;
                        line = sr.ReadLine().Trim();
                        if (string.IsNullOrEmpty(line) || line[0] == ';')
                        {
                            continue;
                        }

                        var splits = line.Split(OptDelims, 2);
                        if (splits.Length != 2)
                        {
                            Console.WriteLine(
                                "ERROR: Could not parse options file {0} at line {1}",
                                optFile,
                                lineNum);
                            return false;
                        }

                        splits[0] = splits[0].Trim();
                        if (string.IsNullOrEmpty(splits[0]))
                        {
                            Console.WriteLine(
                                "ERROR: Could not parse options file {0} at line {1}",
                                optFile,
                                lineNum);
                            return false;
                        }

                        var optVals = new LinkedList<Tuple<OptValueKind, object>>();
                        options.AddLast(
                            new Tuple<string, LinkedList<Tuple<OptValueKind, object>>>(
                                splits[0],
                                optVals));

                        splits[1] = splits[1].Trim();
                        if (!string.IsNullOrEmpty(splits[1]))
                        {
                            optVals.AddLast(new Tuple<OptValueKind, object>(OptValueKind.String, splits[1]));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Could not load options file {0} - {1}", optFile, e.Message);
                return false;
            }

            return true;
        }
    }
}
