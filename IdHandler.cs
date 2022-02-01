using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online {
    /// <summary>
    /// Used to manage pairs of client's id and client's name.
    /// </summary>
    public class IdHandler {
        #region Variables
        private static Dictionary<ushort, string> _idNames = new Dictionary<ushort, string>() {
            { (ushort)SpecialId.Broadcast, "everyone" },
            { (ushort)SpecialId.Server, "server" }
        };

        private static Dictionary<string, ushort> _namesId = new Dictionary<string, ushort>() {
            { "everyone", (ushort)SpecialId.Broadcast },
            { "server", (ushort)SpecialId.Server }
        };
        #endregion

        #region Functions
        /// <summary>Test to know if the Client with a specific attribut exist.</summary>
        public static bool ClientExist(string name) { return _namesId.ContainsKey(name); }
        /// <summary>Test to know if the Client with a specific attribut exist.</summary>
        public static bool ClientExist(ushort id) { return _idNames.ContainsKey(id); }

        /// <summary>Give the name of the client from his id.</summary>
        public static string IdToName(ushort id) { return _idNames[id]; }
        /// <summary>Give the id of the client from his name.</summary>
        public static ushort NameToId(string name) { return _namesId[name]; }

        /// <summary>Add a pair of id and name, and return false if a client with similar name already exist.</summary>
        /// <remarks>If the id is already defined, it will throw an <see cref="ArgumentException"/> instead of a bool.</remarks>
        public static bool AddIdName(ushort id, string name) {
            if (_idNames.ContainsKey(id)) throw new ArgumentException($"Already contains a client with id {id}");
            if (_namesId.ContainsKey(name)) return false;
            _idNames.Add(id, name);
            _namesId.Add(name, id);
            return true;
        }

        /// <summary>Remove a pair of id and name.</summary>
        public static void RemoveIdName(ushort id) {
            if (_idNames.ContainsKey(id)) {
                _namesId.Remove(_idNames[id]);
                _idNames.Remove(id);
            }
        }
        /// <summary>Remove a pair of id and name.</summary>
        public static void RemoveIdName(string name) {
            if (_namesId.ContainsKey(name)) {
                _idNames.Remove(_namesId[name]);
                _namesId.Remove(name);
            }
        }
        #endregion
    }
}
