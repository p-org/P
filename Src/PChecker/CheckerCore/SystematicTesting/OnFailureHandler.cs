// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PChecker.SystematicTesting
{
    /// <summary>
    /// Handles the <see cref="ControlledRuntime.OnFailure"/> event.
    /// </summary>
    public delegate void OnFailureHandler(Exception ex);
}