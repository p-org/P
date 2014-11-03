//-----------------------------------------------------------------------
// <copyright file="SChoice.cs" company="Microsoft">
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

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// A scheduling choice. Contains an integer that represents
    /// a machine ID and a boolean that is true if the choice has
    /// been previously explored.
    /// </summary>
    internal class SChoice
    {
        public int Value;
        public bool IsDone;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value">Value</param>
        public SChoice(int value)
        {
            this.Value = value;
            this.IsDone = false;
        }

        /// <summary>
        /// Marks the choice as done.
        /// </summary>
        public void Done()
        {
            this.IsDone = true;
        }
    }
}
