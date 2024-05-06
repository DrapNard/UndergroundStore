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

        GetFile();
    }

    public static void GetFile()
    {
        string url = "";
        WebFile cdn = new WebFile(url);

        cdn.GetFileLengthAsync().ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                long size = task.Result;
                string msg = "Size of the File : " + size;
                MessageManagement.ConsoleMessage(msg, 2);
                MainViewModel.FileSize = msg;
            }
            else
            {
                // Handle any errors that might occur during download
                MessageManagement.ConsoleMessage("Error getting file size!", 3);
                MainViewModel.FileSize = "Unknowd";
            }
        });

        cdn.Download(AppDomain.CurrentDomain.BaseDirectory);
    }
}
