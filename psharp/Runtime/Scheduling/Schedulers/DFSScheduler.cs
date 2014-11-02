//-----------------------------------------------------------------------
// <copyright file="DFSScheduler.cs" company="Microsoft">
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

using Microsoft.PSharp;
using Microsoft.PSharp.Scheduling;

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Class representing a depth first search scheduler.
    /// </summary>
    public sealed class DFSScheduler : IScheduler
    {
        /// <summary>
        /// Stack of scheduling choices.
        /// </summary>
        internal List<List<SChoice>> ScheduleStack;

        internal int Index;

        /// <summary>
        /// Default constructor of the DFSScheduler class.
        /// </summary>
        public DFSScheduler()
        {
            this.ScheduleStack = new List<List<SChoice>>();
            this.Index = 0;
        }

        /// <summary>
        /// Returns the next machine ID to be scheduled.
        /// </summary>
        /// <param name="nextId">Next machine ID</param>
        /// <param name="machineIDs">Machine IDs</param>
        /// <returns>Boolean value</returns>
        bool IScheduler.TryGetNext(out int nextId, List<int> machineIDs)
        {
            SChoice nextChoice = null;
            List<SChoice> scs = null;
            if (this.Index < this.ScheduleStack.Count)
            {
                scs = this.ScheduleStack[this.Index];
            }
            else
            {
                scs = new List<SChoice>();
                foreach (var id in machineIDs)
                {
                    scs.Add(new SChoice(id));
                }

                this.ScheduleStack.Add(scs);
            }

            nextChoice = scs.FirstOrDefault(v => !v.IsDone);
            if (nextChoice == null)
            {
                nextId = -1;
                return false;
            }

            if (this.Index > 0)
            {
                var previousChoice = this.ScheduleStack[this.Index - 1].
                    LastOrDefault(v => v.IsDone);
                previousChoice.IsDone = false;
            }

            nextId = nextChoice.Value;
            nextChoice.Done();
            this.Index++;

            return true;
        }

        /// <summary>
        /// Returns true if the scheduler has finished.
        /// </summary>
        /// <returns>Boolean value</returns>
        bool IScheduler.HasFinished()
        {
            while (this.ScheduleStack.Count > 0 &&
                this.ScheduleStack[this.ScheduleStack.Count - 1].All(v => v.IsDone))
            {
                this.ScheduleStack.RemoveAt(this.ScheduleStack.Count - 1);
                if (this.ScheduleStack.Count > 0)
                {
                    var previousChoice = this.ScheduleStack[this.ScheduleStack.Count - 1].
                        FirstOrDefault(v => !v.IsDone);
                    if (previousChoice != null)
                    {
                        previousChoice.Done();
                    }
                }
            }

            if (this.ScheduleStack.Count == 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resets the scheduler.
        /// </summary>
        void IScheduler.Reset()
        {
            this.Index = 0;
        }

        /// <summary>
        /// Prints the schedule.
        /// </summary>
        private void PrintSchedule()
        {
            Console.WriteLine("Size: " + this.ScheduleStack.Count);
            for (int idx = 0; idx < this.ScheduleStack.Count; idx++)
            {
                Console.WriteLine("Index: " + idx);
                foreach (var sc in this.ScheduleStack[idx])
                {
                    Console.Write(sc.Value + " [" + sc.IsDone + "], ");
                }
                Console.WriteLine();
            }
        }
    }
}
