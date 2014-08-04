//-----------------------------------------------------------------------
// <copyright file="Utilities.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.PSharp.IO
{
    internal static class Utilities
    {
        internal static string Format(string s, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, s, args);
        }

        internal static void ReportError(string s, params object[] args)
        {
            string message = Utilities.Format(s, args);
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Runtime Error: ");
            Console.ForegroundColor = previous;
            Console.WriteLine(message);
        }

        internal static void Verbose(string s, params object[] args)
        {
            if (!Runtime.Options.Verbose)
                return;
            string message = Utilities.Format(s, args);
            Console.WriteLine(message);
        }

        internal static void WriteLine(string s, params object[] args)
        {
            string message = Utilities.Format(s, args);
            Console.WriteLine(message);
        }

        internal static void Write(string s, params object[] args)
        {
            string message = Utilities.Format(s, args);
            Console.Write(message);
        }
    }
}
