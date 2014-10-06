//-----------------------------------------------------------------------
// <copyright file="Options.cs" company="Microsoft">
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

namespace Microsoft.PSharp.Monitoring
{
    internal static class Options
    {
        internal static string PathToProgram = "";
        internal static SchedulerType Scheduler = SchedulerType.Random;
        internal static int OperationsBound = 0;
        internal static int IterationLimit = 1;
        internal static bool StopAtBug = false;
        internal static bool Debug = false;

        internal enum SchedulerType
        {
            Random = 0,
            RoundRobin = 1,
            PCT = 2
        }
    }
}
