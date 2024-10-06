using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using UndergroundShop.Core.Source;

namespace UndergroundShop.UI.Models
{
    public class GameItem : ObservableObject
    {
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _type;
        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        private string _developmentTeamName;
        public string DevelopmentTeamName
        {
            get => _developmentTeamName;
            set => SetProperty(ref _developmentTeamName, value);
        }

        private Functionalities _function;
        public Functionalities Function
        {
            get => _function;
            set => SetProperty(ref _function, value);
        }

        private List<LanguageSupport> _languages;
        public List<LanguageSupport> Languages
        {
            get => _languages;
            set => SetProperty(ref _languages, value);
        }

        private List<SocialLink> _socialLinks;
        public List<SocialLink> SocialLinks
        {
            get => _socialLinks;
            set => SetProperty(ref _socialLinks, value);
        }

        private Picture _pictures;
        public Picture Pictures
        {
            get => _pictures;
            set => SetProperty(ref _pictures, value);
        }

        private Store _store;
        public Store Store
        {
            get => _store;
            set => SetProperty(ref _store, value);
        }

        public GameItem()
        {
            Languages = new List<LanguageSupport>();
            SocialLinks = new List<SocialLink>();
        }
    }
}
