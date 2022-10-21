// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Globalization;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Utility class for resolving names.
    /// </summary>
    internal static class NameResolver
    {
        /// <summary>
        /// Cache of state names.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, string> StateNamesCache =
            new ConcurrentDictionary<Type, string>();

        /// <summary>
        /// Returns the qualified (i.e. <see cref="StateMachine.StateGroup"/>) name of the specified
        /// state machine or monitor state, or the empty string if there is no such name.
        /// </summary>
        internal static string GetQualifiedStateName(Type state)
        {
            if (state is null)
            {
                return string.Empty;
            }

            if (!StateNamesCache.TryGetValue(state, out string name))
            {
                name = state.Name;

                var nextState = state;
                while (nextState.DeclaringType != null)
                {
                    if (!nextState.DeclaringType.IsSubclassOf(typeof(StateMachine.StateGroup)))
                    {
                        break;
                    }

                    name = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", nextState.DeclaringType.Name, name);
                    nextState = nextState.DeclaringType;
                }

                StateNamesCache.GetOrAdd(state, name);
            }

            return name;
        }

        /// <summary>
        /// Returns the state name to be used for logging purposes.
        /// </summary>
        internal static string GetStateNameForLogging(Type state) => state is null ? "None" : GetQualifiedStateName(state);
    }
}
