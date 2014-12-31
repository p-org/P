//-----------------------------------------------------------------------
// <copyright file="Scheduler.cs" company="Microsoft">
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Class implementing the P# bug-finding scheduler.
    /// </summary>
    public class Scheduler
    {
        public Dictionary<Thread, ThreadInfo> ThreadMap =
            new Dictionary<Thread, ThreadInfo>(new IdentityEqualityComparer<Thread>());

        public List<ThreadInfo> ThreadList = new List<ThreadInfo>();

        public ISchedulingStrategy SchedulingStrategy = null;

        public bool DeadlockHasOccurred = false;
        public bool ErrorHasOccurred = false;
        public bool HitDepthBound = false;

        public ThreadInfo AddNewThreadInfo(Thread thread)
        {
            ThreadInfo threadInfo = new ThreadInfo(this.ThreadList.Count);

            if (this.ThreadList.Count == 0)
            {
                threadInfo.Active = true;
            }

            this.ThreadMap.Add(thread, threadInfo);
            this.ThreadList.Add(threadInfo);

            return threadInfo;
        }

        public void ThreadStarted(ThreadInfo threadInfo)
        {
            lock (threadInfo)
            {
                threadInfo.Started = true;
                System.Threading.Monitor.PulseAll(threadInfo);
                while (!threadInfo.Active)
                {
                    System.Threading.Monitor.Wait(threadInfo);
                }
            }
        }

        public void ThreadEnded(ThreadInfo threadInfo)
        {
            threadInfo.Enabled = false;
            threadInfo.Terminated = true;
            schedule(threadInfo, EventType.THREAD_END);
        }

        public void WaitForThreadToStart(ThreadInfo threadInfo)
        {
            lock (threadInfo)
            {
                while (!threadInfo.Started)
                {
                    System.Threading.Monitor.Wait(threadInfo);
                }
            }
        }

        public bool Reset()
        {
            this.ThreadMap.Clear();
            this.ThreadList.Clear();
            this.DeadlockHasOccurred = false;
            this.ErrorHasOccurred = false;
            this.HitDepthBound = false;
            return this.SchedulingStrategy.Reset();
        }

        private void DeadlockOccurred(ThreadInfo threadInfo)
        {
            this.DeadlockHasOccurred = true;
            foreach (ThreadInfo t in this.ThreadMap.Values)
            {
                lock (t)
                {
                    Debug.Assert(!t.Enabled);
                    t.Active = true;
                    System.Threading.Monitor.PulseAll(t);
                }
            }

            if (!threadInfo.Terminated)
            {
                throw new TaskCanceledException();
            }
        }

        public void ErrorOccurred()
        {
            ThreadInfo threadInfo = GetCurrentThreadInfo();
            this.ErrorHasOccurred = true;
            Debug.Assert(!threadInfo.Terminated);
            foreach (ThreadInfo t in this.ThreadMap.Values)
            {
                t.Enabled = false;
            }
            throw new TaskCanceledException();
        }

        public ThreadInfo GetCurrentThreadInfo()
        {
            return this.ThreadMap[Thread.CurrentThread];
        }

        public void schedule(ThreadInfo threadInfo, EventType eventType)
        {
            if (this.DeadlockHasOccurred)
            {
                if (threadInfo.Terminated)
                {
                    return;
                }
                throw new TaskCanceledException();
            }

            Debug.Assert(threadInfo.Active);

            threadInfo.eventType = eventType;

            // pick enabled thread
            ThreadInfo nextThread = null;

            try
            {
                nextThread = this.SchedulingStrategy.ReachedSchedulingPoint(
                    threadInfo.Id, this.ThreadList);
            }
            catch(NondeterminismException)
            {

                Console.Error.WriteLine("Non-determinism detected!");
                Console.ReadLine();
                Environment.Exit(1);
            }

            if (nextThread == null)
            {
                var enabledThreads = this.ThreadMap.Values.Where((ti) => ti.Enabled).ToList();
                Debug.Assert(enabledThreads.Count == 0);
                // Deadlock
                bool deadlock = false;
                if (this.ErrorHasOccurred)
                {
                    deadlock = true;
                }
                else
                {
                    foreach (ThreadInfo t in this.ThreadMap.Values)
                    {
                        if (!t.Terminated)
                        {
                            deadlock = true;
                            Console.WriteLine("DEADLOCK!");
                            break;
                        }
                    }
                }
                if (deadlock)
                {
                    DeadlockOccurred(threadInfo);
                }
                return;
            }

            if (this.SchedulingStrategy.GetNumSchedPoints() > 100000)
            {
                this.HitDepthBound = true;
                ErrorOccurred();
            }
            
            // POR
            //if (eventType == EventType.TAKE && threadInfo.enabled)
            //{
            //    nextThread = threadInfo;
            //}
            //else
            //{
            //    int i = rand.Next(enabledThreads.Count);
            //    nextThread = enabledThreads.ElementAt(i);
            //}

            // schedule nextThread
            if (threadInfo != nextThread)
            {
                //Console.WriteLine(Thread.CurrentThread.ManagedThreadId + " Going to sleep.");
                threadInfo.Active = false;
                lock (nextThread)
                {
                    Debug.Assert(nextThread.Started);
                    Debug.Assert(!nextThread.Terminated);
                    Debug.Assert(!nextThread.Active);
                    nextThread.Active = true;
                    System.Threading.Monitor.PulseAll(nextThread);
                }

                lock (threadInfo)
                {
                    if (threadInfo.Terminated)
                    {
                        return;
                    }

                    while (!threadInfo.Active)
                    {
                        System.Threading.Monitor.Wait(threadInfo);
                    }

                    if (this.DeadlockHasOccurred)
                    {
                        throw new TaskCanceledException();
                    }
                    
                    Debug.Assert(threadInfo.Active);
                    Debug.Assert(threadInfo.Enabled);
                }
            }

            Debug.Assert((threadInfo.Active && threadInfo.Enabled));
        }
    }
}
