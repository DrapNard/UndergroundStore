using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

namespace UndergroundShop.Management
{
    /// <summary>
    /// Manages the application configuration settings using a singleton pattern.
    /// </summary>
    internal class Configuration
    {
        /// <summary>
        /// Gets the list of paths configured for the application.
        /// </summary>
        public string[] Path { get; private set; } = [];

        /// <summary>
        /// Gets the update channel for the application (e.g., Stable, Beta).
        /// </summary>
        public string Channel { get; private set; } = "Stable";

        /// <summary>
        /// Gets a value indicating whether debugging is enabled.
        /// </summary>
        public bool Debug { get; private set; }

        /// <summary>
        /// Gets a value indicating whether logging to a file is enabled.
        /// </summary>
        public bool LogFile { get; private set; }

        /// <summary>
        /// Gets the version of the application.
        /// </summary>
        public string Version { get; private set; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        /// <summary>
        /// Gets the language setting for the application.
        /// </summary>
        public string Lang { get; private set; } = "En";

        // Singleton instance, thread-safe with Lazy<T>
        private static readonly Lazy<Configuration> _instance = new(() => new Configuration());

        /// <summary>
        /// Gets the singleton instance of the <see cref="Configuration"/> class.
        /// </summary>
        public static Configuration Instance => _instance.Value;

        /// <summary>
        /// Private constructor to enforce the singleton pattern and load configuration.
        /// </summary>
        private Configuration()
        {
            LoadConfiguration();
        }

        /// <summary>
        /// Ensures the configuration instance is loaded and initialized.
        /// </summary>
        public static void Load() => _ = Instance;

        /// <summary>
        /// Loads the configuration settings from a JSON file or creates a default configuration if the file is missing.
        /// </summary>
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

            try
            {
                IConfigurationRoot config = new ConfigurationBuilder()
                    .AddJsonFile(confFile)
                    .Build();

                Path = config.GetSection("Path").Get<string[]>() ?? [];
                Channel = config.GetValue("Channel", "Stable") ?? "Stable";
                Debug = config.GetValue("Debug", false);
                LogFile = config.GetValue("LogFile", false);
                Lang = config.GetValue("Lang", "En") ?? "En";
                Version = config.GetValue("Version", Version) ?? Version;

                ValidateConfig();
            }
            catch (Exception)
            {
                // Log the error and terminate the application if the configuration is invalid
                // MessageManagement.ConsoleMessage($"Error loading configuration: {ex.Message}\n", 5);
                Environment.Exit(540);
            }
        }

        /// <summary>
        /// Creates a default configuration file with pre-defined values.
        /// </summary>
        /// <param name="confFile">The path to the configuration file to create.</param>
        private void CreateDefaultConfig(string confFile)
        {
            Channel = "Stable";
            Path = [];
            Debug = false;
            LogFile = false;
            Lang = "En";
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

            string jsonString = JsonConvert.SerializeObject(this, Formatting.Indented);
            string? dirPath = System.IO.Path.GetDirectoryName(confFile);
            Directory.CreateDirectory(dirPath ?? string.Empty);
            File.WriteAllText(confFile, jsonString);

            // Notify the user to configure the application and restart
            // MessageManagement.ConsoleMessage("Default config generated, please configure and restart.\n", 3);
        }

        /// <summary>
        /// Validates the configuration values and ensures they are initialized correctly.
        /// </summary>
        private void ValidateConfig()
        {
            Version ??= Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
            Path ??= [];
            Lang ??= "En";

            if (string.IsNullOrEmpty(Version))
            {
                // Terminate the application if the configuration is critically invalid
                // MessageManagement.ConsoleMessage("Config is corrupt. Exiting.\n", 5);
                Environment.Exit(540);
            }
        }
    }
}
