// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using PChecker.SystematicTesting.Operations;

namespace PChecker.SystematicTesting.Strategies
{
    internal static class EnabledOperations
    {
        /// <summary>
        /// Fills the buffer with the enabled operations. Strategies run this on every scheduling
        /// step, so they reuse one buffer instead of allocating a filtered list each time.
        /// </summary>
        internal static List<AsyncOperation> Collect(IEnumerable<AsyncOperation> ops, List<AsyncOperation> buffer)
        {
            buffer.Clear();
            if (ops is List<AsyncOperation> list)
            {
                foreach (var op in list)
                {
                    if (op.Status is AsyncOperationStatus.Enabled)
                    {
                        buffer.Add(op);
                    }
                }
            }
            else
            {
                foreach (var op in ops)
                {
                    if (op.Status is AsyncOperationStatus.Enabled)
                    {
                        buffer.Add(op);
                    }
                }
            }

            return buffer;
        }

        /// <summary>
        /// Allocation-free equivalent of <c>ops.Any(op => op == target)</c>.
        /// </summary>
        internal static bool ContainsReference(IEnumerable<AsyncOperation> ops, AsyncOperation target)
        {
            if (ops is List<AsyncOperation> list)
            {
                foreach (var op in list)
                {
                    if (op == target)
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (var op in ops)
            {
                if (op == target)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Allocation-free equivalent of <c>ops.First(op => op.Equals(target))</c>.
        /// </summary>
        internal static AsyncOperation FirstEqual(List<AsyncOperation> ops, AsyncOperation target)
        {
            foreach (var op in ops)
            {
                if (op.Equals(target))
                {
                    return op;
                }
            }

            throw new System.InvalidOperationException("Sequence contains no matching element");
        }
    }
}
