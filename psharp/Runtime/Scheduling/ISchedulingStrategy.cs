//-----------------------------------------------------------------------
// <copyright file="ISchedulingStrategy.cs" company="Microsoft">
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

using System.Collections.Generic;

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Interface of a generic scheduling strategy.
    /// </summary>
    public interface ISchedulingStrategy
    {
        ThreadInfo ReachedSchedulingPoint(int currTid, List<ThreadInfo> threadList);

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        /// <returns>Boolean value</returns>
        bool Reset();

        /// <summary>
        /// Gets the description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        string GetDescription();

        /// <summary>
        /// Returns a random boolean value.
        /// </summary>
        /// <returns>Boolean value</returns>
        bool GetRandomBool();

        /// <summary>
        /// Returns a random integer value.
        /// </summary>
        /// <param name="ceiling">Ceiling</param>
        /// <returns>Integer value</returns>
        int GetRandomInt(int ceiling);

        /// <summary>
        /// Get number of scheduling points.
        /// </summary>
        /// <returns>Integer value</returns>
        int GetNumSchedPoints();
    }
}
