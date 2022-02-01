using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;

namespace Online {
    /// <summary>
    /// Represent a connection with the distant server.
    /// Used to communicate with the server.
    /// </summary>
    public class ClientOrigin : ClientOnline {
        public static ClientOrigin Instance = new ClientOrigin();

        private int spamCount = 0;
        public bool Connected { get; private set; } = false;

        private Action _onConnectSuccess = () => { };
        private Action<Exception> _onConnectFailure = (Exception e) => { };

        private ClientOrigin() {
            PacketHandler.AddPacketHandler(OnlineSide.Client, new Dictionary<string, PacketHandler.Handler>() {
                {
                    // Tous les données nécessaires pour la connection ont été envoyées.
                    "allConnectionDataSent",
                    (Packet p) => {
                        ConsoleServer.WriteLine("Connected successfully to server !", MessageType.Success);
                        _onConnectSuccess();
                        Packet toSend = new Packet(Id, SpecialId.Server, "allConnectionDataReceived");
                        SendPacket(toSend);
                    }
                },
                {
                    // Un autre client s'est déconnecté.
                    "clientDisconnect",
                    (Packet p) => {
                        ushort id = p.ReadUshort();
                        ConsoleServer.WriteLine($"{IdHandler.IdToName(id)} disconnected.");
                        IdHandler.RemoveIdName(id);
                    }
                },
                {
                    // Le serveur a déconnecté le client.
                    "disconnect",
                    (Packet p) => {
                        string errorMsg = p.ReadString();
                        ConsoleServer.WriteLine($"Connection to server failed : {errorMsg}", MessageType.Error);
                        Disconnect();
                    }
                },
                {
                    // Contient le nom attaché à l'id du nouveau client.
                    "idName",
                    (Packet p) => {
                        ushort id = p.ReadUshort();
                        string idName = p.ReadString();
                        if (!IdHandler.ClientExist(id)) IdHandler.AddIdName(id, idName);
                        ConsoleServer.WriteLine($"{idName} is connected to server with id {id}.", MessageType.Debug);
                    }
                },
                {
                    // Réception d'un message.
                    "msg",
                    (Packet p) => {
                        string msg = p.ReadString();
                        ConsoleServer.WriteLine(msg);
                    }
                },
                {
                    // Demande de ping.
                    "ping",
                    (Packet p) => {
                        if (p.SenderId == (ushort)SpecialId.Server) {
                            Packet toSend = new Packet(Id, SpecialId.Server, "pingReturn");
                            SendPacket(toSend);
                        }
                    }
                },
                {
                    // Retour d'un ping envoyé.
                    "pingReturn",
                    (Packet p) => {
                        EndPing();
                    }
                },
                {
                    // Packet vide envoyé en grand nombre pour tester la gestion des hauts débits de packet.
                    "spam",
                    (Packet p) => {
                        ConsoleServer.WriteLine($"Spam count : {++spamCount}", MessageType.Debug);
                    }
                },
                {
                    // Contient l'id du client.
                    "yourId",
                    (Packet p) => {
                        Id = p.TargetId;
                        ConsoleServer.WriteLine($"Your assigned id : {Id}", MessageType.Debug);
                    }
                }
            });
        }

        #region Connection
        /// <summary>Connect the client to the server.</summary>
        /// <param name="ip">The host's ip.</param>
        /// <param name="port">The host's port.</param>
        /// <param name="pseudo">Your client's pseudo.</param>
        /// <param name="onConnectSuccess">lambda to execute after connecting successfully.</param>
        /// <param name="onConnectFailed">lambda to execute after failing connection.</param>
        public void Connect(string ip, int port, string pseudo, Action onSuccess, Action<Exception> onFailure) {
            if (Connected) {
                throw new InvalidOperationException("The client is already connected to a server.");
            }
            Pseudo = string.IsNullOrWhiteSpace(pseudo) ? "Guest" : pseudo;
            _tcpClient = new TcpClient();
            _onConnectSuccess = onSuccess;
            _onConnectFailure = onFailure;

            ConsoleServer.WriteLine($"Starting connexion to {ip}:{port}...");
            try {
                _tcpClient.BeginConnect(ip, port, new AsyncCallback(ConnectCallback), _tcpClient);
            } catch (SocketException e) {
                onFailure(e);
            }
        }

        /// <summary>Disconnect the client.</summary>
        public override void Disconnect() {
            base.Disconnect();
            Connected = false;
        }

        /// <summary>Called when ending the connection to the server.</summary>
        private void ConnectCallback(IAsyncResult res) {
            if (_tcpClient.Connected) {
                ConsoleServer.WriteLine("Connection to server establish. Waiting for datas...", MessageType.Normal);
                _tcpClient.EndConnect(res);
                _stream = _tcpClient.GetStream();
                _stream.BeginRead(_receiveBuffer, 0, BufferSize, new AsyncCallback(ReceiveCallback), null);

                // Sending pseudo to server to finish the connection.
                Packet packet = new Packet(Id, SpecialId.Server, "pseudo");
                packet.Write(Pseudo);
                SendPacket(packet);
                Connected = true;
            } else {
                ConsoleServer.WriteLine("Connection to server failed.", MessageType.Error);
            }
        }
        #endregion

        #region Sending
        /// <summary>Ask for an information to the server.</summary>
        public void Query(SpecialId id, string name) { Query((ushort)id, name); }
        /// <summary>Ask for an information to the server.</summary>
        public void Query(ushort id, string name) {
            Packet packet = new Packet(Id, SpecialId.Server, "query");
            packet.Write(id).Write(name);
            SendPacket(packet);
        }

        /// <summary>Send a message to the distant connection.</summary>
        /// <remarks>The message is just a "msg" packet.</remarks>
        public void SendMessage(SpecialId toClientId, string msg) { SendMessage((ushort)toClientId, msg); }
        /// <summary>Send a message to the distant connection.</summary>
        /// <remarks>The message is just a "msg" packet.</remarks>
        public void SendMessage(ushort toClientId, string msg) {
            Packet packet = new Packet(Id, toClientId, "msg");
            packet.Write(ConsoleServer.ToMessageFormat(Pseudo, msg));
            SendPacket(packet);

            ConsoleServer.WriteLine($"Message sent to {IdHandler.IdToName(toClientId)}.", MessageType.Debug);
        }
        
        /// <summary>Send a packet to the distant connection.</summary>
        public override void SendPacket(Packet packet) {
            try {
                packet.WriteLength();
                ConsoleServer.WriteLine($"sent packet \"{packet.Name}\" with length : {packet.Length}", MessageType.Packet);
                _stream.BeginWrite(packet.ToArray(), 0, packet.Length, null, null);
            } catch (ObjectDisposedException) { }
        }

        /// <summary>Send a ping to the server.</summary>
        /// <remarks>The ping is just a "ping" packet.</remarks>
        public void Ping() {
            Ping((ushort)SpecialId.Server);
        }

        /// <summary>Send a ping to the specified client.</summary>
        /// <remarks>The ping is just a "ping" packet.</remarks>
        public void Ping(ushort id) {
            if (!_stopwatch.IsRunning) {
                Packet packet = new Packet(Id, id, "ping");
                SendPacket(packet);
                _stopwatch.Restart();
            }
        }
        #endregion

        #region Receiving
        /// <summary>Called when a packet is received.</summary>
        private void ReceiveCallback(IAsyncResult res) {
            try {
                int byteLength = _stream.EndRead(res);

                byte[] data = new byte[byteLength];
                Array.Copy(_receiveBuffer, data, byteLength);

                Packet[] packets = _packetManager.GetPackets(data);

                foreach (Packet packet in packets) {
                    ConsoleServer.WriteLine($"Receiving packet from the server named \"{packet.Name}\" (size={packet.Length})", MessageType.Packet);

                    PacketHandler.ExecPacketHandler(OnlineSide.Client, packet);
                    //ClientReceive.HandlePacket(packet, this);
                }
                if (_tcpClient.Connected) _stream.BeginRead(_receiveBuffer, 0, BufferSize, new AsyncCallback(ReceiveCallback), null);

            } catch (IOException e) {
                ConsoleServer.WriteLine($"Lost connection to server.", MessageType.Error);
                _onConnectFailure(e);
                Disconnect();
            }
        }
        #endregion
    }
}
