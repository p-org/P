// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Abstract class representing an event.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/learn/programming-models/actors/overview">Programming model: asynchronous actors</see> for more information.
    /// </remarks>
    [DataContract]
    public abstract class Event
    {
    }
}
