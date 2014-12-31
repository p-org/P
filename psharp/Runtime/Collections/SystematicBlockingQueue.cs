//-----------------------------------------------------------------------
// <copyright file="SystematicBlockingQueue.cs" company="Microsoft">
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
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.PSharp.Scheduling;

using QueueMonitor = System.Threading.Monitor;

namespace Microsoft.PSharp
{
    public class SystematicBlockingQueue<T>
    {
        private List<T> list;
        bool cancelled;
        private HashSet<ThreadInfo> waiters;

        public SystematicBlockingQueue()
        {
            list = new List<T>();
            cancelled = false;
            waiters = new HashSet<ThreadInfo>(new IdentityEqualityComparer<ThreadInfo>());
        }

        public void Add(T item)
        {
            ThreadInfo threadInfo = Runtime.Scheduler.GetCurrentThreadInfo();

            Runtime.Scheduler.schedule(threadInfo, EventType.ADD);

            list.Add(item);

            Debug.Assert(list.Count > 0);

            if (list.Count > 1)
            {
                foreach (var waiter in waiters)
                {
                    Debug.Assert(waiter.Enabled);
                }
            }

            // queue has become non-empty. Wake up waiters!
            if (list.Count == 1)
            {
                foreach (var waiter in waiters)
                {
                    Debug.Assert(cancelled || !waiter.Enabled);
                    Debug.Assert(!waiter.Terminated);
                    waiter.Enabled = true;
                }
            }
        }

        public T Take()
        {
            if (cancelled)
            {
                throw new TaskCanceledException();
            }

            ThreadInfo threadInfo = Runtime.Scheduler.GetCurrentThreadInfo();
            T res;

            if (list.Count == 0)
            {
                threadInfo.Enabled = false;
            }

            waiters.Add(threadInfo);
            
            Runtime.Scheduler.schedule(threadInfo, EventType.TAKE);

            Debug.Assert(cancelled || threadInfo.Terminated || list.Count > 0);

            waiters.Remove(threadInfo);
            
            if (cancelled)
            {
                throw new TaskCanceledException();
            }

            res = list.ElementAt(0);
            list.RemoveAt(0);

            if (list.Count == 0)
            {
                foreach (var waiter in waiters)
                {
                    waiter.Enabled = false;
                }
            }

            return res;
        }

        public void Cancel()
        {
            ThreadInfo threadInfo = Runtime.Scheduler.GetCurrentThreadInfo();
            Runtime.Scheduler.schedule(threadInfo, EventType.CANCEL);
            cancelled = true;

            foreach (var waiter in waiters)
            {
                Debug.Assert(!waiter.Terminated);
                waiter.Enabled = true;
            }
        }
    }
}
