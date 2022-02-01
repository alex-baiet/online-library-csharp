using System;
using System.Collections.Generic;

namespace Online {

    /// <summary>Used to create Packet from received data</summary>
    public class PacketReception {
        private byte[] _dataPart;

        /// <summary>Return the different packets contained in the data.</summary>
        public Packet[] GetPackets(byte[] data) {
            List<Packet> packets = new List<Packet>();
            if (data.Length == 0) {
                packets.Add(new Packet(SpecialId.Null, SpecialId.Server, "disconnect"));
                return packets.ToArray();
            }

            int cursor = 0;
            if (_dataPart != null) {
                // Complete dataPart
                byte[] lengthRaw = new byte[4];
                for (int i = 0; i < 4; i++) {
                    if (i < _dataPart.Length) lengthRaw[i] = _dataPart[i];
                    else lengthRaw[i] = data[i - _dataPart.Length];
                }

                int length = BitConverter.ToInt32(lengthRaw, 0) + 4;
                if (length > ClientOrigin.BufferSize) throw new NotSupportedException($"Can't manage packet of more than {ClientOrigin.BufferSize} bytes.");
                int missingLength = length - _dataPart.Length;
                byte[] dataPacket = new byte[length];
                Array.Copy(_dataPart, dataPacket, _dataPart.Length);
                Array.Copy(data, 0, dataPacket, _dataPart.Length, missingLength);
                packets.Add(new Packet(dataPacket));

                _dataPart = null;
                cursor += missingLength;
            }

            byte[] packetData;
            while (cursor < data.Length) {
                if (data.Length - cursor >= 4) {
                    int size = BitConverter.ToInt32(data, cursor) + 4; // Converts bytes to int
                    if (data.Length - cursor >= size) {
                        packetData = new byte[size];
                        Array.Copy(data, cursor, packetData, 0, size);
                        // ConsoleServer.WriteLine(Helper.ArrayToString(packetData), MessageType.Debug);
                        packets.Add(new Packet(packetData));
                        cursor += size;
                    } else { // The data left is not enough to create one full Packet.
                        StoreLeftData(data, cursor);
                        break;
                    }
                } else { // The data left is not enough to create at least the length of the Packet.
                    StoreLeftData(data, cursor);
                    break;
                }
            }

            return packets.ToArray();
        }

        /// <summary>Store the left data who is not enough to create a packet,
        /// to finish creating the packet at the next reception.</summary>
        private void StoreLeftData(byte[] data, int cursor) {
            int sizeLeft = data.Length - cursor;
            _dataPart = new byte[sizeLeft];
            Array.Copy(data, cursor, _dataPart, 0, sizeLeft);
        }
    }
}
