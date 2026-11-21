using System.Collections.Generic;
using System.Linq;
using RadTadDev.Logger;

namespace RadTadDev.SceneSystem.Tests
{
    /// <summary>
    /// Replaces the logger's console output with a list, so tests can check what was logged and the Unity console
    /// stays clean. The logger delivers console messages later, on the main thread, which can happen after a test has
    /// finished, so tests should capture rather than try to ignore them.
    /// </summary>
    public sealed class CapturingLogSink : ISink
    {
        /// <summary>
        /// Every message written, as "Level: message".
        /// </summary>
        public readonly List<string> Entries = new();

        public void Write(string message, LogLevel level)
        {
            Entries.Add(level + ": " + message);
        }

        public void Close() { }

        /// <summary>
        /// Routes all logging into a new capturing sink.
        /// </summary>
        public static CapturingLogSink Install()
        {
            var sink = new CapturingLogSink();
            Log.ClearSinks();
            Log.AddSink(sink);
            return sink;
        }

        /// <summary>
        /// Restores the logger's console output.
        /// </summary>
        public static void Restore()
        {
            Log.ClearSinks();
            Log.AddSink(new ConsoleSink());
        }

        /// <summary>
        /// True if any message contains the text.
        /// </summary>
        public bool Contains(string text)
        {
            return Entries.Any(entry => entry.Contains(text));
        }
    }
}
