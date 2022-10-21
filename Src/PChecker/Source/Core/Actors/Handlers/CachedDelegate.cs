// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Actors
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
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 1 && method.ReturnType == typeof(void))
            {
                this.Handler = Delegate.CreateDelegate(typeof(Action<Event>), caller, method);
                this.IsAsync = false;
            }
            else if (method.ReturnType == typeof(void))
            {
                this.Handler = Delegate.CreateDelegate(typeof(Action), caller, method);
                this.IsAsync = false;
            }
            else if (parameters.Length == 1 && method.ReturnType == typeof(Task))
            {
                this.Handler = Delegate.CreateDelegate(typeof(Func<Event, Task>), caller, method);
                this.IsAsync = true;
            }
            else if (method.ReturnType == typeof(Task))
            {
                this.Handler = Delegate.CreateDelegate(typeof(Func<Task>), caller, method);
                this.IsAsync = true;
            }
            else
            {
                throw new InvalidOperationException($"Trying to cache invalid action delegate '{method.Name}'.");
            }

            this.MethodInfo = method;
        }
    }
}
