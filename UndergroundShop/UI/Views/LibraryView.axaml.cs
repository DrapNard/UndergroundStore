using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace UndergroundShop.Views.Main
{
    public partial class LibraryView : UserControl
    {
        public LibraryView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
