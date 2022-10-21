// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// Thread safe logger that writes text to an in-memory buffer.
    /// The buffered text can be extracted using the ToString() method.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/learn/core/logging" >Logging</see> for more information.
    /// </remarks>
    public sealed class InMemoryLogger : TextWriter
    {
        /// <summary>
        /// Underlying string builder.
        /// </summary>
        private readonly StringBuilder Builder;

        /// <summary>
        /// Serializes access to the string writer.
        /// </summary>
        private readonly object Lock;

        /// <summary>
        /// When overridden in a derived class, returns the character encoding in which the
        /// output is written.
        /// </summary>
        public override Encoding Encoding => Encoding.Unicode;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryLogger"/> class.
        /// </summary>
        public InMemoryLogger()
        {
            this.Builder = new StringBuilder();
            this.Lock = new object();
        }

        /// <summary>
        /// Writes the specified Unicode character value to the standard output stream.
        /// </summary>
        /// <param name="value">The Unicode character.</param>
        public override void Write(char value)
        {
            try
            {
                lock (this.Lock)
                {
                    this.Builder.Append(value);
                }
            }
            catch (ObjectDisposedException)
            {
                // The writer was disposed.
            }
        }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        public override void Write(string value)
        {
            try
            {
                lock (this.Lock)
                {
                    this.Builder.Append(value);
                }
            }
            catch (ObjectDisposedException)
            {
                // The writer was disposed.
            }
        }

        /// <summary>
        /// Writes a string followed by a line terminator to the text string or stream.
        /// </summary>
        /// <param name="value">The string to write.</param>
        public override void WriteLine(string value)
        {
            try
            {
                lock (this.Lock)
                {
                    this.Builder.AppendLine(value);
                }
            }
            catch (ObjectDisposedException)
            {
                // The writer was disposed.
            }
        }

        /// <summary>
        /// Returns the logged text as a string.
        /// </summary>
        public override string ToString()
        {
            lock (this.Lock)
            {
                return this.Builder.ToString();
            }
        }
    }
}
