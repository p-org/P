﻿using System;

namespace PChecker.Runtime.Exceptions
{
    public class PUnreachableCodeException : Exception
    {
        public PUnreachableCodeException()
        {
        }

        public PUnreachableCodeException(string message) : base(message)
        {
        }

        public PUnreachableCodeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}