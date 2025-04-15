using Avalonia.Media.Imaging;
using System.IO;
using System;
using System.Collections.ObjectModel;

namespace UndergroundShop.ViewModels
{
    /// <summary>
    /// ViewModel principal de l'application qui gère la navigation entre les différents onglets.
    /// </summary>
    public class MainViewModel
    {
        /// <summary>
        /// Collection des éléments d'onglet disponibles dans la barre latérale.
        /// </summary>
        public ObservableCollection<TabItemViewModel> TabItems { get; } = new ObservableCollection<TabItemViewModel>();

        /// <summary>
        /// L'élément d'onglet actuellement sélectionné.
        /// </summary>
        public TabItemViewModel SelectedTabItem { get; set; } = null!;

        /// <summary>
        /// Initialise une nouvelle instance de la classe MainViewModel.
        /// </summary>
        public MainViewModel()
        {
            // Initialiser les onglets de l'application
            TabItems.Add(new TabItemViewModel { Header = "Accueil", Icon = null! });
            TabItems.Add(new TabItemViewModel { Header = "Bibliothèque", Icon = null! });
            
            // Sélectionner le premier onglet par défaut
            if (TabItems.Count > 0)
                SelectedTabItem = TabItems[0];
        }
    }

    /// <summary>
    /// ViewModel représentant un élément d'onglet dans la barre latérale.
    /// </summary>
    public class TabItemViewModel
    {
        /// <summary>
        /// Titre de l'onglet.
        /// </summary>
        public string Header { get; set; } = string.Empty;

        /// <summary>
        /// Icône de l'onglet.
        /// </summary>
        public Bitmap? Icon { get; set; }

        /// <summary>
        /// Contenu de l'onglet.
        /// </summary>
        public object? Content { get; set; }
    }
}