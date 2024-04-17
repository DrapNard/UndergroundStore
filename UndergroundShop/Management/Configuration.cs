using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;

namespace UndergroundShop.Management
{
    internal class Configuration
    {
        public List<ConfigurationItem> Items { get; set; }

        public Configuration()
        {
            Items = new List<ConfigurationItem>();
        }

        public static Configuration LoadFromFile(string filePath)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(filePath)
                .Build();

            var config = configuration.Get<Configuration>();
            return config;
        }
    }

    public class ConfigurationItem
    {
        public string Type { get; set; }
        public string Version { get; set; }
        public string? Channel { get; set; }
        public string? Path { get; set; }
    }
}
