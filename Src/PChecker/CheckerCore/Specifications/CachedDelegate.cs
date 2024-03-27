// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using PChecker.Actors.Events;

namespace PChecker.Specifications.Monitors
{
    /// <summary>
    /// A monitor delegate that has been cached to optimize performance of invocations.
    /// </summary>
    internal class CachedDelegate
    {
        internal readonly MethodInfo MethodInfo;
        internal readonly Delegate Handler;

        internal CachedDelegate(MethodInfo method, object caller)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 1 && method.ReturnType == typeof(void))
            {
                Handler = Delegate.CreateDelegate(typeof(Action<Event>), caller, method);
            }
            else if (method.ReturnType == typeof(void))
            {
                Handler = Delegate.CreateDelegate(typeof(Action), caller, method);
            }
            else if (parameters.Length == 1 && method.ReturnType == typeof(Monitor.Transition))
            {
                Handler = Delegate.CreateDelegate(typeof(Func<Event, Monitor.Transition>), caller, method);
            }
            else if (method.ReturnType == typeof(Monitor.Transition))
            {
                Handler = Delegate.CreateDelegate(typeof(Func<Monitor.Transition>), caller, method);
            }
            else
            {
                throw new InvalidOperationException($"Trying to cache invalid action delegate '{method.Name}'.");
            }

            MethodInfo = method;
        }
    }
}