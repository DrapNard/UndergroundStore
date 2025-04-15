using System.Collections.Generic;

namespace UndergroundShop.Core.Source
{
    internal class GameDictionary
    {
    }

    public class GameInfo
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Type { get; set; }
        public required string DevelopementTeamName { get; set; }
        public required Functionalities Function { get; set; }
        public required List<LanguageSupport> Languages { get; set; }
    }

    public class Functionalities
    {
        public bool Solo { get; set; }
        public bool Multiplayer { get; set; }
        public bool Achievement { get; set; }
    }

    public class LanguageSupport
    {
        public required string LanguageCode { get; set; } // Use an enum for supported languages later
        public required List<LanguageDetails> Details { get; set; }
    }

    public class LanguageDetails
    {
        public bool UI { get; set; }
        public bool Audio { get; set; }
        public bool Subtitle { get; set; }
    }

    public class SocialLink
    {
        public required string Link { get; set; }
    }

    public class Picture
    {
        public required string Thumbnail { get; set; }
        public required string LargeThumbnail { get; set; }
        public required string Video { get; set; }
        public required List<string> Ilustration { get; set; }
    }

    public class Store
    {
        public required string Checksum { get; set; }
        public required string Platform { get; set; }
        public required string VersionLink { get; set; }
        public required string Host { get; set; }
    }
}
