// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PChecker.SmartSockets
{
    /// <summary>
    /// This class wraps the Socket class providing some useful semantics like FindServerAsync
    /// which looks for the UDP message broadcast by the SmartSocketServer. It also provides a
    /// useful SendReceiveAsync message that synchronously waits for a response from the server.
    /// It also supports serializing custom message objects via the DataContractSerializer using
    /// known types provided in your SmartSocketTypeResolver.
    /// </summary>
    public class SmartSocketClient : IDisposable
    {
        private readonly Socket Client;
        private readonly NetworkStream Stream;
        private readonly SmartSocketServer Server;
        private bool Closed;
        private readonly SmartSocketTypeResolver Resolver;
        private readonly DataContractSerializer Serializer;

        // Some standard message ids used for socket bookkeeping.
        private const string DisconnectMessageId = "DisconnectMessageId.3d9cd318-fcae-4a4f-ae63-34907be2700a";
        private const string ConnectedMessageId = "ConnectedMessageId.822280ed-26f5-4cdd-b45c-412e05d1005a";
        private const string MessageAck = "MessageAck.822280ed-26f5-4cdd-b45c-412e05d1005a";
        private const string ErrorMessageId = "ErrorMessageId.385ff3c1-84d8-491a-a8b3-e2a9e8f0e256";
        private const string OpenBackChannelMessageId = "OpenBackChannel.bd89da83-95c8-42e7-bf4e-6e7d0168754a";

        internal SmartSocketClient(SmartSocketServer server, Socket client, SmartSocketTypeResolver resolver)
        {
            Client = client;
            Stream = new NetworkStream(client);
            Server = server;
            Resolver = resolver;
            client.NoDelay = true;

            var settings = new DataContractSerializerSettings();
            settings.DataContractResolver = Resolver;
            settings.PreserveObjectReferences = true;
            Serializer = new DataContractSerializer(typeof(MessageWrapper), settings);
        }

        internal Socket Socket => Client;

        internal string Name { get; set; }

        /// <summary>
        /// Find a SmartSocketServer on the local network using UDP broadcast. This will block
        /// waiting for a server to respond or until you cancel using the CancellationToken.
        /// </summary>
        /// <returns>The connected client or null if task is cancelled.</returns>
        internal static async Task<SmartSocketClient> FindServerAsync(string serviceName, string clientName, SmartSocketTypeResolver resolver,
                                                                    CancellationToken token, string udpGroupAddress = "226.10.10.2", int udpGroupPort = 37992)
        {
            return await Task.Run(async () =>
            {
                var localHost = FindLocalHostName();
                if (localHost == null)
                {
                    return null;
                }
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var groupAddr = IPAddress.Parse(udpGroupAddress);
                        var remoteEP = new IPEndPoint(groupAddr, udpGroupPort);
                        var udpClient = new UdpClient(0);
                        var ms = new MemoryStream();
                        var writer = new BinaryWriter(ms);
                        writer.Write(serviceName.Length);
                        writer.Write(serviceName);
                        var bytes = ms.ToArray();
                        udpClient.Send(bytes, bytes.Length, remoteEP);

                        var receiveTaskSource = new CancellationTokenSource();
                        var receiveTask = udpClient.ReceiveAsync();
                        if (receiveTask.Wait(5000, receiveTaskSource.Token))
                        {
                            var result = receiveTask.Result;
                            var serverEP = result.RemoteEndPoint;
                            var buffer = result.Buffer;
                            var reader = new BinaryReader(new MemoryStream(buffer));
                            var len = reader.ReadInt32();
                            var addr = reader.ReadString();
                            var parts = addr.Split(':');
                            if (parts.Length == 2)
                            {
                                var a = IPAddress.Parse(parts[0]);
                                var client = await ConnectAsync(new IPEndPoint(a, int.Parse(parts[1])), clientName, resolver);
                                if (client != null)
                                {
                                    client.ServerName = serviceName;
                                    client.Name = localHost;
                                    return client;
                                }
                            }
                        }
                        else
                        {
                            receiveTaskSource.Cancel();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Something went wrong with Udp connection: " + ex.Message);
                    }
                }
                return null;
            });
        }

        /// <summary>
        /// Create another socket that will allow the server to send messages to the client any time.
        /// It is expected you will start a ReceiveAsync loop on this server object to process
        /// those messages.
        /// </summary>
        /// <param name="connectedHandler">An event handler to invoke when the server opens the back channel</param>
        /// <returns>New server object that will get one ClientConnected event when the remote server connects</returns>
        public async Task<SmartSocketServer> OpenBackChannel(EventHandler<SmartSocketClient> connectedHandler)
        {
            var ipe = (IPEndPoint)Socket.LocalEndPoint;
            // start a new server that does not use UDP.
            var server = SmartSocketServer.StartServer(Name, Resolver, ipe.Address.ToString(), null, 0);
            server.ClientConnected += connectedHandler;
            var port = server.EndPoint.Port;
            // tell the server we've opened another channel and pass the "port" number
            var response = await SendReceiveAsync(new SocketMessage(OpenBackChannelMessageId, Name + ":" + port));
            if (response.Id == ErrorMessageId)
            {
                throw new InvalidOperationException(response.Message);
            }

            return server;
        }

        internal static async Task<SmartSocketClient> ConnectAsync(IPEndPoint serverEP, string clientName, SmartSocketTypeResolver resolver)
        {
            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var connected = false;
            var src = new CancellationTokenSource();
            try
            {
                var task = Task.Run(() =>
                {
                    try
                    {
                        client.Connect(serverEP);
                        connected = true;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Connect exception: " + e.Message);
                    }
                }, src.Token);

                // give it 30 seconds to connect...
                if (!task.Wait(60000))
                {
                    src.Cancel();
                }
            }
            catch (TaskCanceledException)
            {
                // move on...
            }

            if (connected)
            {
                var result = new SmartSocketClient(null, client, resolver)
                {
                    Name = clientName,
                    ServerName = GetHostName(serverEP.Address)
                };
                var response = await result.SendReceiveAsync(new SocketMessage(ConnectedMessageId, clientName));
                return result;
            }

            return null;
        }

        private static string GetHostName(IPAddress addr)
        {
            try
            {
                var entry = Dns.GetHostEntry(addr);
                if (!string.IsNullOrEmpty(entry.HostName))
                {
                    return entry.HostName;
                }
            }
            catch (Exception)
            {
                // this can fail if machines are in different domains.
            }

            return addr.ToString();
        }

        internal static string FindLocalHostName()
        {
            try
            {
                var e = Dns.GetHostEntry(IPAddress.Loopback);
                return e.HostName;
            }
            catch (Exception)
            {
                // ignore failures to do with DNS lookups
            }

            return null;
        }

        internal static List<string> FindLocalIpAddresses()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.SupportsMulticast && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback
                     && ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    var props = ni.GetIPProperties();
                    if (props.IsDnsEnabled || props.IsDynamicDnsEnabled)
                    {
                        var e = Dns.GetHostEntry(IPAddress.Loopback);
                        var ipAddresses = new List<string>();
                        foreach (var addr in e.AddressList)
                        {
                            ipAddresses.Add(addr.ToString());
                        }

                        return ipAddresses;
                    }
                }
            }

            return null;
        }

        internal string ServerName { get; set; }

        internal bool IsConnected => !Closed;

        /// <summary>
        /// If OpenBackChannel is called, and the server supports it then this property will
        /// be defined when that channel is connected.
        /// </summary>
        public SmartSocketClient BackChannel { get; internal set; }

        /// <summary>
        /// This event is raised if a socket error is detected.
        /// </summary>
        public event EventHandler<Exception> Error;

        /// <summary>
        /// This even is raised if the socket is disconnected.
        /// </summary>
        public event EventHandler Disconnected;

        internal async void Close()
        {
            if (Closed)
            {
                return;
            }

            try
            {
                await SendAsync(new SocketMessage(DisconnectMessageId, Name));

                Closed = true;

                using (Client)
                {
                    Client.Close();
                }
            }
            catch (Exception)
            {
                // ignore failures on close.
            }
        }

        private void OnError(Exception ex)
        {
            var inner = ex;
            while (inner != null)
            {
                var se = inner as SocketException;
                if (se != null && se.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // we're toast!
                    if (Server != null)
                    {
                        Server.RemoveClient(this);
                    }

                    Closed = true;
                }

                if (ex is ObjectDisposedException)
                {
                    Closed = true;
                }

                inner = inner.InnerException;
            }

            if (Error != null)
            {
                Error(this, ex);
            }
        }

        [DataContract]
        internal class MessageWrapper
        {
            [DataMember]
            public object Message { get; set; }
        }

        /// <summary>
        /// Send a message back to the client.
        /// </summary>
        /// <returns>The response message</returns>
        internal async Task<SocketMessage> SendReceiveAsync(SocketMessage msg)
        {
            if (Closed)
            {
                throw new SocketException((int)SocketError.NotConnected);
            }

            // must serialize this send/response sequence, cannot interleave them!
            using (await GetSendLock())
            {
                return await Task.Run(async () =>
                {
                    try
                    {
                        await InternalSendAsync(msg);

                        var response = await InternalReceiveAsync();
                        return response;
                    }
                    catch (Exception ex)
                    {
                        // is the socket dead?
                        OnError(ex);
                    }
                    return null;
                });
            }
        }

        /// <summary>
        /// Send a message and do not wait for a response.
        /// </summary>
        /// <returns>The response message</returns>
        internal async Task SendAsync(SocketMessage msg)
        {
            // must serialize this send/response sequence, cannot interleave them!
            using (await GetSendLock())
            {
                await InternalSendAsync(msg);
            }
        }

        internal async Task InternalSendAsync(SocketMessage msg)
        {
            if (Closed)
            {
                throw new SocketException((int)SocketError.NotConnected);
            }

            // get the buffer containing the serialized message.
            await Task.Run(() =>
            {
                try
                {
                    // Wrap the message in a MessageWrapper and send it
                    var ms = new MemoryStream();
                    Serializer.WriteObject(ms, new MessageWrapper() { Message = msg });

                    var buffer = ms.ToArray();

                    var streamWriter = new BinaryWriter(Stream, Encoding.UTF8, true);
                    streamWriter.Write(buffer.Length);
                    streamWriter.Write(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    // is the socket dead?
                    OnError(ex);
                }
            });
        }

        private void OnClosed()
        {
            Closed = true;
            if (Disconnected != null)
            {
                Disconnected(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Receive one message from the socket. This call blocks until a message has arrived.
        /// </summary>
        internal async Task<SocketMessage> ReceiveAsync()
        {
            using (await GetSendLock())
            {
                return await InternalReceiveAsync();
            }
        }

        private async Task<SocketMessage> InternalReceiveAsync()
        {
            if (Closed)
            {
                throw new SocketException((int)SocketError.NotConnected);
            }

            SocketMessage msg = null;
            try
            {
                using (var streamReader = new BinaryReader(Stream, Encoding.UTF8, true))
                {
                    var len = streamReader.ReadInt32();
                    var block = streamReader.ReadBytes(len);

                    object result = null;
                    if (len != block.Length)
                    {
                        // Can happen if the process at the other side of this socket was terminated.
                        // If we don't have the exact requested bytes then we cannot deserialize it.
                    }
                    else
                    {
                        try
                        {
                            result = Serializer.ReadObject(new MemoryStream(block));
                        }
                        catch (Exception)
                        {
                            // This can also happen when process on other side is terminated, the last network
                            // packet can be scrambled.  This is usually just the DisconnectMessageId which is
                            // ignorable.
                        }
                    }

                    var wrapper = result as MessageWrapper;
                    if (wrapper != null && wrapper.Message is SocketMessage)
                    {
                        msg = (SocketMessage)wrapper.Message;
                        if (msg.Id == DisconnectMessageId)
                        {
                            // client is politely saying good bye...
                            OnClosed();
                        }
                        else if (msg.Id == ConnectedMessageId)
                        {
                            // must send an acknowledgement of the connect message
                            Name = msg.Sender;
                            await SendAsync(new SocketMessage(MessageAck, Name));
                        }
                        else if (msg.Id == OpenBackChannelMessageId && Server != null)
                        {
                            // client is requesting a back channel.
                            await HandleBackchannelRequest(msg);
                        }
                    }
                }
            }
            catch (EndOfStreamException)
            {
                OnClosed();
            }
            catch (IOException ioe)
            {
                var se = ioe.InnerException as SocketException;
                if (se.SocketErrorCode == SocketError.ConnectionReset)
                {
                    OnClosed();
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }

            return msg;
        }

        private async Task HandleBackchannelRequest(SocketMessage msg)
        {
            var parts = msg.Sender.Split(':');
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[1], out var port))
                {
                    var rc = await Server.OpenBackChannel(this, port);
                    if (rc)
                    {
                        await SendAsync(new SocketMessage(MessageAck, Name));
                        return;
                    }
                    else
                    {
                        await SendAsync(new SocketMessage(ErrorMessageId, Name)
                        {
                            Message = "Server is not expecting a back channel"
                        });
                        return;
                    }
                }
            }

            await SendAsync(new SocketMessage(ErrorMessageId, Name)
            {
                Message = "Valid port number was not found in backchannel message"
            });
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Close();

            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        ~SmartSocketClient()
        {
            Close();
        }

        private readonly SendLock Lock = new SendLock();

        private async Task<IDisposable> GetSendLock()
        {
            while (Lock.Locked)
            {
                await Task.Delay(100);
                lock (Lock)
                {
                    if (!Lock.Locked)
                    {
                        Lock.Locked = true;
                        return new ReleaseLock(Lock);
                    }
                }
            }

            return null;
        }

        internal class SendLock
        {
            public bool Locked { get; set; }
        }

        internal class ReleaseLock : IDisposable
        {
            private readonly SendLock Lock;

            public ReleaseLock(SendLock sendLock)
            {
                Lock = sendLock;
            }

            public void Dispose()
            {
                lock (Lock)
                {
                    Lock.Locked = false;
                }
            }
        }
    }
}
