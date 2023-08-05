using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace Pokémon_Infinite_Fusion_Launcher
{
    /// <summary>
    /// Logique d'interaction pour InstallationOptionsDialog.xaml
    /// </summary>
    public partial class InstallationOptionsDialog : Window
    {
        public string selectedFolderPath;
        public bool InstallGraphicPack = true;
        Installer installer = new Installer();
        private HttpClient httpClient;

        public InstallationOptionsDialog()
        {
            InitializeComponent();
            InstallPath.Text = AppDomain.CurrentDomain.BaseDirectory;
            selectedFolderPath = AppDomain.CurrentDomain.BaseDirectory;
            installer.exeDirectory = selectedFolderPath;
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "GithubReleaseDownloader");
        }

        public async Task<long> GetFileSizeAsync(string url)
        {
            if (httpClient == null)
            {
                Console.WriteLine("Erreur : httpClient n'est pas initialisé.");
                return 0;
            }

            if (string.IsNullOrEmpty(url))
            {
                Console.WriteLine("Erreur : URL invalide.");
                return 0;
            }

            try
            {
                HttpResponseMessage response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"La requête n'a pas abouti avec un code d'état de succès : {response.StatusCode}");
                    return 0;
                }

                return response.Content.Headers.ContentLength ?? 0;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Erreur HTTP : {ex.Message}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur : {ex.Message}");
                return 0;
            }
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void BrowsInstallFolder_Click(object sender, RoutedEventArgs e)
        {
            // Créer un nouveau FolderBrowserDialog
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();

            // Afficher la boîte de dialogue de sélection du répertoire
            DialogResult result = folderBrowserDialog.ShowDialog();

            // Vérifier si l'utilisateur a cliqué sur OK
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // Obtenir le chemin d'accès au répertoire sélectionné
                selectedFolderPath = folderBrowserDialog.SelectedPath;
                InstallPath.Text = selectedFolderPath;
                installer.exeDirectory = selectedFolderPath;
            }
        }

        private async void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (GraphPackInstall.IsChecked == true)
            {
                InstallGraphicPack = true;
                long GameSize = await GetFileSizeAsync("https://github.com/infinitefusion/infinitefusion-e18/archive/refs/heads/releases.zip");
                long GraphSize = await GetFileSizeAsync("https://github.com/DrapNard/InfiniteFusion-Launcher/archive/refs/heads/SpritePack.zip");
                int GameSizeInstall = (int)GameSize;
                int GraphSizeInstall = (int)GraphSize;
                int sum = GameSizeInstall + GraphSizeInstall;
                string size = sum.ToString();
                //InstSize.Text = size;
            }
            else
            {
                InstallGraphicPack = false;
                long GameSize = await installer.GetFileSizeAsync("https://github.com/infinitefusion/infinitefusion-e18/archive/refs/heads/releases.zip");
                string GameSizeInstall = GameSize.ToString();
                //InstSize.Text = GameSizeInstall;
            }
        }
    }
}