using System;
using System.Collections.Generic;

namespace Plang.PChecker
{
    /// <summary>
    /// Result of parsing commandline options for PChecker
    /// </summary>
    public enum CommandLineParseResult
    {
        Success,
        Failure,
        HelpRequested
    }

    internal class CommandLineOptions
    {
        public static CommandLineParseResult ParseArguments(IEnumerable<string> args, out PCheckerJobConfiguration job)
        {
            foreach (string x in args)
            {
                string arg = x;
                string colonArg = null;
                if (arg[0] == '-')
                {
                    int colonIndex = arg.IndexOf(':');
                    if (colonIndex >= 0)
                    {
                        arg = x.Substring(0, colonIndex);
                        colonArg = x.Substring(colonIndex + 1);
                    }

                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                    }
                }
            }
            job = new PCheckerJobConfiguration();
            return CommandLineParseResult.Success;
        }

        internal static void PrintUsage()
        {
            throw new NotImplementedException();
        }
    }
}