using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace UndergroundShop.Views.Main
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
