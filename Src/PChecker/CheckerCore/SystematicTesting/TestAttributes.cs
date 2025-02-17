﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PChecker.SystematicTesting
{
    /// <summary>
    /// Attribute for declaring the entry point to
    /// a program test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute for declaring the initialization
    /// method to be called before testing starts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestInitAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute for declaring a cleanup method to be
    /// called when all test schedules terminate.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestDisposeAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute for declaring a cleanup method to be
    /// called when each test schedule terminates.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestIterationDisposeAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute for declaring the factory method that creates
    /// the testing runtime. This is an advanced feature,
    /// only to be used for replacing the original testing
    /// runtime with an alternative implementation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class TestRuntimeCreateAttribute : Attribute
    {
    }
}