// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace PChecker.Actors.Events
{
    /// <summary>
    /// Abstract class representing an event.
    /// </summary>
    [DataContract]
    public abstract class Event
    {
        public ulong? Timestamp = null;
    }
}
