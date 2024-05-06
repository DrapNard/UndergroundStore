using Avalonia.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace UndergroundShop.Management
{
    internal class MessageManagement
    {
        public static string? DirNotEx;

        public static void Message(string message)
        {

        }

        public static void ConsoleMessage(string message, int gravity)
        {
            // Define a dictionary to map gravity levels to log levels and console colors
            var logLevelMap = new Dictionary<int, (LogEventLevel level, ConsoleColor color)>()
            {
                { 0, (LogEventLevel.Verbose, ConsoleColor.Magenta) },
                { 1, (LogEventLevel.Debug, ConsoleColor.Blue) },
                { 2, (LogEventLevel.Information, ConsoleColor.White) },
                { 3, (LogEventLevel.Warning, ConsoleColor.Yellow) },
                { 4, (LogEventLevel.Error, ConsoleColor.Red) },
                { 5, (LogEventLevel.Fatal, ConsoleColor.Red) },
            };

            // Use a switch statement for efficient gravity level handling
            if (logLevelMap.TryGetValue(gravity, out var logLevelAndColor))
            {
                Console.ForegroundColor = logLevelAndColor.color;
                Console.WriteLine($"[{logLevelAndColor.level}] {message}");
                Logger.TryGet(logLevelAndColor.level, LogArea.Visual)?.Log(null, $"[{logLevelAndColor.level}] {message}");

                if (Configuration.Instance.LogFile)
                {
                    WriteLogToFile(logLevelAndColor.level.ToString(), message);
                }
            }
            else
            {
                ConsoleMessage("Invalid gravity level: " + gravity, 3);
            }

            Console.ResetColor(); // Reset console color to default after use
        }
        private static void WriteLogToFile(string level, string message)
        {
            string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Log/{DateTime.Now:yyyy-MM-dd}.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logFile)); // Ensure directory exists

            using (StreamWriter writer = File.AppendText(logFile))
            {
                writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}");
            }
        }
    }
}
