using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace Microsoft.Pc
{
    /// <summary>
    /// This event args is used to communicate messages read from the pipe.
    /// </summary>
    public class PipeMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Construct a new PipeMessageEventArgs with the given message.
        /// This is used by the MessageArrived event on the NamedPipeReader
        /// </summary>
        /// <param name="message">The message that was read by the NamedPipeReader</param>
        public PipeMessageEventArgs(string message)
        {
            Message = message;
        }

        /// <summary>
        /// The message that was read by the NamedPipeReader
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// This class sets up a named pipe for bidirectional communication with another process.
    /// </summary>
    public class NamedPipe : IDisposable
    {
        string pipeName;
        bool server;
        bool closed;
        PipeStream pipe;

        /// <summary>
        /// Construct a new NamedPipeReader for reading messages from the pipe.
        /// Use the MessageArrived event to get the messages.  
        /// </summary>
        /// <param name="pipeName">The pipe name must be unique across the Windows system</param>
        /// <param name="server">Whether this is the server or the client (true=server)</param>
        public NamedPipe(string pipeName, bool server)
        {
            this.pipeName = pipeName;
            this.server = server;
            this.closed = true;
        }

        public bool IsClosed { get { return this.closed; } }

        public bool Connect()
        {
            try
            {
                if (server)
                {
                    if (pipe == null)
                    {
                        pipe = new NamedPipeServerStream(pipeName,
                                    PipeDirection.In, 1, PipeTransmissionMode.Byte,
                                    PipeOptions.Asynchronous, 8192, 8192);
                    }
                    Task.Factory.StartNew(ReadPipe);
                }
                else
                {
                    if (pipe == null)
                    {
                        pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
                        ((NamedPipeClientStream)pipe).Connect(2000); // give server 5 seconds to boot up...
                    }
                }
                closed = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                pipe = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// This event is raised (on a background thread) whenever a message has been
        /// received 
        /// </summary>
        public event EventHandler<PipeMessageEventArgs> MessageArrived;

        /// <summary>
        /// Close the pipe.
        /// </summary>
        public void Close()
        {
            if (!closed && pipe != null)
            {
                pipe.Close();
                pipe = null;
            }
            closed = true;
        }

        /// <summary>
        /// This event is raised whenever the model is paused.
        /// This event is raised from a background thread.
        /// </summary>
        public event EventHandler Break;

        private void OnBreak()
        {
            if (Break != null)
            {
                Break(this, EventArgs.Empty);
            }
        }

        private void ReadPipe()
        {
            while (!closed)
            {
                try
                {
                    if (server && !pipe.IsConnected)
                    {
                        ((NamedPipeServerStream)pipe).WaitForConnection();
                    }
                    string msg = ReadMessage();
                    if (!string.IsNullOrEmpty(msg))
                    {
                        OnMessageArrived(msg);                        
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception e)
                {
                    IOException io = e as IOException;
                    if (io != null)
                    {
                        uint hr = (uint)io.HResult;
                        if (hr == 0x800700e7)
                        {
                            // multiple instances, all pipe instances are busy, so go to deep sleep
                            // waiting for other instance to go away.
                            Thread.Sleep(1000);
                        }
                        else if (hr == 0x80131620)
                        {
                            // Pipe is broken, need to recreate it.
                            using (pipe)
                            {
                                pipe = null;
                            }
                            closed = true;
                        }
                    }
                    else
                    {
                        // on standby, waiting for test to fire up...
                        Thread.Sleep(100);
                    }
                }
            }
        }

        public void Disconnect()
        {
            if (server)
            {
                ((NamedPipeServerStream)pipe).Disconnect();
            }
        }

        private void OnMessageArrived(string msg)
        {
            if (MessageArrived != null)
            {
                MessageArrived(this, new PipeMessageEventArgs(msg));
            }
        }

        /// <summary>
        /// Read a string from the pipe. We assume all strings are unicode.
        /// Only MaxBytes characters will be read, meaning the max string length
        /// is MaxBytes / BytesPerChar.
        /// </summary>
        private string ReadMessage()
        {
            // read the 4 byte length of the string
            byte[] buffer = new byte[4];
            int len = pipe.Read(buffer, 0, 4);
            if (len == 4)
            {
                int stringLength = buffer[0] + (buffer[1] << 8) + (buffer[2] << 16) + (buffer[3] << 24);
                buffer = new byte[stringLength];

                len = pipe.Read(buffer, 0, stringLength);
                if (len == stringLength)
                {
                    return Encoding.Unicode.GetString(buffer, 0, len).Trim('\0');
                }
            }
            return null;
        }



        /// <summary>
        /// Send a string down the pipe using Unicode encoding.
        /// Only the first MaxBytes will be sent, meaning the max
        /// string length is MaxBytes / BytesPerChar.
        /// </summary>
        public bool WriteMessage(string message)
        {
            try
            {
                if (pipe != null && pipe.IsConnected)
                {
                    // Don't use a StreamWriter here because it has buffering which messes up the synchronization
                    // of messages across the pipe.
                    byte[] bytes = Encoding.Unicode.GetBytes(message);
                    int len = bytes.Length;
                    byte[] header = new byte[4];
                    header[0] = (byte)(len & 0xff);
                    header[1] = (byte)((len >> 8) & 0xff);
                    header[2] = (byte)((len >> 16) & 0xff);
                    header[3] = (byte)((len >> 24) & 0xff);
                    pipe.Write(header, 0, 4);
                    pipe.Write(bytes, 0, len);
                    pipe.Flush();
                }
            }
            catch (IOException)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Dispose the reader
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Destructor for the NamedPipeReader
        /// </summary>
        ~NamedPipe()
        {
            Dispose(false);
        }

        /// <summary>
        /// Called when the reader is being disposed
        /// </summary>
        /// <param name="disposing">Whether Dispose was called</param>
        protected virtual void Dispose(bool disposing)
        {
            Close();
            using (pipe)
            {
                pipe = null;
            }
        }
    }
}
