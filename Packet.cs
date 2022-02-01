using System;
using System.Collections.Generic;
using System.Text;

namespace Online {
    /// <summary>Enumeration of reserved id.</summary>
    public enum SpecialId {
        Server = 0,
        Broadcast = ushort.MaxValue,
        Null = ushort.MaxValue - 1
    }

    /// <summary>
    /// Used to read and send data through clients and the server
    /// The packet is in the format [Packet size, Sender Id, Target Id, Name, content]
    /// </summary>
    public class Packet {
        #region Variables
        /// <summary>Gets the length of the packet's content.</summary>
        public int Length { get => buffer.Count; }

        /// <summary>Gets the length of the unread data contained in the packet.</summary>
        public int UnreadLength { get => Length - readPos; }
        /// <summary>The sender's id.</summary>
        public ushort SenderId {
            get => _senderId;
            set {
                byte[] bytes = BitConverter.GetBytes(value);
                int startIndex = _lengthWrote ? 4 : 0;
                for (int i = 0; i < bytes.Length; i++) { buffer[startIndex + i] = bytes[i]; }
                _senderId = value;
            }
        }
        /// <summary>The target client's id.</summary>
        public ushort TargetId {
            get => _targetId;
            set {
                byte[] bytes = BitConverter.GetBytes(value);
                int startIndex = 2 + (_lengthWrote ? 4 : 0);
                for (int i = 0; i < bytes.Length; i++) { buffer[startIndex + i] = bytes[i]; }
                _targetId = value;
            }
        }
        /// <summary>The name given to the packet.</summary>
        public string Name { get; private set; }

        private ushort _senderId = (ushort)SpecialId.Null;
        private ushort _targetId = (ushort)SpecialId.Null;
        private List<byte> buffer;
        private byte[] readableBuffer;
        private int readPos;
        private bool _lengthWrote = false;
        #endregion

        #region Constructors
        /// <summary>Creates a new packet with given IDs and name. Used for sending.</summary>
        public Packet(SpecialId senderId, SpecialId targetId, string name) : this((ushort)senderId, (ushort)targetId, name) { }
        /// <summary>Creates a new packet with given IDs and name. Used for sending.</summary>
        public Packet(ushort senderId, SpecialId targetId, string name) : this(senderId, (ushort)targetId, name) { }
        /// <summary>Creates a new packet with given IDs and name. Used for sending.</summary>
        public Packet(SpecialId senderId, ushort targetId, string name) : this((ushort)senderId, targetId, name) { }

        /// <summary>Creates a new packet with given IDs and name. Used for sending.</summary>
        public Packet(ushort senderId, ushort targetId, string name) {
            buffer = new List<byte>();
            readPos = 0;

            _senderId = senderId;
            _targetId = targetId;
            Name = name;

            Write(SenderId);
            Write(TargetId);
            Write(Name);
        }

        /// <summary>Creates a packet from which data can be read. Used for receiving.</summary>
        /// <param name="data">The bytes to add to the packet.</param>
        public Packet(byte[] data) {
            buffer = new List<byte>(); // Intitialize buffer
            readPos = 0; // Set readPos to 0

            if (data.Length > 0) {
                SetBytes(data);

                ReadInt(); // Read the useless length value from data.
                _senderId = ReadUshort();
                _targetId = ReadUshort();
                Name = ReadString();
            } else {
                // It's a packet of disconnection.
                _targetId = (int)SpecialId.Server;
                Name = "disconnect";
            }
            _lengthWrote = true;
        }

        /// <summary>Copy constructor.</summary>
        public Packet(Packet packet) : this(packet.buffer.ToArray()) { }
        #endregion

        #region Functions
        /// <summary>Sets the packet's content and prepares it to be read.</summary>
        /// <param name="data">The bytes to add to the packet.</param>
        public void SetBytes(byte[] data) {
            Write(data);
            readableBuffer = buffer.ToArray();
        }

        /// <summary>Inserts the length of the packet's content at the start of the buffer.</summary>
        public void WriteLength() {
            if (!_lengthWrote) {
                buffer.InsertRange(0, BitConverter.GetBytes(buffer.Count)); // Insert the byte length of the packet at the very beginning
                _lengthWrote = true;
            }
        }

        /// <summary>Inserts the given int at the start of the buffer.</summary>
        /// <param name="value">The int to insert.</param>
        public void InsertInt(int value) {
            buffer.InsertRange(0, BitConverter.GetBytes(value)); // Insert the int at the start of the buffer
        }

        /// <summary>Gets the packet's content in array form.</summary>
        public byte[] ToArray() {
            readableBuffer = buffer.ToArray();
            return readableBuffer;
        }

        /// <summary>Resets the packet instance to allow it to be reused.</summary>
        /// <param name="shouldReset">Whether or not to reset the packet.</param>
        public void Reset() {
            buffer.Clear(); // Clear buffer
            readableBuffer = null;
            readPos = 0; // Reset readPos
        }

        /// <summary>Reset only the reading of the packet.</summary>
        public void ResetReading() {
            readPos = 0;
            // Read all default values to set the readPos on the right position
            ReadInt();
            ReadUshort();
            ReadUshort();
            ReadString();
        }
        #endregion

        #region Write Data
        /// <summary>Adds a byte to the packet.</summary>
        public Packet Write(byte value) {
            buffer.Add(value);
            return this;
        }
        /// <summary>Adds an array of bytes to the packet.</summary>
        public Packet Write(byte[] value) {
            buffer.AddRange(value);
            return this;
        }
        /// <summary>Adds a short to the packet.</summary>
        public Packet Write(short value) {
            buffer.AddRange(BitConverter.GetBytes(value));
            return this;
        }
        /// <summary>Adds a short to the packet.</summary>
        public Packet Write(ushort value) {
            buffer.AddRange(BitConverter.GetBytes(value));
            return this;
        }
        /// <summary>Adds an int to the packet.</summary>
        public Packet Write(int value) {
            buffer.AddRange(BitConverter.GetBytes(value));
            return this;
        }
        /// <summary>Adds a long to the packet.</summary>
        public Packet Write(long value) {
            buffer.AddRange(BitConverter.GetBytes(value));
            return this;
        }
        /// <summary>Adds a float to the packet.</summary>
        public Packet Write(float value) {
            buffer.AddRange(BitConverter.GetBytes(value));
            return this;
        }
        /// <summary>Adds a bool to the packet.</summary>
        public Packet Write(bool value) {
            buffer.AddRange(BitConverter.GetBytes(value));
            return this;
        }
        /// <summary>Adds a string to the packet.</summary>
        public Packet Write(string value) {
            Write(value.Length); // Add the length of the string to the packet
            buffer.AddRange(Encoding.ASCII.GetBytes(value)); // Add the string itself
            return this;
        }
        #endregion

        #region Read Data
        /// <summary>Reads a byte from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public byte ReadByte(bool moveReadPos = true) {
            if (buffer.Count > readPos) {
                // If there are unread bytes
                byte value = readableBuffer[readPos]; // Get the byte at readPos' position
                if (moveReadPos) {
                    // If moveReadPos is true
                    readPos += 1; // Increase readPos by 1
                }
                return value; // Return the byte
            } else {
                throw new Exception("Could not read value of type 'byte'!");
            }
        }

        /// <summary>Reads an array of bytes from the packet.</summary>
        /// <param name="length">The length of the byte array.</param>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public byte[] ReadBytes(int length, bool moveReadPos = true) {
            if (buffer.Count > readPos) {
                // If there are unread bytes
                byte[] value = buffer.GetRange(readPos, length).ToArray(); // Get the bytes at readPos' position with a range of _length
                if (moveReadPos) {
                    // If moveReadPos is true
                    readPos += length; // Increase readPos by _length
                }
                return value; // Return the bytes
            } else {
                throw new Exception("Could not read value of type 'byte[]'!");
            }
        }

        /// <summary>Reads a short from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public short ReadShort(bool moveReadPos = true) {
            if (buffer.Count > readPos) {
                // If there are unread bytes
                short value = BitConverter.ToInt16(readableBuffer, readPos); // Convert the bytes to a short
                if (moveReadPos) {
                    // If moveReadPos is true and there are unread bytes
                    readPos += 2; // Increase readPos by 2
                }
                return value; // Return the short
            } else {
                throw new Exception("Could not read value of type 'short'!");
            }
        }

        /// <summary>Reads a short from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public ushort ReadUshort(bool moveReadPos = true) {
            if (buffer.Count > readPos) {
                // If there are unread bytes
                ushort value = BitConverter.ToUInt16(readableBuffer, readPos); // Convert the bytes to a short
                if (moveReadPos) {
                    // If moveReadPos is true and there are unread bytes
                    readPos += 2; // Increase readPos by 2
                }
                return value; // Return the short
            } else {
                throw new Exception("Could not read value of type 'short'!");
            }
        }

        /// <summary>Reads an int from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public int ReadInt(bool moveReadPos = true) {
            if (buffer.Count > readPos) {
                // If there are unread bytes
                int value = BitConverter.ToInt32(readableBuffer, readPos); // Convert the bytes to an int
                if (moveReadPos) {
                    // If moveReadPos is true
                    readPos += 4; // Increase readPos by 4
                }
                return value; // Return the int
            } else {
                throw new Exception("Could not read value of type 'int'!");
            }
        }

        /// <summary>Reads a long from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public long ReadLong(bool moveReadPos = true) {
            if (buffer.Count > readPos) {
                // If there are unread bytes
                long value = BitConverter.ToInt64(readableBuffer, readPos); // Convert the bytes to a long
                if (moveReadPos) {
                    // If moveReadPos is true
                    readPos += 8; // Increase readPos by 8
                }
                return value; // Return the long
            } else {
                throw new Exception("Could not read value of type 'long'!");
            }
        }

        /// <summary>Reads a float from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public float ReadFloat(bool moveReadPos = true) {
            if (buffer.Count > readPos) {
                // If there are unread bytes
                float value = BitConverter.ToSingle(readableBuffer, readPos); // Convert the bytes to a float
                if (moveReadPos) {
                    // If moveReadPos is true
                    readPos += 4; // Increase readPos by 4
                }
                return value; // Return the float
            } else {
                throw new Exception("Could not read value of type 'float'!");
            }
        }

        /// <summary>Reads a bool from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public bool ReadBool(bool moveReadPos = true) {
            if (buffer.Count > readPos) {
                // If there are unread bytes
                bool value = BitConverter.ToBoolean(readableBuffer, readPos); // Convert the bytes to a bool
                if (moveReadPos) {
                    // If moveReadPos is true
                    readPos += 1; // Increase readPos by 1
                }
                return value; // Return the bool
            } else {
                throw new Exception("Could not read value of type 'bool'!");
            }
        }

        /// <summary>Reads a string from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public string ReadString(bool moveReadPos = true) {
            try {
                int _length = ReadInt(); // Get the length of the string
                string value = Encoding.ASCII.GetString(readableBuffer, readPos, _length); // Convert the bytes to a string
                if (moveReadPos && value.Length > 0) {
                    // If moveReadPos is true string is not empty
                    readPos += _length; // Increase readPos by the length of the string
                }
                return value; // Return the string
            } catch {
                throw new Exception("Could not read value of type 'string'!");
            }
        }
        #endregion

        public override string ToString() {

            byte[] b = ToArray();
            string res = "";
            foreach (byte by in b) {
                string str = Convert.ToString(by, 16);
                while (str.Length < 2) {
                    str = "0" + str;
                }
                res += str + " ";
            }

            return $"Packet( {res})";
        }

        public string ToStringBetter() {
            return $"Packet details(" +
                $"\n\tData : {ToString()}" +
                $"\n\tLength : {Length}" +
                $"\n\tUnreadLength : {UnreadLength}" +
                $")";
        }
    }
}