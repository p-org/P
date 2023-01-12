// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PChecker.SmartSockets
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
        /// <param name="udpGroupAddress"></param>
        /// <param name="udpGroupPort"></param>
        private SmartSocketServer(string name, SmartSocketTypeResolver resolver, string ipAddress = "127.0.0.1:0",
            string udpGroupAddress = "226.10.10.2", int udpGroupPort = 37992)
        {
            ServiceName = name;
            Resolver = resolver;
            if (ipAddress.Contains(':'))
            {
                var parts = ipAddress.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out var port))
                {
                    IpAddress = new IPEndPoint(IPAddress.Parse(parts[0]), port);
                }
                else
                {
                    throw new ArgumentException("ipAddress is not a valid format");
                }
            }
            else
            {
                IpAddress = new IPEndPoint(IPAddress.Parse(ipAddress), 0);
            }

            if (!string.IsNullOrEmpty(udpGroupAddress))
            {
                GroupAddress = IPAddress.Parse(udpGroupAddress);
                GroupPort = udpGroupPort;
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
        internal static SmartSocketServer StartServer(string name, SmartSocketTypeResolver resolver, string ipAddress,
            string udpGroupAddress = "226.10.10.2", int udpGroupPort = 37992)
        {
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = "127.0.0.1:0";
            }

            var server = new SmartSocketServer(name, resolver, ipAddress, udpGroupAddress, udpGroupPort);
            server.StartListening();
            return server;
        }

        /// <summary>
        /// Start listening for connections from anyone.
        /// </summary>
        private void StartListening()
        {
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ep = IpAddress;
            Listener.Bind(ep);

            var ip = Listener.LocalEndPoint as IPEndPoint;
            EndPoint = ip;
            Listener.Listen(10);

            // now start a background thread to process incoming requests.
            Task.Run(Run);

            if (GroupAddress != null)
            {
                // Start the UDP listener thread
                Task.Run(UdpListenerThread);
            }
        }

        private void UdpListenerThread()
        {
            var localHost = SmartSocketClient.FindLocalHostName();
            var addresses = SmartSocketClient.FindLocalIpAddresses();
            if (localHost == null || addresses.Count == 0)
            {
                return; // no network.
            }

            var remoteEP = new IPEndPoint(GroupAddress, GroupPort);
            UdpListener = new UdpClient(GroupPort);
            UdpListener.JoinMulticastGroup(GroupAddress);
            while (true)
            {
                var data = UdpListener.Receive(ref remoteEP);
                if (data != null)
                {
                    var reader = new BinaryReader(new MemoryStream(data));
                    var len = reader.ReadInt32();
                    var msg = reader.ReadString();
                    if (msg == ServiceName)
                    {
                        // send response back with info on how to connect to this server.
                        var localEp = (IPEndPoint)Listener.LocalEndPoint;
                        var addr = localEp.ToString();
                        var ms = new MemoryStream();
                        var writer = new BinaryWriter(ms);
                        writer.Write(addr.Length);
                        writer.Write(addr);
                        writer.Flush();
                        var buffer = ms.ToArray();
                        UdpListener.Send(buffer, buffer.Length, remoteEP);
                    }
                }
            }
        }

        /// <summary>
        /// Call this method on a background thread to listen to our port.
        /// </summary>
        internal void Run()
        {
            if (acceptArgs == null)
            {
                acceptArgs = new SocketAsyncEventArgs();
                acceptArgs.Completed += OnAcceptComplete;
            }

            if (!Stopped)
            {
                try
                {
                    Listener.AcceptAsync(acceptArgs);
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
            if (acceptArgs == e)
            {
                acceptArgs = null;
                var client = e.AcceptSocket;
                OnAccept(client);
                Run();
            }
        }

        private void OnAccept(Socket client)
        {
            var ep1 = client.RemoteEndPoint as IPEndPoint;
            var proxy = new SmartSocketClient(this, client, Resolver)
            {
                Name = ep1.ToString(),
                ServerName = SmartSocketClient.FindLocalHostName()
            };

            proxy.Disconnected += OnClientDisconnected;

            SmartSocketClient[] snapshot = null;

            lock (Clients)
            {
                snapshot = Clients.ToArray();
            }

            foreach (var s in snapshot)
            {
                var ep2 = s.Socket.RemoteEndPoint as IPEndPoint;
                if (ep1 == ep2)
                {
                    // can only have one client using this end point.
                    RemoveClient(s);
                }
            }

            lock (Clients)
            {
                Clients.Add(proxy);
            }

            if (ClientConnected != null)
            {
                ClientConnected(this, proxy);
            }
        }

        private void OnClientDisconnected(object sender, EventArgs e)
        {
            var client = (SmartSocketClient)sender;
            RemoveClient(client);
        }

        internal void RemoveClient(SmartSocketClient client)
        {
            var found = false;
            lock (Clients)
            {
                found = Clients.Contains(client);
                Clients.Remove(client);
            }

            if (found && ClientDisconnected != null)
            {
                ClientDisconnected(this, client);
            }
        }

        /// <summary>
        /// Call this method to stop the background thread, it is good to do this before your app shuts down.
        /// This will also send a Disconnect message to all the clients so they know the server is gone.
        /// </summary>
        public void Stop()
        {
            Stopped = true;
            using (Listener)
            {
                try
                {
                    if (acceptArgs != null)
                    {
                        acceptArgs.Dispose();
                        acceptArgs = null;
                    }
                }
                catch (Exception)
                {
                }
            }

            Listener = null;

            SmartSocketClient[] snapshot = null;
            lock (Clients)
            {
                snapshot = Clients.ToArray();
            }

            foreach (var client in snapshot)
            {
                client.Close();
            }

            lock (Clients)
            {
                Clients.Clear();
            }
        }

        internal async Task<bool> OpenBackChannel(SmartSocketClient client, int port)
        {
            if (BackChannelOpened != null)
            {
                var ipe = (IPEndPoint)client.Socket.RemoteEndPoint;
                var endPoint = new IPEndPoint(ipe.Address, port);
                var channel = await SmartSocketClient.ConnectAsync(endPoint, ServiceName, Resolver);
                client.BackChannel = channel;
                BackChannelOpened(this, client);
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
