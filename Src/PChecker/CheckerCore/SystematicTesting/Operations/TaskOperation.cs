// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace PChecker.SystematicTesting.Operations
{
    /// <summary>
    /// The operation that runs the test entry method.
    /// </summary>
    [DebuggerStepThrough]
    internal sealed class TaskOperation : AsyncOperation
    {
        /// <inheritdoc/>
        public override ulong Id { get; }

        /// <inheritdoc/>
        public override string Name { get; }

        internal TaskOperation(ulong operationId)
            : base()
        {
            Id = operationId;
            Name = $"Task({operationId})";
        }
    }
}
