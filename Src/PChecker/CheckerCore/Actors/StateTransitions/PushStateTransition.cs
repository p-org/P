// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using PChecker.Actors.Handlers;

namespace PChecker.Actors.StateTransitions
{
    /// <summary>
    /// Defines a push state transition.
    /// </summary>
    internal sealed class PushStateTransition : EventHandlerDeclaration
    {
        /// <summary>
        /// The target state.
        /// </summary>
        public Type TargetState;

        /// <summary>
        /// Initializes a new instance of the <see cref="PushStateTransition"/> class.
        /// </summary>
        /// <param name="targetState">The target state.</param>
        public PushStateTransition(Type targetState)
        {
            TargetState = targetState;
        }

        internal override bool Inheritable => false;
    }
}