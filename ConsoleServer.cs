using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online {
    public enum MessageType {
        Normal = ConsoleColor.White,
        Warning = ConsoleColor.Yellow,
        Error = ConsoleColor.Red,
        Success = ConsoleColor.Green,
        Debug = ConsoleColor.DarkGray,
        Packet = ConsoleColor.DarkMagenta
    }

    /// <summary>Write in console with a better look aspect than <see cref="Console"/>.</summary>
    /// <remarks>This class is useless on non-console based application.</remarks>
    public class ConsoleServer {
        private const string ReadPrefix = "> ";

        /// <summary>Display or not debug message.</summary>
        public static bool ShowDebug { get; set; } = true;
        /// <summary>Display or not packet message.</summary>
        public static bool ShowListenPacket { get; set; } = false;
        /// <summary>Display or not warning message.</summary>
        public static bool ShowWarning { get; set; } = true;

        private static bool _isReadingLine = false;
        private static object writer = new object();

        /// <summary>Use parameter to create a message format text.</summary>
        public static string ToMessageFormat(string pseudo, string msg) {
            return $"[{pseudo}] {msg}";
        }

        /// <summary>Write a line, using whire color.</summary>
        public static void WriteLine(string msg) { WriteLine(msg, (ConsoleColor)MessageType.Normal); }
        /// <summary>Write a line, using colors.</summary>
        public static void WriteLine(string msg, MessageType color) {
            if (color == MessageType.Debug && !ShowDebug
                || color == MessageType.Packet && !ShowListenPacket
                || color == MessageType.Warning && !ShowWarning
                ) return;
            WriteLine(msg, (ConsoleColor)color);
        }
        /// <summary>Write a line, using colors.</summary>
        public static void WriteLine(string msg, ConsoleColor color) {
            lock (writer) {
                if (_isReadingLine) RemoveCharacters(ReadPrefix.Length);

                Console.ForegroundColor = color;
                Console.WriteLine(msg);
                Console.ForegroundColor = ConsoleColor.White;

                if (_isReadingLine) Console.Write(ReadPrefix);
            }
        }

        /// <summary>Better way to read a line.</summary>
        public static string ReadLine() {
            lock (writer) {
                _isReadingLine = true;
                Console.Write(ReadPrefix);
            }
            string res = Console.ReadLine();
            _isReadingLine = false;
            return res;
        }

        /// <summary>Remove prefix reading characters.</summary>
        private static void RemoveCharacters(int count) {
            for (int i = 0; i < count; i++) Console.Write("\b \b");
        }
    }
}
