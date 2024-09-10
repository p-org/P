﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace PChecker.Exceptions
{
    /// <summary>
    /// An exception that is thrown by the ControlledRuntime.
    /// </summary>
    [Serializable]
    [DebuggerStepThrough]
    public class RuntimeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeException"/> class.
        /// </summary>
        internal RuntimeException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeException"/> class.
        /// </summary>
        internal RuntimeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeException"/> class.
        /// </summary>
        internal RuntimeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}