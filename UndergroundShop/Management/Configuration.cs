using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System;
using System.IO;
using System.Reflection;
using Avalonia.Logging;
using System.Diagnostics;

namespace UndergroundShop.Management
{
    internal class Configuration
    {
        public string[] Path { get; set; }
        public string Channel { get; set; }
        public bool Debug { get; set; }
        public string Version { get; set; }

        public Configuration()
        {
            string ExeDir = AppDomain.CurrentDomain.BaseDirectory;
            string ConfDir = System.IO.Path.Combine(ExeDir, "Config/");
            string ConfFile = System.IO.Path.Combine(ConfDir, "config.json");

            if (!File.Exists(ConfFile))
            {
                Console.WriteLine("Creating config" + "\n");
                Channel = "Stable";
                Path = null;
                Debug = false;
                Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                string jsonString = JsonSerializer.Serialize(this);

                Directory.CreateDirectory(ConfDir);
                File.WriteAllText(ConfFile, jsonString);

                Logger.TryGet(LogEventLevel.Information, LogArea.Control)?.Log(this, "Config generated please compleate and restart" + "\n");
                return;
            }

            Console.WriteLine("Config Loading" + "\n");
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile("Config/config.json")
                .Build();

            Path = config.GetValue<string[]>("Path");
            Channel = config.GetValue<string>("Channel");
            Debug = config.GetValue<bool>("Debug", false);
            Version = config.GetValue<string>("Version", null);

            Logger.TryGet(LogEventLevel.Information, LogArea.Control)?.Log(this, "Path : " + Path);
            Logger.TryGet(LogEventLevel.Information, LogArea.Control)?.Log(this, "Channel : " + Channel);
            Logger.TryGet(LogEventLevel.Information, LogArea.Control)?.Log(this, "Debug : " + Debug);
            Logger.TryGet(LogEventLevel.Information, LogArea.Control)?.Log(this, "Version : " + Version);

            if (Version == null)
            {
                Logger.TryGet(LogEventLevel.Fatal, LogArea.Control)?.Log(this, "Config Corrupt. Exiting." + "\n");
                Environment.Exit(0);
                return;
            }
        }
    }
}
