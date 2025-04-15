using Avalonia.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace UndergroundShop.Management
{
    internal class MessageManagement
    {
        private static readonly Dictionary<string, string> DefaultMessages = [];
        private static readonly Dictionary<string, string> ActiveMessages = [];

        public static string? DirNotEx => "0fc0001";

        static MessageManagement()
        {
            LoadDefaultMessages();
            LoadActiveMessages();
        }

        /// <summary>
        /// Loads the default English messages into the dictionary.
        /// </summary>
        private static void LoadDefaultMessages()
        {
            string defaultFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "en.json");
            LoadMessagesFromFile(defaultFilePath, DefaultMessages, true);
        }

        /// <summary>
        /// Loads the selected language messages, falling back to English for missing keys or the entire file.
        /// </summary>
        private static void LoadActiveMessages()
        {
            string lang = Configuration.Instance.Lang ?? "en";
            string langFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{lang}.json");

            if (lang != "en" && !File.Exists(langFilePath))
            {
                Console.WriteLine($"Language file for '{lang}' not found. Defaulting to English.");
                ActiveMessages.Clear();
                foreach (var kvp in DefaultMessages)
                {
                    ActiveMessages[kvp.Key] = kvp.Value;
                }
                return;
            }

            LoadMessagesFromFile(langFilePath, ActiveMessages);
            MergeWithDefaults();
        }

        /// <summary>
        /// Loads messages from a JSON file into a target dictionary.
        /// </summary>
        private static void LoadMessagesFromFile(string filePath, Dictionary<string, string> target, bool isDefault = false)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var parsedJson = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);

                    if (parsedJson != null && parsedJson.TryGetValue("Messages", out var messages))
                    {
                        target.Clear();
                        foreach (var kvp in messages)
                        {
                            target[kvp.Key] = kvp.Value;
                        }
                    }
                }
                else if (isDefault)
                {
                    Console.WriteLine($"Default language file '{filePath}' is missing. No messages loaded.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading messages from '{filePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Ensures all missing keys in ActiveMessages are filled with DefaultMessages.
        /// </summary>
        private static void MergeWithDefaults()
        {
            foreach (var kvp in DefaultMessages)
            {
                if (!ActiveMessages.ContainsKey(kvp.Key))
                {
                    ActiveMessages[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Displays a message from the dictionary using a key.
        /// </summary>
        public static void ConsoleMessage(string key, int severity, params object[] args)
        {
            if (ActiveMessages.TryGetValue(key, out var message))
            {
                string formattedMessage = string.Format(message, args);
                LogMessage(formattedMessage, severity);
            }
            else
            {
                LogMessage($"Unknown message key: {key}", 3);
            }
        }

        /// <summary>
        /// Logs the message to the console and optionally to a file.
        /// </summary>
        private static void LogMessage(string message, int severity)
        {
            var logLevels = new Dictionary<int, (LogEventLevel level, ConsoleColor color)>
            {
                { 0, (LogEventLevel.Verbose, ConsoleColor.Magenta) },
                { 1, (LogEventLevel.Debug, ConsoleColor.Blue) },
                { 2, (LogEventLevel.Information, ConsoleColor.White) },
                { 3, (LogEventLevel.Warning, ConsoleColor.Yellow) },
                { 4, (LogEventLevel.Error, ConsoleColor.Red) },
                { 5, (LogEventLevel.Fatal, ConsoleColor.DarkRed) }
            };

            if (logLevels.TryGetValue(severity, out var logLevel))
            {
                Console.ForegroundColor = logLevel.color;
                Console.WriteLine($"[{logLevel.level}] {message}");
                Logger.TryGet(logLevel.level, LogArea.Visual)?.Log(null, $"[{logLevel.level}] {message}");

                if (Configuration.Instance.LogFile)
                {
                    WriteLogToFile(logLevel.level.ToString(), message);
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[Warning] Invalid severity level: {severity}");
            }

            Console.ResetColor();
        }

        /// <summary>
        /// Writes a message to a log file.
        /// </summary>
        private static void WriteLogToFile(string level, string message)
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Log/{DateTime.Now:yyyy-MM-dd}.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath) ?? string.Empty);

            using StreamWriter writer = File.AppendText(logFilePath);
            writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}");
        }
    }
}
