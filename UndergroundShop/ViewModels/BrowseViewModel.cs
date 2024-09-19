using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UndergroundShop.Core.Source;

namespace UndergroundShop.ViewModels
{
    public class BrowseViewModel : ObservableObject
    {
        public ObservableCollection<GameItem> BrowseItems { get; } = new ObservableCollection<GameItem>();

        public BrowseViewModel()
        {
            // Initialize your collections with some data
            BrowseItems.Add(new GameItem
            {
                Name = "Sample Fan Game",
                Description = "A sample fan game",
                Type = "Fan Game",
                DevelopmentTeamName = "Team Beta",
                Function = new Functionalities { Solo = true, Multiplayer = true, Achievement = false },
                Languages = new List<LanguageSupport>
                {
                    new LanguageSupport
                    {
                        LanguageCode = "EN",
                        Details = new List<LanguageDetails>
                        {
                            new LanguageDetails { UI = true, Audio = true, Subtitle = false }
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
                    Checksum = "def456",
                    Platform = "PC",
                    VersionLink = "https://example.com/version",
                    Host = "example.com"
                }
            });
        }
    }
}
