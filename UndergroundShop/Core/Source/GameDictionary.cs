using System.Collections.Generic;

namespace UndergroundShop.Core.Source
{
    internal class GameDictionary
    {
    }

    public class GameInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string DevelopementTeamName { get; set; }
        public Functionalities Function { get; set; }
        public List<LanguageSupport> Languages { get; set; }
    }

    public class Functionalities
    {
        public bool Solo { get; set; }
        public bool Multiplayer { get; set; }
        public bool Achievement { get; set; }
    }

    public class LanguageSupport
    {
        public string LanguageCode { get; set; } // Use an enum for supported languages later
        public List<LanguageDetails> Details { get; set; }
    }

    public class LanguageDetails
    {
        public bool UI { get; set; }
        public bool Audio { get; set; }
        public bool Subtitle { get; set; }
    }

    public class SocialLink
    {
        public string Link { get; set; }
    }

    public class Picture
    {
        public string Thumbnail { get; set; }
        public string LargeThumbnail { get; set; }
        public string Video { get; set; }
        public List<string> Ilustration { get; set; }
    }

    public class Store
    {
        public string Checksum { get; set; }
        public string Platform { get; set; }
        public string VersionLink { get; set; }
        public string Host { get; set; }
    }
}
