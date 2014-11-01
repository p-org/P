//-----------------------------------------------------------------------
// <copyright file="ScheduleExplorer.cs" company="Microsoft">
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
    /// Static class implementing schedule exploration methods
    /// for the P# runtime scheduler.
    /// </summary>
    internal static class ScheduleExplorer
    {
        #region fields

        /// <summary>
        /// List containing the recently explored schedule steps.
        /// </summary>
        internal static List<ScheduleStep> Schedule = new List<ScheduleStep>();

        #endregion

        #region internal API

        /// <summary>
        /// Adds a new schedule step to the explored schedule.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="receiver">Receiver machine</param>
        /// <param name="e">Sent event</param>
        internal static void Add(string sender, string receiver, string e)
        {
            ScheduleExplorer.Schedule.Add(new ScheduleStep(sender, receiver, e));
        }

        /// <summary>
        /// Pushes a new scheduling decision to the cache.
        /// </summary>
        /// <param name="chosenID">Chosen ID</param>
        /// <param name="enabledIDs">Enabled IDs</param>
        internal static void Push(int chosenID, int enabledIDs)
        {

        }

        /// <summary>
        /// Resets the explored schedule.
        /// </summary>
        internal static void ResetExploredSchedule()
        {
            ScheduleExplorer.Schedule.Clear();
        }

        /// <summary>
        /// Prints the explored schedule.
        /// </summary>
        internal static void Print()
        {
            Utilities.WriteLine("Printing the explored schedule.\n");

            foreach (var schedule in ScheduleExplorer.Schedule)
            {
                ConsoleColor previous = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(schedule.Sender);
                Console.ForegroundColor = previous;
                Console.Write(" sent ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(schedule.Event);
                Console.ForegroundColor = previous;
                Console.Write(" to ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(schedule.Receiver);
                Console.ForegroundColor = previous;
            }

            Utilities.WriteLine("");
        }

        #endregion
    }
}
