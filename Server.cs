using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Online {
    /// <summary>The server, managing all clients, connection, etc.</summary>
    public class Server {
        public static Server Instance { get; private set; } = new Server();

        public readonly int Port = 26950;
        public readonly ushort MaxClient = 4;
        public readonly string Name = "Server Test";

        public HashSet<ushort> ConnectedClientsId { get => new HashSet<ushort>(_assignedId); }
        public bool IsOpen { get; private set; } = false;

        private Action _onStartSuccess = () => { };
        private Action<Exception> _onStartFailure = (Exception e) => { };
        private TcpListener _tcpListener;
        private ClientDistant[] _clients;
        private int _connectedCount = 0;
        private HashSet<ushort> _assignedId = new HashSet<ushort>();

        private Server() {
            _clients = new ClientDistant[MaxClient + 1];
        }

        /// <summary>Start the server, beginning to listen to entering connection.</summary>
        public void Start(Action onSuccess, Action<Exception> onFailure) {
            try {
                ConsoleServer.WriteLine($"Starting server...");

                _onStartSuccess = onSuccess;
                _onStartFailure = onFailure;
                _tcpListener = new TcpListener(IPAddress.Any, Port);
                _tcpListener.Start();
                _tcpListener.BeginAcceptTcpClient(new AsyncCallback(ConnectCallback), null);
                IsOpen = true;

                ConsoleServer.WriteLine($"Server started on port {Port} !", MessageType.Success);
                _onStartSuccess();
            } catch (Exception e) {
                _onStartFailure(e);
            }
        }

        /// <summary>Stop the server, disconnecting all clients.</summary>
        public void Stop() {
            HashSet<ushort> copy = new HashSet<ushort>(_assignedId);
            foreach (ushort id in copy) {
                RemoveClient(id, "The server closed.");
            }
            _tcpListener.Stop();
            IsOpen = false;
        }

        /// <summary>Called when a distant connexion try to connect to the server.</summary>
        private void ConnectCallback(IAsyncResult res) {
            try {
                TcpClient tcpClient = _tcpListener.EndAcceptTcpClient(res);
                ConsoleServer.WriteLine("Incoming connection...", MessageType.Debug);

                if (_connectedCount == MaxClient) {
                    // Canceling connection.
                    ClientDistant client = new ClientDistant(tcpClient, (ushort)SpecialId.Null);
                    Packet packet = new Packet(SpecialId.Server, client.Id, "disconnect");
                    packet.Write("Server is already full !");
                    client.SendPacket(packet);
                    client.Disconnect();
                    ConsoleServer.WriteLine("Connection refused : Server is already full !", MessageType.Debug);
                } else {
                    if (tcpClient.Client.Connected) ConsoleServer.WriteLine($"Connected to {tcpClient.Client.RemoteEndPoint}.", MessageType.Debug);
                    AddClient(tcpClient);
                }

                _tcpListener.BeginAcceptTcpClient(new AsyncCallback(ConnectCallback), null);
            } catch (ObjectDisposedException) { }
        }

        /// <summary>Add a client to the server.</summary>
        private void AddClient(TcpClient tcpClient) {
            OpenCheck();
            for (ushort i = 1; i <= MaxClient; i++) {
                if (_clients[i] == null) {
                    _clients[i] = new ClientDistant(tcpClient, i);
                    _assignedId.Add(i);
                    _connectedCount++;
                    return;
                }
            }
        }

        /// <summary>Remove and disconnect a client from the server</summary>
        public void RemoveClient(ushort id, string msg) {
            OpenCheck();
            ClientDistant client = _clients[id];

            Packet packet = new Packet(SpecialId.Server, client.Id, "disconnect");
            packet.Write(msg);
            client.SendPacket(packet);
            client.Disconnect();

            _clients[id] = null;
            _assignedId.Remove(id);
            IdHandler.RemoveIdName(id);
            _connectedCount--;

            packet = new Packet(SpecialId.Server, SpecialId.Broadcast, "clientDisconnect");
            packet.Write(id);
            SendPacket(SpecialId.Broadcast, packet);
        }

        public ClientDistant GetClient(ushort id) {
            return _clients[id];
        }

        /// <summary>Send a message to a client.</summary>
        public void SendMessage(SpecialId id, string msg) { SendMessage((ushort)id, msg); }
        /// <summary>Send a message to a client.</summary>
        public void SendMessage(ushort id, string msg) { SendMessage(new ushort[]{ id }, msg); }
        /// <summary>Send a message to a client.</summary>
        public void SendMessage(ushort[] ids, string msg) {
            OpenCheck();
            ConsoleServer.WriteLine(msg);
            foreach (ushort id in ids) {
                if (id == (ushort)SpecialId.Null) {
                    ConsoleServer.WriteLine($"The message \"{msg}\" target no client.", MessageType.Error);
                    return;
                }
                if (id == (ushort)SpecialId.Broadcast) {
                    foreach (ushort clientId in _assignedId) {
                        Packet packet = new Packet(SpecialId.Server, clientId, "msg");
                        packet.Write(msg);
                        _clients[clientId].SendPacket(packet);
                    }
                    return;
                }
                if (id != (ushort)SpecialId.Server) {
                    Packet packet = new Packet(SpecialId.Server, id, "msg");
                    packet.Write(msg);
                    _clients[id].SendPacket(packet);
                    return;
                }
            }
        }

        /// <summary>Send a packet to a client.</summary>
        public void SendPacket(SpecialId id, Packet packet) { SendPacket((ushort)id, packet); }
        /// <summary>Send a packet to a client.</summary>
        public void SendPacket(ushort id, Packet packet) {
            if (id == (ushort)SpecialId.Server) throw new ArgumentException("Can't send a packet to the server : You are the server !");
            if (id == (ushort)SpecialId.Null) throw new ArgumentNullException("The given id is null.");

            if (id == (ushort)SpecialId.Broadcast) {
                foreach (ushort assigned in _assignedId) {
                    if (packet.SenderId == assigned) continue;
                    _clients[assigned].SendPacket(packet);
                }
                return;
            }
            if (!_assignedId.Contains(id)) throw new ArgumentException($"Can't send a packet to the client {id} : It does not exist.");
            _clients[id].SendPacket(packet); // Last possibility : sending packet to a specific and existing client.
        }

        /// <summary>Ping all clients.</summary>
        public void Ping() {
            OpenCheck();
            foreach (int id in _assignedId) {
                _clients[id].Ping();
            }
        }

        /// <summary>Test if the server is open.</summary>
        private void OpenCheck() {
            if (!IsOpen) throw new NotSupportedException("The server must be open.");
        }
    }
}
