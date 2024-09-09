// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PChecker.StateMachines.Events;

namespace PChecker.StateMachines.Exceptions
{
    /// <summary>
    /// Handles the <see cref="ControlledRuntime.OnEventDropped"/> event.
    /// </summary>
    public delegate void OnEventDroppedHandler(Event e, StateMachineId target);
}