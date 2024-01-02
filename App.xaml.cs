using System.Windows;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Pokémon_Infinite_Fusion_Launcher
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>

    public sealed partial class App
	{

        public static string[]? Args = new string[0];

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length > 0)
            {
                Args = e.Args;
                return;
            }
        }
        public App()
		{
			
		}
	}
}
