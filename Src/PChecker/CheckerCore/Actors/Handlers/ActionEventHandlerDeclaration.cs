// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PChecker.Actors.Handlers
{
    /// <summary>
    /// Defines an action event handler declaration.
    /// </summary>
    internal sealed class ActionEventHandlerDeclaration : EventHandlerDeclaration
    {
        /// <summary>
        /// Name of the action.
        /// </summary>
        public string Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionEventHandlerDeclaration"/> class.
        /// </summary>
        public ActionEventHandlerDeclaration(string actionName)
        {
            Name = actionName;
        }

        internal override bool Inheritable => true;
    }
}