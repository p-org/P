// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Defines a goto state transition.
    /// </summary>
    internal sealed class GotoStateTransition : EventHandlerDeclaration
    {
        /// <summary>
        /// The target state.
        /// </summary>
        public Type TargetState;

        /// <summary>
        /// An optional lambda function that executes after the
        /// on-exit handler of the exiting state.
        /// </summary>
        public string Lambda;

        /// <summary>
        /// Initializes a new instance of the <see cref="GotoStateTransition"/> class.
        /// </summary>
        /// <param name="targetState">The target state.</param>
        /// <param name="lambda">Lambda function that executes after the on-exit handler of the exiting state.</param>
        public GotoStateTransition(Type targetState, string lambda)
        {
            this.TargetState = targetState;
            this.Lambda = lambda;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GotoStateTransition"/> class.
        /// </summary>
        /// <param name="targetState">The target state.</param>
        public GotoStateTransition(Type targetState)
        {
            this.TargetState = targetState;
            this.Lambda = null;
        }

        internal override bool Inheritable => false;
    }
}
