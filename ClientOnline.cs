using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace Online {
    /// <summary>
    /// Partie commune des classes client côté serveur et client.
    /// </summary>
    public abstract class ClientOnline {
        public const int BufferSize = 4096;

        public ushort Id { get; set; }
        public string Pseudo { get; set; } = "Guest";

        protected TcpClient _tcpClient;
        protected byte[] _receiveBuffer = new byte[BufferSize];
        protected NetworkStream _stream; // on peut pas le recuperer de _tcpClient plutot que de le stocker ?
        protected PacketReception _packetManager = new PacketReception();
        protected Stopwatch _stopwatch = new Stopwatch();

        #region Connection
        public virtual void Disconnect() {
            _tcpClient.Close();
        }
        #endregion

        #region Sending
        public abstract void SendPacket(Packet packet);
        #endregion

        #region Receiving
        /// <summary>To call after receiving a ping answer.</summary>
        public virtual long EndPing() {
            long res = _stopwatch.ElapsedMilliseconds;
            _stopwatch.Stop();
            UnityEngine.Debug.Log($"Ping returned in {res}ms.");
            return res;
        }
        #endregion

        public override string ToString() {
            return $"ClientOnline(Id={Id}, Pseudo=\"{Pseudo})\"";
        }
    }
}