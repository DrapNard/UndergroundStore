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
        WebFile cdn = new WebFile(url);

        cdn.GetFileLengthAsync().ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                long size = task.Result;
                string msg = "Size of the File : " + size;
                Logger.TryGet(LogEventLevel.Fatal, LogArea.Control)?.Log(this, msg);
                MainViewModel.FileSize = msg;
            }
            else
            {
                // Handle any errors that might occur during download
                Console.WriteLine("Error getting file size!");
                MainViewModel.FileSize = "Unknowd";
            }
        });

        cdn.Download(AppDomain.CurrentDomain.BaseDirectory);
    }
}
