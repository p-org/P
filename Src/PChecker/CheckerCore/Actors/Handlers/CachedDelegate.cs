// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Threading.Tasks;
using PChecker.Actors.Events;

namespace PChecker.Actors.Handlers
{
    /// <summary>
    /// An actor delegate that has been cached to optimize performance of invocations.
    /// </summary>
    internal class CachedDelegate
    {
        internal readonly MethodInfo MethodInfo;
        internal readonly Delegate Handler;
        internal readonly bool IsAsync;

        internal CachedDelegate(MethodInfo method, object caller)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 1 && method.ReturnType == typeof(void))
            {
                Handler = Delegate.CreateDelegate(typeof(Action<Event>), caller, method);
                IsAsync = false;
            }
            else if (method.ReturnType == typeof(void))
            {
                Handler = Delegate.CreateDelegate(typeof(Action), caller, method);
                IsAsync = false;
            }
            else if (parameters.Length == 1 && method.ReturnType == typeof(Task))
            {
                Handler = Delegate.CreateDelegate(typeof(Func<Event, Task>), caller, method);
                IsAsync = true;
            }
            else if (method.ReturnType == typeof(Task))
            {
                Handler = Delegate.CreateDelegate(typeof(Func<Task>), caller, method);
                IsAsync = true;
            }
            else
            {
                throw new InvalidOperationException($"Trying to cache invalid action delegate '{method.Name}'.");
            }

            MethodInfo = method;
        }
    }
}