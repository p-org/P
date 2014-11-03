//-----------------------------------------------------------------------
// <copyright file="Replayer.cs" company="Microsoft">
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
    /// Static class implementing execution schedule replay methods.
    /// </summary>
    internal static class Replayer
    {
        #region fields

        /// <summary>
        /// List containing the replayed schedule.
        /// </summary>
        internal static List<ScheduleStep> ReplayedSchedule = new List<ScheduleStep>();

        #endregion

        #region internal API

        /// <summary>
        /// Adds a new schedule step to the replayed schedule.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="receiver">Receiver machine</param>
        /// <param name="e">Sent event</param>
        internal static void Add(string sender, string receiver, string e)
        {
            Replayer.ReplayedSchedule.Add(new ScheduleStep(sender, receiver, e));
        }

        /// <summary>
        /// Replays the previously explored execution schedule. The
        /// main machine is constructed with an optional payload.
        /// The input payload must be the same as the one in the
        /// previous execution to achieve deterministic replaying.
        /// </summary>
        /// <param name="m">Main machine</param>
        /// <param name="payload">Payload</param>
        internal static void Run(Type m, Object payload = null)
        {
            Machine.Factory.CreateMachine(m, payload);
            Runtime.IsRunning = true;
        }

        /// <summary>
        /// Compares the original execution schedule with the replayed
        /// execution schedule to check for non deterministic behaviour.
        /// </summary>
        internal static void CompareExecutions()
        {
            Utilities.WriteLine("Comparing the original and the " +
                    "replayed execution schedules.\n");

            if (ScheduleExplorer.Schedule.Count != Replayer.ReplayedSchedule.Count)
            {
                Utilities.ReportError("The replayed schedule [length {0}] " +
                    "differs from the originally explored schedule " +
                    "[length {1}].\n", Replayer.ReplayedSchedule.Count,
                    ScheduleExplorer.Schedule.Count);
                Utilities.WriteLine("... investigating further ...\n");
            }

            for (int idx = 0; idx < Replayer.ReplayedSchedule.Count; idx++)
            {
                if (ScheduleExplorer.Schedule.Count == idx)
                    break;

                string originalSender = ScheduleExplorer.Schedule[idx].Sender;
                string replaySender = Replayer.ReplayedSchedule[idx].Sender;

                Runtime.Assert(originalSender.Equals(replaySender),
                    "The replayed execution schedule differs " +
                    "from the originally explored execution schedule: " +
                    "Schedule Index [{0}] :: Original Sender [{1}] :: " +
                    "Replay Sender [{2}].\n", idx, originalSender,
                    replaySender);

                string originalReceiver = ScheduleExplorer.Schedule[idx].Receiver;
                string replayReceiver = Replayer.ReplayedSchedule[idx].Receiver;

                Runtime.Assert(originalReceiver.Equals(replayReceiver),
                    "The replayed execution schedule differs " +
                    "from the originally explored execution schedule: " +
                    "Schedule Index [{0}] :: Original Receiver [{1}] :: " +
                    "Replay Receiver [{2}].\n", idx, originalReceiver,
                    originalReceiver);

                string originalEvent = ScheduleExplorer.Schedule[idx].Event;
                string replayEvent = Replayer.ReplayedSchedule[idx].Event;

                Runtime.Assert(originalEvent.Equals(replayEvent),
                    "The replayed execution schedule differs " +
                    "from the originally explored execution schedule: " +
                    "Schedule Index [{0}] :: Original Event [{1}] :: " +
                    "Replay Event [{2}].\n", idx, originalEvent,
                    originalEvent);
            }
        }

        #endregion
    }
}
