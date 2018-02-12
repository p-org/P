using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Formula.API;
using Microsoft.Formula.API.Nodes;
using Microsoft.Formula.Common;
using Microsoft.Formula.Common.Terms;

namespace Microsoft.Pc
{
    public class ErrorReporter
    {
        private const string MsgPrefix = "msg:";
        private const int TypeErrorCode = 1;
        private const string ErrorClassName = "error";

        public SortedSet<Flag> errors;
        public Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo;

        public ErrorReporter()
        {
            this.idToSourceInfo = new Dictionary<string, Dictionary<int, SourceInfo>>();
            this.errors = new SortedSet<Flag>(default(FlagSorter));
        }

        public void PrintErrors(ICompilerOutput Log, CommandLineOptions Options)
        {
            foreach (var f in errors)
            {
                PrintFlag(f, Log, Options);
            }
        }

        public void AddFlag(Flag f)
        {
            if (errors.Contains(f))
            {
                return;
            }
            errors.Add(f);
        }

        public void PrintFlag(Flag f, ICompilerOutput Log, CommandLineOptions Options)
        {
            Log.WriteMessage(FormatError(f, Options), f.Severity);
        }

        public static string FormatError(Flag f, CommandLineOptions Options)
        {
            string programName = "?";
            if (f.ProgramName != null)
            {
                bool shortFileNames = Options.shortFileNames;
                if (shortFileNames)
                {
                    var envParams = new EnvParams(
                        new Tuple<EnvParamKind, object>(EnvParamKind.Msgs_SuppressPaths, true));
                    programName = f.ProgramName.ToString(envParams);
                }
                else
                {
                    programName = (f.ProgramName.Uri.IsFile ? f.ProgramName.Uri.LocalPath : f.ProgramName.ToString());
                }
            }

            string errorNumber = "PC1001"; // todo: invent meaningful error numbers to go with P documentation...
            return
                // this format causes VS to put the errors in the error list window.
                string.Format("{0}({1},{2},{3},{4}): error {5}: {6}",
                              programName,
                              f.Span.StartLine,
                              f.Span.StartCol,
                              f.Span.EndLine,
                              f.Span.EndCol,
                              errorNumber,
                              f.Message);
        }

        public static bool FindIdFromTerm(Term term, out string fileName, out int id)
        {
            id = 0;
            fileName = "";
            if (term.Args.Count() == 0) return false;
            var idTerm = term.Args.Last();
            var symbol = idTerm.Args[0].Symbol as BaseCnstSymb;
            if (symbol == null) return false;
            if (symbol.CnstKind != CnstKind.Numeric) return false;
            id = (int)((Rational)symbol.Raw).Numerator;

            symbol = idTerm.Args[1].Symbol as BaseCnstSymb;
            if (symbol == null) return false;
            if (symbol.CnstKind != CnstKind.String) return false;
            fileName = (string)((Cnst)(symbol).Raw).GetStringValue();
            return true;
        }

        public static bool FindIdFromFuncTerm(FuncTerm idTerm, out string fileName, out int id)
        {
            id = 0;
            fileName = "";
            if (idTerm.Args.Count() == 0) return false;
            var symbol = idTerm.Args.ElementAt(0) as Cnst;
            if (symbol == null) return false;
            if (symbol.CnstKind != CnstKind.Numeric) return false;
            id = (int)(symbol.GetNumericValue()).Numerator;

            symbol = (idTerm.Args.ElementAt(1) as FuncTerm).Args.ElementAt(0) as Cnst;
            if (symbol == null) return false;
            if (symbol.CnstKind != CnstKind.String) return false;
            fileName = (string)(symbol).GetStringValue();
            return true;
        }

        private struct FlagSorter : IComparer<Flag>
        {
            public int Compare(Flag x, Flag y)
            {
                if (x.Severity != y.Severity)
                {
                    return ((int)x.Severity) - ((int)y.Severity);
                }

                int cmp;
                if (x.ProgramName == null && y.ProgramName != null)
                {
                    return -1;
                }
                else if (y.ProgramName == null && x.ProgramName != null)
                {
                    return 1;
                }
                else if (x.ProgramName != null && y.ProgramName != null)
                {
                    cmp = string.Compare(x.ProgramName.ToString(), y.ProgramName.ToString());
                    if (cmp != 0)
                    {
                        return cmp;
                    }
                }

                if (x.Span.StartLine != y.Span.StartLine)
                {
                    return x.Span.StartLine < y.Span.StartLine ? -1 : 1;
                }

                if (x.Span.StartCol != y.Span.StartCol)
                {
                    return x.Span.StartCol < y.Span.StartCol ? -1 : 1;
                }

                cmp = string.Compare(x.Message, y.Message);
                if (cmp != 0)
                {
                    return cmp;
                }

                if (x.Code != y.Code)
                {
                    return x.Code < y.Code ? -1 : 1;
                }

                return 0;
            }
        }
    }
}