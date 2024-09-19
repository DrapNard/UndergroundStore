using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UndergroundShop.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public ObservableCollection<TabItemViewModel> TabItems { get; } = new ObservableCollection<TabItemViewModel>();

        private TabItemViewModel _selectedTabItem;
        public TabItemViewModel SelectedTabItem
        {
            get => _selectedTabItem;
            set => SetProperty(ref _selectedTabItem, value);
        }

        public MainViewModel()
        {
            TabItems.Add(new TabItemViewModel
            {
                Header = "User Interface",
                Icon = "/Assets/icons/ui.png", // Update with the correct icon path
                Content = new HomeViewModel()
            });

            TabItems.Add(new TabItemViewModel
            {
                Header = "Input",
                Icon = "/Assets/icons/input.png", // Update with the correct icon path
                Content = new LibraryViewModel()
            });

            TabItems.Add(new TabItemViewModel
            {
                Header = "Keyboard Hotkeys",
                Icon = "/Assets/icons/keyboard.png", // Update with the correct icon path
                Content = new BrowseViewModel()
            });

            TabItems.Add(new TabItemViewModel
            {
                Header = "System",
                Icon = "/Assets/icons/system.png", // Update with the correct icon path
                Content = new SettingsViewModel()
            });

            // Add more tabs as needed

            SelectedTabItem = TabItems[0]; // Set default tab
        }
    }

    public class TabItemViewModel : ObservableObject
    {
        public string Header { get; set; }
        public string Icon { get; set; }
        public object Content { get; set; }
    }
}
