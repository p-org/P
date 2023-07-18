// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using PChecker.SystematicTesting;

namespace PChecker.Actors.Events
{
    /// <summary>
    /// Abstract class representing an event.
    /// </summary>
    [DataContract]
    public abstract class Event
    {
        public string DelayDistribution = null;
        public bool IsOrdered = true;
        public readonly Timestamp EnqueueTime = new();
        public readonly Timestamp DequeueTime = new();
    }
}
