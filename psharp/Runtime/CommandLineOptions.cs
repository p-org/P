//-----------------------------------------------------------------------
// <copyright file="CommandLineOptions.cs" company="Microsoft">
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
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp
{
    public class CommandLineOptions
    {
        #region fields

        private string[] Options;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args">Array of arguments</param>
        public CommandLineOptions(string[] args)
        {
            this.Options = args;
        }

        /// <summary>
        /// Parses the command line options and assigns values to
        /// the global options for the analyser.
        /// </summary>
        public void Parse()
        {
            for (int idx = 0; idx < this.Options.Length; idx++)
            {
                if (this.Options[idx].ToLower().Equals("/bugfinding") ||
                    this.Options[idx].ToLower().Equals("/bf"))
                {
                    Runtime.Options.Mode = Runtime.Mode.BugFinding;
                }
                else
                {
                    Console.WriteLine("Not recognised command line option.");
                    Environment.Exit(1);
                }
            }
        }

        #endregion
    }
}
