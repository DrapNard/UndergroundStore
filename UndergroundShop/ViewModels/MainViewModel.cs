
using Avalonia.Controls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace UndergroundShop.ViewModels;

public partial class MainViewModel : ViewModelBase, INotifyPropertyChanged
{
    public static string? FileSize { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
