// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Contains information about an asynchronous actor operation
    /// that can be controlled during testing.
    /// </summary>
    [DebuggerStepThrough]
    internal sealed class ActorOperation : AsyncOperation
    {
        /// <summary>
        /// The actor that executes this operation.
        /// </summary>
        internal readonly Actor Actor;

        /// <summary>
        /// Unique id of the operation.
        /// </summary>
        public override ulong Id => this.Actor.Id.Value;

        /// <summary>
        /// Unique name of the operation.
        /// </summary>
        public override string Name => this.Actor.Id.Name;

        /// <summary>
        /// Set of events that this operation is waiting to receive. Receiving
        /// any event in the set allows this operation to resume.
        /// </summary>
        private readonly HashSet<Type> EventDependencies;

        /// <summary>
        /// True if it should skip the next receive scheduling point,
        /// because it was already called in the end of the previous
        /// event handler.
        /// </summary>
        internal bool SkipNextReceiveSchedulingPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorOperation"/> class.
        /// </summary>
        internal ActorOperation(Actor actor)
            : base()
        {
            this.Actor = actor;
            this.EventDependencies = new HashSet<Type>();
            this.SkipNextReceiveSchedulingPoint = false;
        }

        /// <summary>
        /// Invoked when the operation is waiting to receive an event of the specified type or types.
        /// </summary>
        internal void OnWaitEvent(IEnumerable<Type> eventTypes)
        {
            this.EventDependencies.UnionWith(eventTypes);
            this.Status = AsyncOperationStatus.BlockedOnReceive;
        }

        /// <summary>
        /// Invoked when the operation received an event from the specified operation.
        /// </summary>
        internal void OnReceivedEvent()
        {
            this.EventDependencies.Clear();
            this.Status = AsyncOperationStatus.Enabled;
        }

        /// <summary>
        /// Invoked when the operation completes.
        /// </summary>
        internal override void OnCompleted()
        {
            this.SkipNextReceiveSchedulingPoint = true;
            base.OnCompleted();
        }
    }
}
