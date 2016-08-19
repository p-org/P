namespace CheckP
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;

    internal enum OptValueKind { Id, Integer, String }

    internal class Options
    {
        private static readonly char[] OptDelims = new char[] { ':' };

        private OptValueKind? kind = null;
        private string token = "";

        private LinkedList<Tuple<string, LinkedList<Tuple<OptValueKind, object>>>> options =
            new LinkedList<Tuple<string, LinkedList<Tuple<OptValueKind, object>>>>();

        public Options()
        {
            Variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<Tuple<string, LinkedList<Tuple<OptValueKind, object>>>> OptionLists
        {
            get
            {
                return options;
            }
        }

        /// <summary>
        /// An optional list of variables can be defined which will be substituted in the property values.
        /// For example, the variable "Configuration=Debug" can be substituted anywhere in an options
        /// file that uses the special variable syntax $(Configuration).
        /// </summary>
        public Dictionary<string,string> Variables { get; private set; }


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
                                "ERROR: Expecting 'name: value' syntax in options file {0} at line {1}",
                                optFile,
                                lineNum);
                            return false;
                        }

                        string name  = splits[0].Trim();
                        if (string.IsNullOrEmpty(name))
                        {
                            Console.WriteLine(
                                "ERROR: Expecting line to start with a property name in options file {0} at line {1}",
                                optFile,
                                lineNum);
                            return false;
                        }

                        var optVals = new LinkedList<Tuple<OptValueKind, object>>();
                        string value = splits[1].Trim();
                        if (!string.IsNullOrEmpty(value))
                        {
                            value = SubstituteVariables(value);
                            optVals.AddLast(new Tuple<OptValueKind, object>(OptValueKind.String, value));
                        }

                        options.AddLast(
                            new Tuple<string, LinkedList<Tuple<OptValueKind, object>>>(
                                name,
                                optVals));

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

        private string SubstituteVariables(string value)
        {
            if (Variables == null)
            {
                return value; 
            }
            StringBuilder result = new StringBuilder();
            for (int i = 0, n = value.Length; i<n; i++)
            {
                char ch = value[i];
                if (ch == '$')
                {
                    if (i + 1 < n && value[i+1] == '(')
                    {
                        i++;                        
                        int start = i + 1;
                        int end = start;
                        while (i < n)
                        {
                            if (value[i] == ')')
                            {
                                end = i;
                                break;
                            }
                            i++;
                        }
                        if (end > start + 1)
                        {
                            string name = value.Substring(start, end - start);
                            string expansion = null;
                            if (Variables.TryGetValue(name, out expansion))
                            {
                                result.Append(expansion);
                                i = end;
                            }
                        }
                    }
                    else
                    {
                        result.Append(ch);
                    }
                }
                else
                {
                    result.Append(ch);
                }
            }
            return result.ToString();
        }
    }
}
