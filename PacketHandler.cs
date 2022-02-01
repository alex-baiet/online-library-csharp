using System.Collections.Generic;
using System.Collections;
using System;

namespace Online {
    /// <summary>
    /// Manage action to do when receiving packet.
    /// </summary>
    public class PacketHandler {
        public delegate void Handler(Packet packet);

        private static Dictionary<OnlineSide, Dictionary<string, Handler>> handlers = new Dictionary<OnlineSide, Dictionary<string, Handler>>() {
            { OnlineSide.Server, new Dictionary<string, Handler>() },
            { OnlineSide.Client, new Dictionary<string, Handler>() }
        };

        public static void AddPacketHandler(OnlineSide side, Dictionary<string, Handler> handlers) {
            foreach (KeyValuePair<string, Handler> pair in handlers) {
                AddPacketHandler(side, pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Défini l'action a effectué lors de la réception d'un packet correspondant à packetName.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="packetName"></param>
        /// <param name="handler"></param>
        public static void AddPacketHandler(OnlineSide side, string packetName, Handler handler) {
            handlers[side][packetName] = handler;
        }

        public static void ExecPacketHandler(OnlineSide side, Packet packet) {
            if (handlers[side].ContainsKey(packet.Name)) {
                handlers[side][packet.Name](packet);
            } else {
                throw new ArgumentException($"Le packet {packet.Name} n'a pas de handler approprié.");
            }
        }
    }
}