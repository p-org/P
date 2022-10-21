// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Microsoft.Coyote.SmartSockets
{
    /// <summary>
    /// This class sets up a UDP broadcaster so clients on the same network can find the server by
    /// a given string name, no fussing about with ip addresses and ports.It then listens for
    /// new clients to connect and spins off ClientConnected messages so your app can process the
    /// server side of each conversation.Your application server then can handle any number of
    /// clients at the same time, each client will have their own SmartSocketClient on different ports.
    /// If the client goes away, the ClientDisconnected event is raised so the server can cleanup.
    /// </summary>
    public class SmartSocketServer
    {
        private bool Stopped;
        private Socket Listener;
        private readonly string ServiceName;
        private readonly IPEndPoint IpAddress;
        private readonly List<SmartSocketClient> Clients = new List<SmartSocketClient>();
        private readonly SmartSocketTypeResolver Resolver;
        private UdpClient UdpListener;
        private SocketAsyncEventArgs acceptArgs;

        /// <summary>
        /// Address for UDP group.
        /// </summary>
        public IPAddress GroupAddress { get; internal set; }

        /// <summary>
        /// Port used for UDP broadcasts.
        /// </summary>
        public int GroupPort { get; internal set; }

        /// <summary>
        /// The end point we are listening on (valid after calling StartServer)
        /// </summary>
        public IPEndPoint EndPoint { get; set; }

        /// <summary>
        /// Raised when a new client is connected
        /// </summary>
        public event EventHandler<SmartSocketClient> ClientConnected;

        /// <summary>
        /// Raised when the given client disconnects
        /// </summary>
        public event EventHandler<SmartSocketClient> ClientDisconnected;

        /// <summary>
        /// Raised when client requests a back channel for server to communicate independently with the client
        /// The given SmartSocketClient will have a BackChannel property set to a new SmartSocketClient that
        /// the server can use to send messages to the client.
        /// </summary>
        public event EventHandler<SmartSocketClient> BackChannelOpened;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartSocketServer"/> class.
        /// Construct a new SmartSocketServer.
        /// </summary>
        /// <param name="name">The name the client will check in UDP broadcasts to make sure it is connecting to the right server</param>
        /// <param name="resolver">A way of providing custom Message types for serialization</param>
        /// <param name="ipAddress">An optional ipAddress so you can decide which network interface to use</param>
        private SmartSocketServer(string name, SmartSocketTypeResolver resolver, string ipAddress = "127.0.0.1:0",
            string udpGroupAddress = "226.10.10.2", int udpGroupPort = 37992)
        {
            this.ServiceName = name;
            this.Resolver = resolver;
            if (ipAddress.Contains(':'))
            {
                string[] parts = ipAddress.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out int port))
                {
                    this.IpAddress = new IPEndPoint(IPAddress.Parse(parts[0]), port);
                }
                else
                {
                    throw new ArgumentException("ipAddress is not a valid format");
                }
            }
            else
            {
                this.IpAddress = new IPEndPoint(IPAddress.Parse(ipAddress), 0);
            }

            if (!string.IsNullOrEmpty(udpGroupAddress))
            {
                this.GroupAddress = IPAddress.Parse(udpGroupAddress);
                this.GroupPort = udpGroupPort;
            }
        }

        /// <summary>
        /// Start a new server that listens for connections from anyone.
        /// </summary>
        /// <param name="name">The unique name of the server</param>
        /// <param name="resolver">For resolving custom message types received from the client</param>
        /// <param name="ipAddress">Determines which local network interface to use</param>
        /// <param name="udpGroupAddress">Optional request to setup UDP listener, pass null if you don't want that</param>
        /// <param name="udpGroupPort">Optional port required if you provide udpGroupAddress</param>
        /// <returns>Returns the new server object</returns>
        public static SmartSocketServer StartServer(string name, SmartSocketTypeResolver resolver, string ipAddress,
            string udpGroupAddress = "226.10.10.2", int udpGroupPort = 37992)
        {
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = "127.0.0.1:0";
            }

            SmartSocketServer server = new SmartSocketServer(name, resolver, ipAddress, udpGroupAddress, udpGroupPort);
            server.StartListening();
            return server;
        }

        /// <summary>
        /// Start listening for connections from anyone.
        /// </summary>
        private void StartListening()
        {
            this.Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = this.IpAddress;
            this.Listener.Bind(ep);

            IPEndPoint ip = this.Listener.LocalEndPoint as IPEndPoint;
            this.EndPoint = ip;
            this.Listener.Listen(10);

            // now start a background thread to process incoming requests.
            Task.Run(this.Run);

            if (this.GroupAddress != null)
            {
                // Start the UDP listener thread
                Task.Run(this.UdpListenerThread);
            }
        }

        private void UdpListenerThread()
        {
            var localHost = SmartSocketClient.FindLocalHostName();
            List<string> addresses = SmartSocketClient.FindLocalIpAddresses();
            if (localHost == null || addresses.Count == 0)
            {
                return; // no network.
            }

            IPEndPoint remoteEP = new IPEndPoint(this.GroupAddress, this.GroupPort);
            this.UdpListener = new UdpClient(this.GroupPort);
            this.UdpListener.JoinMulticastGroup(this.GroupAddress);
            while (true)
            {
                byte[] data = this.UdpListener.Receive(ref remoteEP);
                if (data != null)
                {
                    BinaryReader reader = new BinaryReader(new MemoryStream(data));
                    int len = reader.ReadInt32();
                    string msg = reader.ReadString();
                    if (msg == this.ServiceName)
                    {
                        // send response back with info on how to connect to this server.
                        IPEndPoint localEp = (IPEndPoint)this.Listener.LocalEndPoint;
                        string addr = localEp.ToString();
                        MemoryStream ms = new MemoryStream();
                        BinaryWriter writer = new BinaryWriter(ms);
                        writer.Write(addr.Length);
                        writer.Write(addr);
                        writer.Flush();
                        byte[] buffer = ms.ToArray();
                        this.UdpListener.Send(buffer, buffer.Length, remoteEP);
                    }
                }
            }
        }

        /// <summary>
        /// Send a message to all connected clients.
        /// </summary>
        /// <param name="message">The message to send</param>
        public async Task BroadcastAsync(SocketMessage message)
        {
            SmartSocketClient[] snapshot = null;
            lock (this.Clients)
            {
                snapshot = this.Clients.ToArray();
            }

            foreach (var client in snapshot)
            {
                await client.SendAsync(message);
            }
        }

        /// <summary>
        /// Call this method on a background thread to listen to our port.
        /// </summary>
        internal void Run()
        {
            if (this.acceptArgs == null)
            {
                this.acceptArgs = new SocketAsyncEventArgs();
                this.acceptArgs.Completed += this.OnAcceptComplete;
            }

            if (!this.Stopped)
            {
                try
                {
                    this.Listener.AcceptAsync(this.acceptArgs);
                }
                catch (Exception)
                {
                    // listener was probably closed then, which means we've probably been stopped.
                    Debug.WriteLine("Listener is gone");
                }
            }
        }

        private void OnAcceptComplete(object sender, SocketAsyncEventArgs e)
        {
            if (this.acceptArgs == e)
            {
                this.acceptArgs = null;
                Socket client = e.AcceptSocket;
                this.OnAccept(client);
                this.Run();
            }
        }

        private void OnAccept(Socket client)
        {
            IPEndPoint ep1 = client.RemoteEndPoint as IPEndPoint;
            SmartSocketClient proxy = new SmartSocketClient(this, client, this.Resolver)
            {
                Name = ep1.ToString(),
                ServerName = SmartSocketClient.FindLocalHostName()
            };

            proxy.Disconnected += this.OnClientDisconnected;

            SmartSocketClient[] snapshot = null;

            lock (this.Clients)
            {
                snapshot = this.Clients.ToArray();
            }

            foreach (SmartSocketClient s in snapshot)
            {
                IPEndPoint ep2 = s.Socket.RemoteEndPoint as IPEndPoint;
                if (ep1 == ep2)
                {
                    // can only have one client using this end point.
                    this.RemoveClient(s);
                }
            }

            lock (this.Clients)
            {
                this.Clients.Add(proxy);
            }

            if (this.ClientConnected != null)
            {
                this.ClientConnected(this, proxy);
            }
        }

        private void OnClientDisconnected(object sender, EventArgs e)
        {
            SmartSocketClient client = (SmartSocketClient)sender;
            this.RemoveClient(client);
        }

        internal void RemoveClient(SmartSocketClient client)
        {
            bool found = false;
            lock (this.Clients)
            {
                found = this.Clients.Contains(client);
                this.Clients.Remove(client);
            }

            if (found && this.ClientDisconnected != null)
            {
                this.ClientDisconnected(this, client);
            }
        }

        /// <summary>
        /// Call this method to stop the background thread, it is good to do this before your app shuts down.
        /// This will also send a Disconnect message to all the clients so they know the server is gone.
        /// </summary>
        public void Stop()
        {
            this.Stopped = true;
            using (this.Listener)
            {
                try
                {
                    if (this.acceptArgs != null)
                    {
                        this.acceptArgs.Dispose();
                        this.acceptArgs = null;
                    }
                }
                catch (Exception)
                {
                }
            }

            this.Listener = null;

            SmartSocketClient[] snapshot = null;
            lock (this.Clients)
            {
                snapshot = this.Clients.ToArray();
            }

            foreach (SmartSocketClient client in snapshot)
            {
                client.Close();
            }

            lock (this.Clients)
            {
                this.Clients.Clear();
            }
        }

        internal async Task<bool> OpenBackChannel(SmartSocketClient client, int port)
        {
            if (this.BackChannelOpened != null)
            {
                IPEndPoint ipe = (IPEndPoint)client.Socket.RemoteEndPoint;
                IPEndPoint endPoint = new IPEndPoint(ipe.Address, port);
                SmartSocketClient channel = await SmartSocketClient.ConnectAsync(endPoint, this.ServiceName, this.Resolver);
                client.BackChannel = channel;
                this.BackChannelOpened(this, client);
                return true;
            }
            else
            {
                // server is not expecting a backchannel!
                return false;
            }
        }
    }
}
