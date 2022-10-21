// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Factory for creating actor instances.
    /// </summary>
    internal static class ActorFactory
    {
        /// <summary>
        /// Cache storing actors constructors.
        /// </summary>
        private static readonly Dictionary<Type, Func<Actor>> ActorConstructorCache =
            new Dictionary<Type, Func<Actor>>();

        /// <summary>
        /// Creates a new <see cref="Actor"/> instance of the specified type.
        /// </summary>
        /// <param name="type">The type of the actors.</param>
        /// <returns>The created actor instance.</returns>
        public static Actor Create(Type type)
        {
            Func<Actor> constructor = null;
            lock (ActorConstructorCache)
            {
                if (!ActorConstructorCache.TryGetValue(type, out constructor))
                {
                    var constructorInfo = type.GetConstructor(Type.EmptyTypes);
                    if (constructorInfo == null)
                    {
                        throw new Exception("Could not find empty constructor for type " + type.FullName);
                    }

                    constructor = Expression.Lambda<Func<Actor>>(
                        Expression.New(constructorInfo)).Compile();
                    ActorConstructorCache.Add(type, constructor);
                }
            }

            return constructor();
        }
    }
}
