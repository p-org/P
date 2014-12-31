//-----------------------------------------------------------------------
// <copyright file="TestConfiguration.cs" company="Microsoft">
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
using Microsoft.PSharp.Scheduling;

namespace Microsoft.PSharp
{
    public class TestConfiguration
    {
        public string Name;
        public Action EntryPoint;

        public ISchedulingStrategy SchedulingStrategy;
        public int ScheduleLimit = 10;
        public bool UntilBugFound = false;
        public double SoftTimeLimit = 0;

        public bool Completed = false;
        public int NumSteps = 0;
        public int NumSchedules = 0;
        public int NumBuggy = 0;
        public int NumHitDepthBound = 0;
        public int NumDeadlocks = 0;

        public int NumSchedulesToFirstBug = -1;
        public double TimeToFirstBug = -1;

        public double Time = 0;

        public TestConfiguration(string name, Action entryPoint, ISchedulingStrategy schedStrat, int scheduleLimit)
        {
            this.Name = name;
            this.EntryPoint = entryPoint;
            this.SchedulingStrategy = schedStrat;
            this.ScheduleLimit = scheduleLimit;
        }

        public string Result()
        {
            return "{ name: " + Name + ", " +
                "schedulingStrategy: " + SchedulingStrategy.GetDescription() + ", " +
                "scheduleLimit: " + ScheduleLimit + ", " +
                "completed: " + Completed + ", " +
                "numSteps: " + NumSteps + ", " +
                "numSchedules: " + NumSchedules + ", " +
                "numBuggy: " + NumBuggy + ", " +
                "percBuggy: " + (NumBuggy * 100 / NumSchedules) + ", " +
                "numDeadlocks: " + NumDeadlocks + ", " +
                "numHitDepthBound: " + NumHitDepthBound + ", " +
                "numSchedulesToFirstBug: " + NumSchedulesToFirstBug + ", " +
                "timeToFirstBug: " + TimeToFirstBug + ", " +
                "time: " + Time + ", " +
                "softTimeLimit: " + SoftTimeLimit + " }";
        }
    }
}
