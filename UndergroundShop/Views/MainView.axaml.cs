using Avalonia.Controls;
using Avalonia.Logging;
using UndergroundShop.Management;
using UndergroundShop.ViewModels;
using System;


namespace UndergroundShop.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        Logger.TryGet(LogEventLevel.Fatal, LogArea.Control)?.Log(this, "Avalonia Infrastructure");
        InitializeComponent();

        GetFile();
    }

    public void GetFile()
    {
        string url = "https://github.com/DrapNard/InfiniteFusion-Launcher/releases/download/1.7.2/Pokemon.Infinite.Fusion.Launcher.Setup.Windows.exe";
        Downloader downloader = new Downloader(url);

        downloader.GetFileLengthAsync().ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                long size = task.Result;
                MainViewModel.FileSize = size.ToString();
                Logger.TryGet(LogEventLevel.Fatal, LogArea.Control)?.Log(this, "Size of the File : " + size);
            }
            else
            {
                // Handle any errors that might occur during download
                Console.WriteLine("Error getting file size!");
            }
        });
    }
}
