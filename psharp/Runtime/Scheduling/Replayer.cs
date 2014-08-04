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
    /// Static class implementing execution path replay methods.
    /// </summary>
    internal static class Replayer
    {
        #region fields

        /// <summary>
        /// List containing the explored path.
        /// </summary>
        internal static List<PathStep> ReplayPath = new List<PathStep>();

        #endregion

        #region internal API

        /// <summary>
        /// Adds a new path step to the replayed path.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="receiver">Receiver machine</param>
        /// <param name="e">Sent event</param>
        internal static void Add(string sender, string receiver, string e)
        {
            Replayer.ReplayPath.Add(new PathStep(sender, receiver, e));
        }

        /// <summary>
        /// Replays the previously explored execution path. The
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
        /// Compares the original execution path with the replayed
        /// execution path to check for non deterministic behaviour.
        /// </summary>
        internal static void CompareExecutions()
        {
            Utilities.WriteLine("Comparing the original and the " +
                    "replayed execution paths.\n");

            if (PathExplorer.Path.Count != Replayer.ReplayPath.Count)
            {
                Utilities.ReportError("The replayed execution path [length {0}] " +
                    "differs from the originally explored execution path " +
                    "[length {1}].\n", Replayer.ReplayPath.Count,
                    PathExplorer.Path.Count);
                Utilities.WriteLine("... investigating further ...\n");
            }

            for (int idx = 0; idx < Replayer.ReplayPath.Count; idx++)
            {
                if (PathExplorer.Path.Count == idx)
                    break;

                string originalSender = PathExplorer.Path[idx].Sender;
                string replaySender = Replayer.ReplayPath[idx].Sender;

                Runtime.Assert(originalSender.Equals(replaySender),
                    "The replayed execution path differs " +
                    "from the originally explored execution path: " +
                    "Path Index [{0}] :: Original Sender [{1}] :: " +
                    "Replay Sender [{2}].\n", idx, originalSender,
                    replaySender);

                string originalReceiver = PathExplorer.Path[idx].Receiver;
                string replayReceiver = Replayer.ReplayPath[idx].Receiver;

                Runtime.Assert(originalReceiver.Equals(replayReceiver),
                    "The replayed execution path differs " +
                    "from the originally explored execution path: " +
                    "Path Index [{0}] :: Original Receiver [{1}] :: " +
                    "Replay Receiver [{2}].\n", idx, originalReceiver,
                    originalReceiver);

                string originalEvent = PathExplorer.Path[idx].Event;
                string replayEvent = Replayer.ReplayPath[idx].Event;

                Runtime.Assert(originalEvent.Equals(replayEvent),
                    "The replayed execution path differs " +
                    "from the originally explored execution path: " +
                    "Path Index [{0}] :: Original Event [{1}] :: " +
                    "Replay Event [{2}].\n", idx, originalEvent,
                    originalEvent);
            }
        }

        #endregion
    }
}
