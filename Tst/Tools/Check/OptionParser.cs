namespace Check
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Threading;

    internal class OptionParser
    {
        private enum ParseState
        {
            SwStart = 0,
            SwStartCnt,
            SwName,
            SwNameOrEnd,
            SwEndOrVal,
            FirstVal,
            Next,
            IdVal,
            IntVal,
            StrValStart,
            StrValEnd,
            StrVal,    //// A string without escapes '"..."'
            EStrVal,   //// A string with escapes "..."
            EStrValEsc, 
            Unhandled,
            NParseStates
        }

        private static Func<char, char, Options, ParseState>[][] ParseTable =
            new Func<char, char, Options, ParseState>[(int)ParseState.NParseStates][];

        static OptionParser()
        {
            ParseTable[(int)ParseState.SwStart] = new Func<char, char, Options, ParseState>[]
            {
                (c, la, opt) => c != '-' 
                    ? ParseState.Unhandled 
                    : (la == '-' 
                            ? ParseState.SwStartCnt 
                            : (IsIdStart(la) ? ParseState.SwName : ParseState.Unhandled)),
                (c, la, opt) => c == '/' && IsIdStart(la) ? ParseState.SwName : ParseState.Unhandled,
                (c, la, opt) => IsWhitespace(c) ? ParseState.SwStart : ParseState.Unhandled
            };

            ParseTable[(int)ParseState.SwStartCnt] = new Func<char, char, Options, ParseState>[]
            {
                (c, la, opt) => Start(opt, null, ParseState.SwName)
            };

            ParseTable[(int)ParseState.SwName] = new Func<char, char, Options, ParseState>[]
            {
                (c, la, opt) => !IsIdStart(c) 
                    ? ParseState.Unhandled 
                    : (IsIdMiddle(la) ? Start(opt, null, ParseState.SwNameOrEnd, c) 
                                      : SE(opt, null, ParseState.SwEndOrVal, c))
            };

            ParseTable[(int)ParseState.SwNameOrEnd] = new Func<char, char, Options, ParseState>[]
            {
                (c, la, opt) => IsIdMiddle(la) 
                                    ? App(opt, ParseState.SwNameOrEnd, c)
                                    : AE(opt, ParseState.SwEndOrVal, c)
            };

            ParseTable[(int)ParseState.SwEndOrVal] = new Func<char, char, Options, ParseState>[]
            {
                (c, la, opt) => c != '-' 
                    ? ParseState.Unhandled 
                    : (la == '-' 
                            ? ParseState.SwStartCnt 
                            : (IsIdStart(la) ? ParseState.SwName : ParseState.Unhandled)),
                (c, la, opt) => c == '/' && IsIdStart(la) ? ParseState.SwName : ParseState.Unhandled,
                (c, la, opt) => c == ':' ? ParseState.FirstVal : ParseState.Unhandled,
                (c, la, opt) => IsWhitespace(c) ? ParseState.SwEndOrVal : ParseState.Unhandled
            };

            ParseTable[(int)ParseState.FirstVal] = new Func<char, char, Options, ParseState>[]
            {
                (c, la, opt) => !IsIdStart(c) 
                                    ? ParseState.Unhandled
                                    : (IsIdMiddle(la)
                                       ? Start(opt, OptValueKind.Id, ParseState.IdVal, c)
                                       : SE(opt, OptValueKind.Id, ParseState.Next, c)),
                (c, la, opt) => !char.IsDigit(c) 
                                    ? ParseState.Unhandled
                                    : (char.IsDigit(la)
                                       ? Start(opt, OptValueKind.Integer, ParseState.IntVal, c)
                                       : SE(opt, OptValueKind.Integer, ParseState.Next, c)),
                (c, la, opt) => c == '\"' 
                                    ? Start(opt, OptValueKind.String, ParseState.EStrVal)
                                    : ParseState.Unhandled,
                (c, la, opt) => c == '\'' && la == '\"' 
                                    ? Start(opt, OptValueKind.String, ParseState.StrValStart) 
                                    : ParseState.Unhandled,
                (c, la, opt) => IsWhitespace(c) ? ParseState.FirstVal : ParseState.Unhandled
            };

            ParseTable[(int)ParseState.IdVal] = new Func<char, char, Options, ParseState>[]
            {
                (c, la, opt) => IsIdMiddle(la) 
                                    ? App(opt, ParseState.IdVal, c)
                                    : AE(opt, ParseState.Next, c)
            };

            ParseTable[(int)ParseState.IntVal] = new Func<char, char, Options, ParseState>[]
            {
                (c, la, opt) => char.IsDigit(la) 
                                    ? App(opt, ParseState.IntVal, c)
                                    : AE(opt, ParseState.Next, c)
            };

            ParseTable[(int)ParseState.StrValStart] = new Func<char, char, Options, ParseState>[]
            {
                (c, la, opt) => ParseState.StrVal
            };

            ParseTable[(int)ParseState.StrVal] = new Func<char, char, Options, ParseState>[]
            {
                (c, la, opt) => c == '\"' && la == '\'' 
                                    ? ParseState.StrValEnd 
                                    : App(opt, ParseState.StrVal, c)
            };

            ParseTable[(int)ParseState.StrValEnd] = new Func<char, char, Options, ParseState>[]
            {
                (c, la, opt) => End(opt, ParseState.Next)
            };

            ParseTable[(int)ParseState.EStrVal] = new Func<char, char, Options, ParseState>[]
            {
                (c, la, opt) => c == '\\' ? ParseState.EStrValEsc : ParseState.Unhandled,
                (c, la, opt) => c == '\"' ? End(opt, ParseState.Next) : App(opt, ParseState.EStrVal, c)
            };

            ParseTable[(int)ParseState.EStrValEsc] = new Func<char, char, Options, ParseState>[]
            {
                (c, la, opt) => AppEsc(opt, ParseState.EStrVal, c)
            };

            ParseTable[(int)ParseState.Next] = new Func<char, char, Options, ParseState>[]
            {
                (c, la, opt) => c != '-' 
                    ? ParseState.Unhandled 
                    : (la == '-' 
                            ? ParseState.SwStartCnt 
                            : (IsIdStart(la) ? ParseState.SwName : ParseState.Unhandled)),
                (c, la, opt) => c == '/' && IsIdStart(la) ? ParseState.SwName : ParseState.Unhandled,
                (c, la, opt) => c == ',' ? ParseState.FirstVal : ParseState.Unhandled,
                (c, la, opt) => IsWhitespace(c) ? ParseState.Next : ParseState.Unhandled
            };
        }

        /// <summary>
        /// Attempts to parse the command line arguments contained in the environment.
        /// </summary>
        /// <returns></returns>
        public static bool Parse(
                            out Options options,                             
                            out int errPos,
                            out string cmdStr)
        {
            var cmdLine = Environment.CommandLine.Trim();
            var exeName = Environment.GetCommandLineArgs()[0].Trim();

            //// Need to find where the name of the exeName ends in 
            //// the command line. The exeName is provided after
            //// command line escaping rules have been applied, so we cannot
            //// directly compare exeName with the unescaped cmdLine.
            //// This code uses an approximate logic that quotes are ignored
            //// in the transform from cmdLine -> exeName.

            int i = 0, j = 0;
            char cExe, cCmd;
            while (i < cmdLine.Length)
            {
                cCmd = cmdLine[i];
                cExe = j < exeName.Length ? exeName[j] : '\0';

                if (cCmd == '\"')
                {
                    ++i;
                }
                else if (j == 0 && cCmd == ' ')
                {
                    //// Leading spaces in cmdLine can be ignored                    
                    ++i;
                }
                else if (cCmd == cExe)
                {
                    ++i;
                    ++j;
                }
                else if (j == exeName.Length)
                {

                    break;
                }
                else
                {
                    cmdStr = cmdLine;
                    errPos = i;
                    options = new Options();
                    return false;
                }
            }

            if (i == cmdLine.Length)
            {
                cmdStr = string.Empty;
                errPos = 0;
                options = new Options();
                return true;
            }

            cmdStr = cmdLine.Substring(i);
            return Parse(cmdStr, out options, out errPos);
        }

        /// <summary>
        /// A switch string can contain switches and flags.
        /// Flags have the form:
        /// -f or --flag
        /// Switches have the form:
        /// -s: val_1,...,val_n
        /// --switch: val_1,...,val_n
        /// where val_i is either an integer, quoted string, or bareword.
        /// </summary>
        public static bool Parse(string switchString,
                                 out Options options,
                                 out int errPos)
        {
            //// Contract.Requires(flags != null && switches != null && msgs != null);
            options = new Options();
            var state = ParseState.SwStart;
            char c, la = switchString[0];
            int last = switchString.Length - 1;
            Func<char, char, Options, ParseState>[] actions;
            for (int i = 0; i < switchString.Length; ++i)
            {
                c = la;
                la = i < last ? switchString[i + 1] : '\0';
                actions = ParseTable[(int)state];
                foreach (var a in actions)
                {
                    if ((state = a(c, la, options)) != ParseState.Unhandled)
                    {
                        break;
                    }
                }

                if (state == ParseState.Unhandled)
                {
                    errPos = i;
                    return false;
                }
            }

            if (state == ParseState.FirstVal ||
                state == ParseState.StrValStart ||
                state == ParseState.StrVal ||
                state == ParseState.EStrVal ||
                state == ParseState.EStrValEsc ||
                !options.EndToken())
            {
                errPos = switchString.Length;
                return false;
            }

            errPos = 0;
            return true;
        }

        private static bool IsWhitespace(char c)
        {
            return char.IsWhiteSpace(c) || c == '\0';
        }

        private static bool IsIdStart(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private static bool IsIdMiddle(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        private static ParseState Start(Options opt, OptValueKind? kind, ParseState next, char c = '\0')
        {
            opt.StartToken(kind, c);
            return next;
        }

        private static ParseState App(Options opt, ParseState next, char c)
        {
            opt.AppendToken(c);
            return next;
        }

        private static ParseState End(Options opt, ParseState next)
        {
            return opt.EndToken() ? next : ParseState.Unhandled;
        }

        private static ParseState SE(Options opt, OptValueKind? kind, ParseState next, char c = '\0')
        {
            opt.StartToken(kind, c);
            return opt.EndToken() ? next : ParseState.Unhandled;
        }

        private static ParseState AE(Options opt, ParseState next, char c)
        {
            opt.AppendToken(c);
            return opt.EndToken() ? next : ParseState.Unhandled;
        }

        private static ParseState AppEsc(Options opt, ParseState next, char c)
        {
            switch (c)
            {
                case 'n':
                case 'N':
                    opt.AppendToken('\n');
                    break;
                case 'r':
                case 'R':
                    opt.AppendToken('\r');
                    break;
                case 't':
                case 'T':
                    opt.AppendToken('\t');
                    break;
                default:
                    opt.AppendToken(c);
                    break;
            }

            return next;
        }

    }
}
