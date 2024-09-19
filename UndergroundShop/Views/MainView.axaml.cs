using Avalonia.Controls;
using UndergroundShop.Management;
using UndergroundShop.ViewModels;
using System;


namespace UndergroundShop.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        Configuration.Load();
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
