// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace PChecker.StateMachines
{
    /// <summary>
    /// Factory for creating state machine instances.
    /// </summary>
    internal static class StateMachineFactory
    {
        /// <summary>
        /// Cache storing state machine constructors.
        /// </summary>
        private static readonly Dictionary<Type, Func<StateMachine>> StateMachineConstructorCache =
            new Dictionary<Type, Func<StateMachine>>();

        /// <summary>
        /// Creates a new <see cref="StateMachine"/> instance of the specified type.
        /// </summary>
        /// <param name="type">The type of the state machines.</param>
        /// <returns>The created state machine instance.</returns>
        public static StateMachine Create(Type type)
        {
            Func<StateMachine> constructor = null;
            lock (StateMachineConstructorCache)
            {
                if (!StateMachineConstructorCache.TryGetValue(type, out constructor))
                {
                    var constructorInfo = type.GetConstructor(Type.EmptyTypes);
                    if (constructorInfo == null)
                    {
                        throw new Exception("Could not find empty constructor for type " + type.FullName);
                    }

                    constructor = Expression.Lambda<Func<StateMachine>>(
                        Expression.New(constructorInfo)).Compile();
                    StateMachineConstructorCache.Add(type, constructor);
                }
            }

            return constructor();
        }
    }
}