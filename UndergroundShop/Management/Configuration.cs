using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

namespace UndergroundShop.Management
{
    internal class Configuration
    {
        public string[] Path { get; private set; } = Array.Empty<string>();
        public string Channel { get; private set; } = "Stable";
        public bool Debug { get; private set; }
        public bool LogFile { get; private set; }
        public string Version { get; private set; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        public string Lang { get; private set; } = "En";

        // Singleton instance, thread-safe with Lazy<T>
        private static readonly Lazy<Configuration> _instance = new(() => new Configuration());

        public static Configuration Instance => _instance.Value;

        private Configuration() // Private constructor to enforce singleton pattern
        {
            LoadConfiguration();
        }

        public static void Load() => _ = Instance; // Trigger instance creation if not already loaded

        private void LoadConfiguration()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string confDir = System.IO.Path.Combine(exeDir, "Config");
            string confFile = System.IO.Path.Combine(confDir, "config.json");

            if (!File.Exists(confFile))
            {
                // If the config file does not exist, create it with default values
                CreateDefaultConfig(confFile);
                return;
            }

            // Load the existing configuration from the file
            try
            {
                IConfigurationRoot config = new ConfigurationBuilder()
                    .AddJsonFile(confFile)
                    .Build();

                Path = config.GetSection("Path").Get<string[]>() ?? Array.Empty<string>();
                Channel = config.GetValue("Channel", "Stable");
                Debug = config.GetValue("Debug", false);
                LogFile = config.GetValue("LogFile", false);
                Lang = config.GetValue("Lang", "En");
                Version = config.GetValue("Version", Version); // Use assembly version as fallback

                ValidateConfig();
            }
            catch (Exception ex)
            {
                // Log the error and exit if configuration cannot be loaded
                // MessageManagement.ConsoleMessage($"Error loading configuration: {ex.Message}\n", 5);
                Environment.Exit(540);
            }
        }

        private void CreateDefaultConfig(string confFile)
        {
            // Set default values
            Channel = "Stable";
            Path = Array.Empty<string>();
            Debug = false;
            LogFile = false;
            Lang = "En";
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

            // Serialize and save the default configuration
            string jsonString = JsonConvert.SerializeObject(this, Formatting.Indented);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(confFile) ?? string.Empty);
            File.WriteAllText(confFile, jsonString);

            // Prompt user to configure and restart
            // MessageManagement.ConsoleMessage("Default config generated, please configure and restart.\n", 3);
        }

        private void ValidateConfig()
        {
            // Ensure Version, Path, and Lang are properly initialized
            Version ??= Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
            Path ??= Array.Empty<string>();
            Lang ??= "En";

            // Exit if Version is still null (critical error)
            if (string.IsNullOrEmpty(Version))
            {
                // MessageManagement.ConsoleMessage("Config is corrupt. Exiting.\n", 5);
                Environment.Exit(540);
            }
        }
    }
}
