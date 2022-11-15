// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PChecker.Actors.Timers
{
    /// <summary>
    /// Interface of a timer that can send timeout events to its owner actor.
    /// </summary>
    internal interface IActorTimer : IDisposable
    {
        /// <summary>
        /// Stores information about this timer.
        /// </summary>
        TimerInfo Info { get; }
    }
}
