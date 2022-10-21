// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// An abstract event handler declaration.
    /// </summary>
    internal abstract class EventHandlerDeclaration
    {
        internal abstract bool Inheritable { get; }
    }
}
