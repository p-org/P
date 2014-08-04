//-----------------------------------------------------------------------
// <copyright file="PathExplorer.cs" company="Microsoft">
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
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Static class implementing path exploration methods
    /// for the P# runtime scheduler.
    /// </summary>
    internal static class PathExplorer
    {
        #region fields

        /// <summary>
        /// List containing the explored path.
        /// </summary>
        internal static List<PathStep> Path = new List<PathStep>();

        #endregion

        #region internal API

        /// <summary>
        /// Adds a new path step to the explored path.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="receiver">Receiver machine</param>
        /// <param name="e">Sent event</param>
        internal static void Add(string sender, string receiver, string e)
        {
            PathExplorer.Path.Add(new PathStep(sender, receiver, e));
        }

        /// <summary>
        /// Prints the explored execution path.
        /// </summary>
        internal static void Print()
        {
            Utilities.WriteLine("Printing the explored schedule.\n");

            foreach (var path in PathExplorer.Path)
            {
                ConsoleColor previous = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(path.Sender);
                Console.ForegroundColor = previous;
                Console.Write(" sent ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(path.Event);
                Console.ForegroundColor = previous;
                Console.Write(" to ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(path.Receiver);
                Console.ForegroundColor = previous;
            }

            Utilities.WriteLine("");
        }

        #endregion
    }
}
