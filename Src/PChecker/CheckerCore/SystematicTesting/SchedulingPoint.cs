// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using PChecker.SystematicTesting.Operations;

namespace PChecker.SystematicTesting
{
    /// <summary>
    /// Awaitable returned by a scheduling decision. It completes synchronously when the
    /// scheduler lets the current operation keep running. Otherwise awaiting it parks the
    /// continuation on the operation, and the scheduler run loop resumes it once the
    /// operation is scheduled again.
    /// </summary>
    public readonly struct SchedulingPoint : ICriticalNotifyCompletion
    {
        internal static readonly SchedulingPoint Continue = default;

        private readonly AsyncOperation SuspendedOperation;

        internal SchedulingPoint(AsyncOperation suspendedOperation)
        {
            SuspendedOperation = suspendedOperation;
        }

        public SchedulingPoint GetAwaiter() => this;

        public bool IsCompleted => SuspendedOperation is null;

        public void GetResult()
        {
        }

        public void OnCompleted(Action continuation) => SuspendedOperation.Continuation = continuation;

        public void UnsafeOnCompleted(Action continuation) => SuspendedOperation.Continuation = continuation;
    }
}
