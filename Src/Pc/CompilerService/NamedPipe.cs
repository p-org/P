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
    /// This class sets up a named pipe for bidirectional communication with another process.
    /// </summary>
    public class NamedPipe : IDisposable
    {
        string pipeName;
        bool server;
        bool closed;
        PipeStream clientPipe;
        int maxServerThreads;

        /// <summary>
        /// Construct a new NamedPipeReader for reading & writing messages to/from the pipe.
        /// </summary>
        /// <param name="pipeName">The pipe name must be unique across the Windows system</param>
        public NamedPipe(string pipeName)
        {
            this.pipeName = pipeName;
            this.closed = true;
        }

        private NamedPipe(string pipeName, PipeStream pipe) : this(pipeName)
        {
            this.clientPipe = pipe;
            this.closed = false;
        }

        public bool IsClosed { get { return this.closed || clientPipe == null || !clientPipe.IsConnected; } }

        /// <summary>
        /// Start the named pipe server thread that will accept clients.</param>
        /// </summary>
        public void StartServer(int maxThreads)
        {
            this.maxServerThreads = maxThreads;
            this.closed = false;
            this.server = true;
            Task.Factory.StartNew(ServerThread);
        }

        public bool Connect()
        {
            try
            {
                if (server)
                {
                    throw new Exception("Cannot call Connect on the server pipe");
                }
                else
                {
                    if (clientPipe == null)
                    {
                        clientPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
                        ((NamedPipeClientStream)clientPipe).Connect(2000); // see if service is running...
                    }
                }
                closed = false;
            }
            catch (Exception ex)
            {
                DebugWriteLine(ex.Message);
                using (clientPipe)
                {
                    clientPipe = null;
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// This event is raised when a new client connects, and a pipe is provided for talking to that client.
        /// </summary>
        public event EventHandler<NamedPipe> ClientConnected;

        /// <summary>
        /// Close the pipe.
        /// </summary>
        public void Close()
        {
            if (!closed)
            {
                using (clientPipe)
                {
                    clientPipe = null;
                }
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

        private void ClientThread(PipeStream pipe)
        {
            try
            {
                NamedPipe client = new NamedPipe(this.pipeName, pipe);

                if (ClientConnected != null)
                {
                    ClientConnected(this, client);
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
                        closed = true;
                    }
                    else if (hr == 0x8007006d)
                    {
                        // the pipe has ended, so client went away, we can go back to listening.
                        DebugWriteLine("The pipe has ended: " + this.pipeName);
                    }
                }
                else
                {
                    // on standby, waiting for test to fire up...
                    Thread.Sleep(100);
                }
            }
        }

        private void ServerThread()
        {
            while (!closed)
            {
                try
                {
                    NamedPipeServerStream pipe = null;
                    pipe = new NamedPipeServerStream(pipeName,
                                    PipeDirection.InOut, maxServerThreads, PipeTransmissionMode.Byte,
                                    PipeOptions.Asynchronous, 8192, 8192);
                    ((NamedPipeServerStream)pipe).WaitForConnection();                    
                    Task.Run(() => { ClientThread(pipe); });
                }
                catch (Exception ex)
                {
                    DebugWriteLine("ServerException: " + ex.Message);
                    Thread.Sleep(1000);
                }
            }
        }

        private void DebugWriteLine(string msg)
        {
            Debug.WriteLine("(" + System.Threading.Thread.CurrentThread.ManagedThreadId + ") " + msg);
        }

        /// <summary>
        /// Read a string from the pipe. We assume all strings are unicode.
        /// Only MaxBytes characters will be read, meaning the max string length
        /// is MaxBytes / BytesPerChar.
        /// </summary>
        public string ReadMessage()
        {
            // read the 4 byte length of the string
            byte[] buffer = new byte[4];
            int len = clientPipe.Read(buffer, 0, 4);
            if (len == 4)
            {
                int stringLength = buffer[0] + (buffer[1] << 8) + (buffer[2] << 16) + (buffer[3] << 24);
                buffer = new byte[stringLength];

                len = clientPipe.Read(buffer, 0, stringLength);
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
                if (clientPipe != null && clientPipe.IsConnected)
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
                    clientPipe.Write(header, 0, 4);
                    clientPipe.Write(bytes, 0, len);
                    clientPipe.Flush();
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

        internal void Flush()
        {
            if (clientPipe != null)
            {
                clientPipe.Flush();
            }
        }

        /// <summary>
        /// Called when the reader is being disposed
        /// </summary>
        /// <param name="disposing">Whether Dispose was called</param>
        protected virtual void Dispose(bool disposing)
        {
            Close();
            using (clientPipe)
            {
                clientPipe = null;
            }
        }
    }
}
