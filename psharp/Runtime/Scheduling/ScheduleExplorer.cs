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

        /// <summary>
        /// List containing the recently scheduled machine IDs.
        /// </summary>
        internal static List<int> ScheduledMachineIDs = new List<int>();

        internal static List<List<int>> CachedSchedule = new List<List<int>>();

        /// <summary>
        /// True if all possible schedules have been explored.
        /// </summary>
        internal static bool AllPossibleSchedulesExplored = false;

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
        /// Tries to add a new scheduled machine ID to the explored
        /// schedule. If schedule caching is enabled and the sequence
        /// of scheduled machine IDs has been already explored then it
        /// does not add the machine ID and returns false.
        /// </summary>
        /// <param name="machineID">Machine ID</param>
        /// <returns>Boolean value</returns>
        internal static bool TryAdd(int machineID)
        {
            var result = true;

            ScheduleExplorer.ScheduledMachineIDs.Add(machineID);
            if (Runtime.Options.CacheExploredSchedules)
            {
                foreach (var schedule in ScheduleExplorer.CachedSchedule)
                {
                    var scheduleLength = ScheduleExplorer.ScheduledMachineIDs.Count;
                    var subSchedule = schedule.Take(scheduleLength);
                    result = !ScheduleExplorer.ScheduledMachineIDs.SequenceEqual(subSchedule);
                    if (!result)
                    {
                        ScheduleExplorer.ScheduledMachineIDs.RemoveAt(scheduleLength - 1);
                        break;
                    }
                }

                return result;
            }

            return result;
        }

        /// <summary>
        /// Caches and resets the explored schedule. The caching
        /// happens only if schedule caching is enabled.
        /// </summary>
        internal static void CacheAndResetExploredSchedule()
        {
            if (Runtime.Options.CacheExploredSchedules)
            {
                CachedSchedule.Add(new List<int>(ScheduleExplorer.ScheduledMachineIDs));
            }

            ScheduleExplorer.Schedule.Clear();
            ScheduleExplorer.ScheduledMachineIDs.Clear();
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
