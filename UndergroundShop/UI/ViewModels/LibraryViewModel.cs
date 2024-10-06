using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UndergroundShop.Core.Source;
using UndergroundShop.UI.Models;

namespace UndergroundShop.UI.ViewModels
{
    public class LibraryViewModel : ObservableObject
    {
        public ObservableCollection<GameItem> LibraryItems { get; } = new ObservableCollection<GameItem>();

        public LibraryViewModel()
        {
            // Initialize your collections with some data
            LibraryItems.Add(new GameItem
            {
                Name = "Sample Mod",
                Description = "A sample mod",
                Type = "Mod",
                DevelopmentTeamName = "Team Alpha",
                Function = new Functionalities { Solo = true, Multiplayer = false, Achievement = true },
                Languages = new List<LanguageSupport>
                {
                    new LanguageSupport
                    {
                        LanguageCode = "EN",
                        Details = new List<LanguageDetails>
                        {
                            new LanguageDetails { UI = true, Audio = true, Subtitle = true }
                        }
                    }
                },
                SocialLinks = new List<SocialLink>
                {
                    new SocialLink { Link = "https://example.com" }
                },
                Pictures = new Picture
                {
                    Thumbnail = "https://example.com/thumb.jpg",
                    LargeThumbnail = "https://example.com/largethumb.jpg",
                    Video = "https://example.com/video.mp4",
                    Ilustration = new List<string> { "https://example.com/illust1.jpg" }
                },
                Store = new Store
                {
                    Checksum = "abc123",
                    Platform = "PC",
                    VersionLink = "https://example.com/version",
                    Host = "example.com"
                }
            });
        }
    }
}
