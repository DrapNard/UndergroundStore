using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;
using Avalonia;
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
            string ConfDir = System.IO.Path.Combine(ExeDir, "Config/");
            string ConfFile = System.IO.Path.Combine(ConfDir, "config.json");

            if (!File.Exists(ConfFile))
            {
                //MessageManagement.ConsoleMessage("Creating config" + "\n", 3);
                Channel = "Stable";
                Path = null;
                Debug = false;
                LogFile = false;
                Lang = "En";
                Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                string jsonString = JsonConvert.SerializeObject(this, Formatting.Indented); // Use Newtonsoft.Json for better formatting

                Directory.CreateDirectory(ConfDir);
                File.WriteAllText(ConfFile, jsonString);

                //MessageManagement.ConsoleMessage("Config generated please compleate and restart" + "\n", 3);
                return;
            }

            //MessageManagement.ConsoleMessage("Config Loading" + "\n", 2);
            IConfigurationRoot config = new ConfigurationBuilder()
              .AddJsonFile("Config/config.json")
              .Build();

            Path = config.GetValue<string[]>("Path");
            Channel = config.GetValue("Channel", "Stable");
            Debug = config.GetValue("Debug", false);
            LogFile = config.GetValue("LogFile", false);
            Lang = config.GetValue<string>("Lang");
            Version = config.GetValue<string>("Version");

            if (Version is null || Path is null || Lang is null)
            {
                MessageManagement.ConsoleMessage("Config Corrupt. Exiting.\n", 5);
                Environment.Exit(0);
            }
        }
    }
}
