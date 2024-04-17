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
        string url = "";
        Downloader downloader = new Downloader(url);

        downloader.GetFileLengthAsync().ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                string size = task.Result.ToString();
                Logger.TryGet(LogEventLevel.Fatal, LogArea.Control)?.Log(this, "Size of the File : " + size);
                MainViewModel.FileSize = size + "byte";
            }
            else
            {
                // Handle any errors that might occur during download
                Console.WriteLine("Error getting file size!");
                MainViewModel.FileSize = "Unknowd";
            }
        });
    }
}
