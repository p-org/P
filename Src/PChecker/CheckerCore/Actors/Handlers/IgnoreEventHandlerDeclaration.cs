// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PChecker.Actors.Handlers
{
    /// <summary>
    /// Defines an ignore event handler declaration.
    /// </summary>
    internal sealed class IgnoreEventHandlerDeclaration : EventHandlerDeclaration
    {
        internal override bool Inheritable => true;
    }
}