// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PChecker.Runtime
{
    /// <summary>
    /// Handles the <see cref="ICoyoteRuntime.OnFailure"/> event.
    /// </summary>
    public delegate void OnFailureHandler(Exception ex);
}