using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UndergroundShop.UI.Models;

namespace UndergroundShop.ViewModels
{
    public class LibraryViewModel : ObservableObject
    {
        private ObservableCollection<LibraryItem> _libraryItems;

        public ObservableCollection<LibraryItem> LibraryItems
        {
            get => _libraryItems;
            set => SetProperty(ref _libraryItems, value);
        }

        public LibraryViewModel()
        {
            LibraryItems = new ObservableCollection<LibraryItem>
            {
                new LibraryItem
                {
                    Name = "Test Game",
                    Description = "A test game entry",
                    DevelopmentTeamName = "Test Team"
                }
            };
        }
    }
}