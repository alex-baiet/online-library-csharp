using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections.Generic;

namespace Online {
    /// <summary>Used to communicate with distant clients.</summary>
    public class ClientDistant : ClientOnline {
        private int spamCount = 0;
        private bool _handlersAdded = false;
        private byte[] _test { get => _receiveBuffer; set => _receiveBuffer = value; }

        /// <summary>Create a connexion to a client.</summary>
        public ClientDistant(TcpClient tcpClient, ushort id) {
            if (!_handlersAdded) {
                AddPacketHandlers();
                _handlersAdded = true;
            }

            // Tcp connection
            _tcpClient = tcpClient;
            _stream = _tcpClient.GetStream();
            _receiveBuffer = new byte[BufferSize];
            Pseudo = _tcpClient.Client.RemoteEndPoint.ToString();
            Id = id;

            _stream.BeginRead(_test, 0, BufferSize, new AsyncCallback(ReceiveCallback), null);
        }

        private void AddPacketHandlers() {
            PacketHandler.AddPacketHandler(OnlineSide.Server, new Dictionary<string, PacketHandler.Handler>() {
                {
                    // Confirmation reception of all datas.
                    "allConnectionDataReceived",
                    (Packet p) => {
                        Packet toSend = new Packet(SpecialId.Server, SpecialId.Broadcast, "idName");
                        toSend.Write(Id);
                        toSend.Write(Pseudo);
                        Server.Instance.SendPacket(SpecialId.Broadcast, toSend);
                        Server.Instance.SendMessage(SpecialId.Broadcast, $"{Pseudo} join the Server.Instance.");
                    }
                },
                {
                    // Beginning of the client's connection.
                    "connected",
                    (Packet p) => {
                        ConsoleServer.WriteLine($"{Pseudo} connecting to the Server. Instance with id {Id}...", MessageType.Debug);
                    }
                },
                {
                    // Client's disconnection.
                    "disconnect",
                    (Packet p) => {
                        ConsoleServer.WriteLine($"{Pseudo} disconnected.");
                        Server.Instance.RemoveClient(Id, "Well it's you who disconnected but if you see this this is not normal :(");
                    }
                },
                {
                    // Message.
                    "msg",
                    (Packet p) => {
                        string msg = p.ReadString();
                        if (p.TargetId == (ushort)SpecialId.Broadcast) Server.Instance.SendMessage(p.TargetId, msg);
                        else Server.Instance.SendMessage(new ushort[] { p.TargetId, p.SenderId }, msg);
                    }
                },
                {
                    // Ping request.
                    "ping",
                    (Packet p) => {
                        Packet toSend = new Packet(SpecialId.Server, Id, "pingReturn");
                        SendPacket(toSend);
                        if (p.TargetId != (ushort)SpecialId.Server) {
                            Server.Instance.SendPacket(p.TargetId, p);
                        }
                    }
                },
                {
                    // Return of ping send.
                    "pingReturn",
                    (Packet p) => {
                        EndPing();
                    }
                },
                {
                    // Reception of the connecting client, send of all datas required by the client
                    "pseudo",
                    (Packet p) => {
                        Pseudo = p.ReadString();
                        if (!IdHandler.AddIdName(Id, Pseudo)) { // A client with the same name already exist
                            ConsoleServer.WriteLine($"Connection of {Pseudo} failed : another client with the same name already exist.");
                            Server.Instance.RemoveClient(Id, "Another client with the same name already exist.");
                            return;
                        }
                        // Sending all datas
                        Packet toSend = new Packet(SpecialId.Server, Id, "yourId");
                        SendPacket(toSend);

                        // Sending all clients name
                        foreach (ushort idConnected in Server.Instance.ConnectedClientsId) {
                            if (idConnected == Id) continue;
                            toSend = new Packet(SpecialId.Server, Id, "idName");
                            toSend.Write(idConnected);
                            toSend.Write(Server.Instance.GetClient(idConnected).Pseudo);
                            SendPacket(toSend);
                        }

                        // Packet meaning all data has been sent.
                        toSend = new Packet(SpecialId.Server, Id, "allConnectionDataSent");
                        SendPacket(toSend);
                    }
                },
                {
                    // Test management of big amount of data.
                    "spam",
                    (Packet p) => {
                        ConsoleServer.WriteLine($"Spam count : {++spamCount}", MessageType.Debug);
                    }
                },
            });
        }

        #region Sending
        /// <summary>Send a message to the distant connection.</summary>
        /// <remarks>The message is just a "msg" packet.</remarks>
        public void SendMessage(string msg) {
            Packet packet = new Packet(SpecialId.Server, Id, "msg");
            packet.Write(ConsoleServer.ToMessageFormat(Server.Instance.Name, msg));
            SendPacket(packet);

            ConsoleServer.WriteLine($"Message sent to {Pseudo}.", MessageType.Debug);
        }

        /// <summary>Send a packet to the distant connection.</summary>
        public override void SendPacket(Packet packet) {
            if (!_tcpClient.Connected) return;
            if (packet.TargetId != Id && packet.TargetId != (ushort)SpecialId.Broadcast && packet.SenderId != Id) {
                throw new NotSupportedException($"The packet's id ({packet.TargetId}) must correspond to the client's id ({Id}).");
            }
            packet.WriteLength();
            ConsoleServer.WriteLine($"sent packet \"{packet.Name}\" with length : {packet.Length}", MessageType.Packet);
            _stream.BeginWrite(packet.ToArray(), 0, packet.Length, null, null);
        }

        /// <summary>Send a ping to the distant connection.</summary>
        /// <remarks>The ping is just a "ping" packet.</remarks>
        public void Ping() {
            if (!_stopwatch.IsRunning) {
                Packet packet = new Packet(SpecialId.Server, Id, "ping");
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
                    ConsoleServer.WriteLine($"Receiving packet from {Pseudo} named \"{packet.Name}\" (size={packet.Length})", MessageType.Packet);
                    //Server.InstanceReceive.HandlePacket(packet, this);
                    PacketHandler.ExecPacketHandler(OnlineSide.Server, packet);
                }
                
                if (_tcpClient.Connected) _stream.BeginRead(_receiveBuffer, 0, BufferSize, new AsyncCallback(ReceiveCallback), null);
            } catch (System.IO.IOException) {
                ConsoleServer.WriteLine($"{Pseudo} lost connection.", MessageType.Error);
                Server.Instance.RemoveClient(Id, "Lost connection.");
            } catch (System.ObjectDisposedException) { }
        }

        #endregion
    }
}
