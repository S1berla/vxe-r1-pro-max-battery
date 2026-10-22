using System;
using System.IO;

namespace MouseBattery.Tray
{
    internal static class AppLog
    {
        private static readonly object SyncRoot = new object();

        internal static string LogPath
        {
            get
            {
                string directory = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "VxeMouseBatteryTray");

                return Path.Combine(directory, "app.log");
            }
        }

        internal static void Info(string message)
        {
            Write("INFO", message);
        }

        internal static void Error(string message, Exception error)
        {
            string detail = error == null
                ? message
                : message + " " + error;

            Write("ERROR", detail);
        }

        private static void Write(string level, string message)
        {
            try
            {
                lock (SyncRoot)
                {
                    string path = LogPath;
                    string directory = Path.GetDirectoryName(path);
                    Directory.CreateDirectory(directory);

                    File.AppendAllText(
                        path,
                        string.Format(
                            "{0:O} [{1}] {2}{3}",
                            DateTime.UtcNow,
                            level,
                            message,
                            Environment.NewLine));
                }
            }
            catch
            {
                // Logging must never terminate the tray process.
            }
        }
    }
}

