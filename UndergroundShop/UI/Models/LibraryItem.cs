using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UndergroundShop.UI.Models
{
    public class LibraryItem : ObservableObject
    {
        private string _name;
        private string _description;
        private string _developmentTeamName;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string DevelopmentTeamName
        {
            get => _developmentTeamName;
            set => SetProperty(ref _developmentTeamName, value);
        }
    }
}