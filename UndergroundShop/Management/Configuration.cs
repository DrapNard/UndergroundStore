using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace UndergroundShop.Management
{
    internal class Configuration
    {
        public string[]? Path { get; set; }
        public string? Channel { get; set; }
        public bool Debug { get; set; }
        public bool LogFile { get; set; } = false;
        public string? Version { get; set; }
        public string? Lang { get; set; }

        public static Configuration? Instance { get; private set; } // Singleton Instance

        private Configuration() // Private constructor to enforce singleton pattern
        {
            LoadConfiguration();
        }

        public static void Load() // Static method to access and load configuration
        {
            if (Instance == null)
            {
                Instance = new Configuration(); // Create instance on first call
            }
        }

        private void LoadConfiguration() // Helper method for loading from file (private)
        {
            string ExeDir = AppDomain.CurrentDomain.BaseDirectory;
            string ConfDir = System.IO.Path.Combine(ExeDir, "Config");
            string ConfFile = System.IO.Path.Combine(ConfDir, "config.json");

            if (!File.Exists(ConfFile))
            {
                // If the config file does not exist, create it with default values
                Channel = "Stable";
                Path = Array.Empty<string>(); // Initialize empty array
                Debug = false;
                LogFile = false;
                Lang = "En";
                Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();

                string jsonString = JsonConvert.SerializeObject(this, Formatting.Indented); // Use Newtonsoft.Json for better formatting

                Directory.CreateDirectory(ConfDir);
                File.WriteAllText(ConfFile, jsonString);

                // Config generated, prompt user to complete and restart
                // MessageManagement.ConsoleMessage("Config generated please complete and restart\n", 3);
                return;
            }

            // Load the existing configuration from the file
            // MessageManagement.ConsoleMessage("Config Loading\n", 2);
            IConfigurationRoot config = new ConfigurationBuilder()
              .AddJsonFile(ConfFile) // Use full path
              .Build();

            Path = config.GetSection("Path").Get<string[]>() ?? Array.Empty<string>();
            Channel = config.GetValue("Channel", "Stable");
            Debug = config.GetValue("Debug", false);
            LogFile = config.GetValue("LogFile", false);
            Lang = config.GetValue<string>("Lang") ?? "En";
            Version = config.GetValue<string>("Version") ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString();

            // Validate the loaded configuration and set defaults if necessary
            if (Version is null || Path is null || Lang is null)
            {
                // Ensure the values are set to default if any are missing
                Path = Path ?? Array.Empty<string>();
                Lang = Lang ?? "En";
                Version = Version ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            }

            // If critical values are still missing, exit the application
            if (Version is null)
            {
                // MessageManagement.ConsoleMessage("Config Corrupt. Exiting.\n", 5);
                Environment.Exit(540);
            }
        }
    }
}
