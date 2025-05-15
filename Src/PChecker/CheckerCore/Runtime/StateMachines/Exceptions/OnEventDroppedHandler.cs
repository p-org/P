// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PChecker.Runtime.Events;

namespace PChecker.Runtime.StateMachines.Exceptions
{
    /// <summary>
    /// Handles the event dropped notification.
    /// </summary>
    public delegate void OnEventDroppedHandler(Event e, StateMachineId target);
}
