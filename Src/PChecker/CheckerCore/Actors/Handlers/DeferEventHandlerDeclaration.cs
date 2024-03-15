// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PChecker.Actors.Handlers
{
    /// <summary>
    /// Defines a defer event handler declaration.
    /// </summary>
    internal sealed class DeferEventHandlerDeclaration : EventHandlerDeclaration
    {
        internal override bool Inheritable => true;
    }
}