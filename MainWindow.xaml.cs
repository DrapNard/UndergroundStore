using Discord;
using Discord.WebSocket;
using DiscordRPC;
using Hardcodet.Wpf.TaskbarNotification;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using Window = System.Windows.Window;

namespace Pokémon_Infinite_Fusion_Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        public bool install;
        private string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        Installer InstallerInstance = new Installer();
        private TaskbarIcon taskbarIcon;
        private DiscordRpcClient client;
        private ulong channelId = 302158319886532618;
        private TOKEN TOKEN = new TOKEN();

        string configFilePath = "config.json";
        Configuration config;

        public MainWindow()
        {
            InitializeComponent();
            Initialisation();
        }

        private void FirstInitialisation()
        {
            Install_Update_Play.Style = (Style)FindResource("InstallImageButtonStyle");
            install = true;
            News.Width = 730;
            GameVer.Content = "Not Installed";
            UninstallBtn.IsEnabled = false;
            RepairBtn.IsEnabled = false;
            GamePathBtn.IsEnabled = false;
            InstSpritePack.IsEnabled = false;
            GamePathBtnBlock.Visibility = Visibility.Visible;
            GamePathBtnBlockToolTip.Visibility = Visibility.Visible;
            UnInstallRepairBtnBlock.Visibility = Visibility.Visible;
            UnInstallRepairBtnBlockTooltip.Visibility = Visibility.Visible;
            InsSpritePackBtnBlock.Visibility = Visibility.Visible;
            InsSpritePackBtnBlockToolTip.Visibility= Visibility.Visible;
        }

        private async void Initialisation()
        {
            config = Configuration.Load(configFilePath);
            config.Save(configFilePath);
            Main.Visibility = Visibility.Hidden;

            client = new DiscordRpcClient("1136234506857226410");
            client.Initialize();

            TaskBarInitialize();

            if (App.Args.Length > 0)
            {
                for (int i = 0; i < App.Args.Length; i++)
                {
                    string arg = App.Args[i];

                    if (arg == "{f6bab8a7-1324-4221-85e2-4041ca01b96f}")
                    {
                        await InstallerInstance.UpdaterUpdater(this);
                    }

                    if (arg == "-DirectPlay")
                    {
                        if (i + 1 < App.Args.Length)
                        {
                            string DirectPlayOptions = App.Args[i + 1];

                            if (DirectPlayOptions == "Preloaded")
                            {
                                Hide();
                                await Install_Update_Play_Action(true);
                            }
                            else
                            {
                                Hide();
                                await Install_Update_Play_Action(false);
                            }

                            i++;
                        }
                        else
                        {
                            Hide();
                            await Install_Update_Play_Action(false);
                        }
                    }
                }
            }

            if (config.UID == null || config.UID == 0)
            {
                config.UID = Configuration.GenerateUserId();
                config.Save(configFilePath);
                string applicationPath = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(applicationPath);
                Application.Current.Shutdown();
            }

            await Updater();

            LaunchVer.Content = config.Version;
            Main.Visibility = Visibility.Hidden;
            AlternateLauncherCheckBox.IsChecked = config.AlternateLauncherEnable;
            ConsoleCheckBox.IsChecked = config.EnableConsole;
            TabManager(0);

            DiscordBot discordBot = new DiscordBot(channelId, TOKEN.token, this.News);
            discordBot.Initialize();

            string GameDir;
            string GameFile;

            if (config.GamePath != null)
            {
                GameDir = Path.Combine(config.GamePath, "InfiniteFusion");
                GameFile = Path.Combine(GameDir, "Game.exe");
            }
            else
            {
                GameDir = exeDirectory;
                GameFile = exeDirectory;
            }

            if (config.GamePath != null && Directory.Exists(GameDir) && File.Exists(GameFile))
            {
                Install_Update_Play.Style = (Style)FindResource("PlayImageButtonStyle");
                Install_Update_Play.Content = "Play";
                GameVer.Content = config.GameVersion;
                install = false;
                News.Width = 640;
            }
            else
            {
                FirstInitialisation();
            }

            if (config.CloseMode == "SysTray")
            {
                config.CloseMode = "SysTray";
                MinimizeSysTray.IsChecked = true;
                CloseApp.IsChecked = false;
            }
            else if (config.CloseMode == "Close")
            {
                config.CloseMode = "Close";
                MinimizeSysTray.IsChecked = false;
                CloseApp.IsChecked = true;
            }

            if (!config.EnableConsole || config.EnableConsole == null)
            {
                config.EnableConsole = false;
                ConsoleManager.Hide();
            }
            else
            {
                config.EnableConsole = true;
                ConsoleManager.Show();
            }

            string SaveFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "infinitefusion");

            if (!Directory.Exists(SaveFolder))
            {
                SaveBtn.IsEnabled = false;
                SaveBtnBlock.Visibility = Visibility.Visible;
                SaveBtnBlockToolTip.Visibility = Visibility.Visible;
            }

            DiscordRpc("On the Launcher of Infinite Fusion", "Idle");

            Main.Visibility = Visibility.Visible;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            config.Save(configFilePath);
        }
        private void TaskBarInitialize()
        {
            taskbarIcon = new TaskbarIcon();
            taskbarIcon.IconSource = new BitmapImage(new Uri("pack://application:,,,/icon.ico"));
            taskbarIcon.ToolTipText = "Pokémon Infinite Fusion Launcher";

            var contextMenu = new ContextMenu();

            var menuItemLauncher = new MenuItem();
            menuItemLauncher.Header = "Pokémon Infinite Fusion Launcher";
            menuItemLauncher.Click += menuItemLauncher_Click;
            contextMenu.Items.Add(menuItemLauncher);

            var menuItemWiki = new MenuItem();
            menuItemWiki.Header = "Wiki";
            menuItemWiki.Click += MenuItemWiki_Click; // Gérer l'événement clic du menu item 1
            contextMenu.Items.Add(menuItemWiki);

            var menuItemCalculator = new MenuItem();
            menuItemCalculator.Header = "Calculator";
            menuItemCalculator.Click += MenuItemCalculator_Click; // Gérer l'événement clic du menu item 2
            contextMenu.Items.Add(menuItemCalculator);

            var menuItemQuit = new MenuItem();
            menuItemQuit.Header = "Exit";
            menuItemQuit.Click += MenuItemQuit_Click; // Gérer l'événement clic du menu item 2
            contextMenu.Items.Add(menuItemQuit);

            taskbarIcon.ContextMenu = contextMenu;

            // Associez les événements pour gérer les actions de clic
            taskbarIcon.TrayMouseDoubleClick += TaskbarIcon_TrayMouseDoubleClick;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Déplacer la fenêtre lorsque le titre bar est cliqué
            if (e.ClickCount == 1)
            {
                this.DragMove();
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (config.CloseMode == "SysTray")
            {
                config.CloseMode = "SysTray";
                config.Save(configFilePath);
                Hide();
            }
            else
            {
                config.CloseMode = "Close";
                config.Save(configFilePath);
                Application.Current.Shutdown();
            }
        }

        private void MinimazeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private async void OptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (OptionGrid.Visibility == Visibility.Visible)
            {
                DisiableOtherButton.Visibility = Visibility.Collapsed;
                OptionsExitBtn.Visibility = Visibility.Collapsed;
                // Cacher le menu d'options et appliquer l'animation de flou
                DoubleAnimation blurAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.3));
                BlurEffect.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);

                DoubleAnimation fadeOutAnimation = (DoubleAnimation)FindResource("FadeOutAnimation");
                await ApplyAnimationAsync(OptionGrid, fadeOutAnimation, true);

            }
            else
            {
                DisiableOtherButton.Visibility = Visibility.Visible;
                OptionsExitBtn.Visibility = Visibility.Visible;
                // Afficher le menu d'options et appliquer l'animation de flou
                DoubleAnimation blurAnimation = new DoubleAnimation(10, TimeSpan.FromSeconds(0.3));
                BlurEffect.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);

                OptionGrid.Visibility = Visibility.Visible;

                DoubleAnimation fadeInAnimation = (DoubleAnimation)FindResource("FadeInAnimation");
                OptionGrid.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
            }
        }
        private void MenuItemWiki_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://infinitefusion.fandom.com/wiki/Pok%C3%A9mon_Infinite_Fusion_Wiki")
            {
                UseShellExecute = true
            });
        }

        private void menuItemLauncher_Click(object sender, RoutedEventArgs e)
        {
            Show();
            Updater();
        }

        private void MenuItemCalculator_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://aegide.gitlab.io/")
            {
                UseShellExecute = true
            });
        }

        private void MenuItemQuit_Click(object sender, RoutedEventArgs e)
        {
            config.CloseMode = "SysTray";
            config.Save(configFilePath);
            Application.Current.Shutdown();
        }

        private void TaskbarIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            Show();
        }

        private void Discord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://discord.gg/infinitefusion")
            {
                UseShellExecute = true
            });
        }

        private void Wiki_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://infinitefusion.fandom.com/wiki/Pok%C3%A9mon_Infinite_Fusion_Wiki")
            {
                UseShellExecute = true
            });
        }

        private void Calcutor_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://aegide.gitlab.io/")
            {
                UseShellExecute = true
            });
        }

        private void Reddit_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.reddit.com/r/PokemonInfiniteFusion/")
            {
                UseShellExecute = true
            });
        }

        private void Pokecommunity_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://play.pokeathlon.com/")
            {
                UseShellExecute = true
            });
        }

        private void FusionDex_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://if.daena.me/")
            {
                UseShellExecute = true
            });
        }

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/DrapNard/InfiniteFusion-Launcher")
            {
                UseShellExecute = true
            });
        }

        private async void InstSpritePack_Click(object sender, RoutedEventArgs e)
        {
            DisiableOtherButton.Visibility = Visibility.Collapsed;
            OptionsExitBtn.Visibility = Visibility.Collapsed;
            // Cacher le menu d'options et appliquer l'animation de flou
            DoubleAnimation blurAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.3));
            BlurEffect.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);

            DoubleAnimation fadeOutAnimation = (DoubleAnimation)FindResource("FadeOutAnimation");
            await ApplyAnimationAsync(OptionGrid, fadeOutAnimation, true);

            Install_Update_Play.IsEnabled = false;
            UninstallBtn.IsEnabled = false;
            RepairBtn.IsEnabled = false;
            InstSpritePack.IsEnabled = false;
            ExitBlock.Visibility = Visibility.Visible;
            await InstallerInstance.GraphicPackInstall(this);
        }

        private async void Install_Update_Play_Click(object sender, RoutedEventArgs e)
        {
            if (!config.AlternateLauncherEnable || config.AlternateLauncherEnable == null)
            {
                Install_Update_Play_Action(false);
            }
            else
            {
                Install_Update_Play_Action(true);
            }
        }

        private async Task Install_Update_Play_Action(bool Preloaded)
        {
            await InstallerInstance.UpdateChecker(this, "infinitefusion", "infinitefusion-e18", false);

            if (install)
            {
                // Créer une boîte de dialogue personnalisée (peut être un UserControl ou une autre fenêtre WPF)
                InstallationOptionsDialog optionsDialog = new InstallationOptionsDialog();

                // Afficher la boîte de dialogue de manière modale
                bool? result = optionsDialog.ShowDialog();

                // Vérifier si l'utilisateur a cliqué sur le bouton OK
                if (result == true)
                {
                        string SelectFolder = optionsDialog.selectedFolderPath;
                        SystemSounds.Beep.Play();
                        ExitBlock.Visibility = Visibility.Visible;
                        await InstallerInstance.Install(this, SelectFolder, false);
                    
                }
                return;
            }
            if (!install)
            {
                await GamePlay(Preloaded);
            }
        }

        private async Task GamePlay(bool Preloaded)
        {
            string GameExecution;

            if (!Preloaded)
            {
                GameExecution = Path.Combine(config.GamePath, "InfiniteFusion/Game.exe");
            }
            else
            {
                GameExecution = Path.Combine(config.GamePath, "InfiniteFusion/Game-preloaded.exe");
            }

            Hide();

            Process process = new Process();
            process.StartInfo.FileName = GameExecution;

            DiscordRpc("Playing Pokémon Infinite Fusion", "In Game");
            try
            {
                // Démarrer le processus
                process.Start();

                // Attendre que le logiciel soit lancé
                process.WaitForInputIdle();

                Install_Update_Play.IsEnabled = false;

                // Attendre la fin du processus (si nécessaire)
                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors du démarrage du logiciel : " + ex.Message);
            }
            finally
            {
                // Libérer les ressources du processus
                process.Dispose();
                DiscordRpc("On the Launcher of Infinite Fusion", "Idle");
                Install_Update_Play.IsEnabled = true;
                Show();

                string SaveFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "infinitefusion");

                if (!Directory.Exists(SaveFolder))
                {
                    SaveBtn.IsEnabled = false;
                    SaveBtnBlock.Visibility = Visibility.Visible;
                    SaveBtnBlockToolTip.Visibility = Visibility.Visible;
                }
                else
                {
                    SaveBtn.IsEnabled = true;
                    SaveBtnBlock.Visibility = Visibility.Collapsed;
                    SaveBtnBlockToolTip.Visibility = Visibility.Collapsed;
                }
            }
        }

        public void DiscordRpc(string Details, string State)
        {
            var presence = new RichPresence()
            {
                Details = Details,
                State = State,
                Assets = new Assets()
                {
                    LargeImageKey = "https://cdn2.steamgriddb.com/file/sgdb-cdn/logo_thumb/286128ebe26db08577503bea21351778.png",
                    LargeImageText = "Pokémon Infinite Fusion",
                    SmallImageKey = "https://cdn2.steamgriddb.com/file/sgdb-cdn/icon_thumb/faaebcdfe2773845e540b7981ee4a09b.png",
                    SmallImageText = "Petite image"
                }
            };

            // Mettre à jour le statut
            client.SetPresence(presence);
        }

        private async Task Updater()
        {
            await InstallerInstance.UpdateChecker(this, "infinitefusion", "infinitefusion-e18", false);
            StartUpdater();
            //await UD.LauncherUpdateChecker("DrapNard", "InfiniteFusion-Launcher");
        }

        public async Task StartUpdater()
        {
            string baseDirectory = Path.Combine(exeDirectory, "Updater.exe");

            // Créez un processus pour exécuter le programme externe
            Process externalProcess = new Process();
            externalProcess.StartInfo.FileName = baseDirectory;

            // Démarrez le processus
            externalProcess.Start();

            // Attendez que le processus externe se termine
            externalProcess.WaitForExit();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string SaveFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "infinitefusion");

            Process.Start("explorer.exe", SaveFolder);
        }

        private void MinimizeSysTray_Checked(object sender, RoutedEventArgs e)
        {
            if (MinimizeSysTray.IsChecked == true)
            {
                CloseApp.IsChecked = false;
            }
            else
            {
                CloseApp.IsChecked = true;
            }
            // Désactiver l'autre CheckBox
            config.CloseMode = "SysTray";
        }

        private void CloseApp_Checked(object sender, RoutedEventArgs e)
        {
            if (CloseApp.IsChecked == true)
            {
                MinimizeSysTray.IsChecked = false;
            }
            else
            {
                MinimizeSysTray.IsChecked = true;
            }
            // Désactiver l'autre CheckBox
            config.CloseMode = "Close";
        }

        public void InstPlayButtonStyle(bool Install)
        {
            if (Install)
            {
                Install_Update_Play.Style = (Style)FindResource("PlayImageButtonStyle");
            }
            else
            {
                Install_Update_Play.Style = (Style)FindResource("InstallImageButtonStyle");
            }
        }

        private void TabManager(int TabIndex)
        {
            if (TabIndex == 0)
            {
                TroubleshootTabBtn.IsEnabled = true;
                GeneralTabBtn.IsEnabled = false;
                GeneralTabBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3E3E3E"));
                TroubleshootTabBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF333333"));
            }
            else if (TabIndex == 1)
            {
                TroubleshootTabBtn.IsEnabled = false;
                GeneralTabBtn.IsEnabled = true;
                TroubleshootTabBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3E3E3E"));
                GeneralTabBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF333333"));
            }
        }

        private async void GeneralTabBtn_Click(object sender, RoutedEventArgs e)
        {
            GeneralGrid.Visibility = Visibility.Visible;
            TroubleshootGrid.Visibility = Visibility.Collapsed;
            TabManager(0);
        }

        private async void TroubleshootTabBtn_Click(object sender, RoutedEventArgs e)
        {
            TroubleshootGrid.Visibility = Visibility.Visible;
            GeneralGrid.Visibility = Visibility.Collapsed;
            TabManager(1);
        }

        private void ConsoleCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (ConsoleCheckBox.IsChecked == true)
            {
                config.EnableConsole = true;
                ConsoleManager.Show();
                Console.WriteLine("Console Enable");
            }
            else
            {
                config.EnableConsole = false;
                ConsoleManager.Hide();
            }
        }

        private void AlternateLauncherCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (AlternateLauncherCheckBox.IsChecked == true)
            {
                config.AlternateLauncherEnable = true;
            }
            else
            {
                config.AlternateLauncherEnable = false;
            }
            config.Save(configFilePath);
        }

        private async void RepairBtn_Click(object sender, RoutedEventArgs e)
        {
            DisiableOtherButton.Visibility = Visibility.Collapsed;
            OptionsExitBtn.Visibility = Visibility.Collapsed;
            // Cacher le menu d'options et appliquer l'animation de flou
            DoubleAnimation blurAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.3));
            BlurEffect.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);

            DoubleAnimation fadeOutAnimation = (DoubleAnimation)FindResource("FadeOutAnimation");
            await ApplyAnimationAsync(OptionGrid, fadeOutAnimation, true);

            Install_Update_Play.IsEnabled = false;
            UninstallBtn.IsEnabled = false;
            RepairBtn.IsEnabled = false;
            ExitBlock.Visibility = Visibility.Visible;
            await InstallerInstance.UpdateChecker(this, "infinitefusion", "infinitefusion-e18", true);
        }

        private async void UninstallBtn_Click(object sender, RoutedEventArgs e)
        {
            DoubleAnimation blurAnimation = new DoubleAnimation(10, TimeSpan.FromSeconds(0.3));
            BlurEffectOptions.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);
            UninstallConfirmation.Visibility = Visibility.Visible;

            DoubleAnimation fadeInAnimation = (DoubleAnimation)FindResource("FadeInAnimation");
            UninstallConfirmation.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
        }

        private async void NoUninstallConfirmation_Click(object sender, RoutedEventArgs e)
        {
            DoubleAnimation blurAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.3));
            BlurEffectOptions.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);
            DoubleAnimation fadeOutAnimation = (DoubleAnimation)FindResource("FadeOutAnimation");
            await ApplyAnimationAsync(UninstallConfirmation, fadeOutAnimation, true);
        }

        private async Task ApplyAnimationAsync(UIElement element, DoubleAnimation animation, bool Colapse)
        {
            element.Visibility = Visibility.Visible;
            element.BeginAnimation(UIElement.OpacityProperty, animation);

            // Attendre que l'animation se termine.
            await Task.Delay(animation.Duration.TimeSpan);
            if (Colapse)
            {
                element.Visibility = Visibility.Collapsed;
            }
        }

        private async void YesUninstallConfirmation_Click(object sender, RoutedEventArgs e)
        {
            DoubleAnimation blurAnimation = new DoubleAnimation(10, TimeSpan.FromSeconds(0.3));
            BlurEffectUnistall.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);

            AppBlock.Visibility = Visibility.Visible;

            Task.Run(() => UnistallDeleteMultiThread());
        }

        private async void UnistallDeleteMultiThread()
        {
            string gamePath = Path.Combine(config.GamePath, "InfiniteFusion");
            foreach (string SubFolder in Directory.GetDirectories(gamePath))
            {
                Directory.Delete(SubFolder, true); // true indique de supprimer récursivement

                foreach (string fichier in Directory.GetFiles(gamePath))
                {
                    File.Delete(fichier);
                }
            }
            Directory.Delete(gamePath, true);
            config.GamePath = null;
            config.GameVersion = null;
            config.Save(configFilePath);

            MessageBoxResult result = MessageBox.Show("The launcher will restart when this window is closed", "Uninstall Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            string applicationPath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(applicationPath);
            Application.Current.Shutdown();
        }

        private async void OptionsExitBtn_Click(object sender, RoutedEventArgs e)
        {
            DisiableOtherButton.Visibility = Visibility.Collapsed;
            OptionsExitBtn.Visibility = Visibility.Collapsed;
            // Cacher le menu d'options et appliquer l'animation de flou
            DoubleAnimation blurAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.3));
            BlurEffect.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);

            DoubleAnimation fadeOutAnimation = (DoubleAnimation)FindResource("FadeOutAnimation");
            await ApplyAnimationAsync(OptionGrid, fadeOutAnimation, true);
        }

        private void GamePathBtn_Click(object sender, RoutedEventArgs e)
        {
            string SaveFolder = Path.Combine(config.GamePath, "InfiniteFusion");

            Process.Start("explorer.exe", SaveFolder);
        }
    }

    public class Configuration
    {
        public string CloseMode { get; set; }
        public string Version { get; set; }
        public string GamePath { get; set; }
        public string GameVersion { get; set; }
        public bool AlternateLauncherEnable { get; set; }
        public bool EnableConsole { get; set; }
        public long UID { get; set; }

        public static Configuration Load(string filePath)
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<Configuration>(json);
            }

            return new Configuration(); // Default values if the file doesn't exist
        }

        public void Save(string filePath)
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }


        public static long GenerateUserId()
        {
            // Obtenez le nom de l'ordinateur
            string machineName = Environment.MachineName;
            // Utiliser SHA256 pour créer un hash unique à partir du nom de l'ordinateur
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineName));
                long hashedComputerName = BitConverter.ToInt64(hashBytes, 0);

                // Utiliser l'horodatage actuel comme graine pour Random
                long seed = DateTime.Now.Ticks;
                Random random = new Random((int)seed);

                // Obtenir une partie aléatoire sous forme d'entier long
                long randomNumber = random.Next();

                // Combinaison du nom de l'ordinateur hashé et de la partie aléatoire
                long uid = Math.Abs(hashedComputerName ^ randomNumber);

                return uid;
            }
        }

    }

    // Initialisation du client Discord
    public class DiscordBot
    {
        private DiscordSocketClient discordClient;
        private ulong channelId;
        private TextBox News; // Change the type to TextBox
        private string TOKEN;

        public DiscordBot(ulong channelId, string token, TextBox news) // Change the type to TextBox
        {
            this.channelId = channelId;
            this.TOKEN = token;
            this.News = news;
        }

        public async Task Initialize()
        {
            discordClient = new DiscordSocketClient();
            discordClient.Log += LogMessage;
            discordClient.Ready += DiscordClientReady;
            discordClient.MessageReceived += DiscordClientMessageReceived;

            // Connect to Discord using a valid bot token
            await discordClient.LoginAsync(TokenType.Bot, TOKEN);
            await discordClient.StartAsync();
        }

        private Task LogMessage(LogMessage log)
        {
            // Handle Discord log messages here
            return Task.CompletedTask;
        }

        private async Task DiscordClientReady()
        {
            // The Discord client is ready, you can now retrieve and display past messages
            await FetchAndDisplayPastMessages();
        }

        private async Task FetchAndDisplayPastMessages()
        {
            try
            {
                // Get the text channel specified by its ID
                var channel = discordClient.GetChannel(channelId) as ITextChannel;

                // Retrieve past messages in the channel
                var messages = await channel.GetMessagesAsync().FlattenAsync();

                // Display past messages in the TextBox
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var message in messages)
                    {
                        string formattedMessage = FormatDiscordMessage(message);
                        News.AppendText(formattedMessage + Environment.NewLine + Environment.NewLine);
                    }
                });
            }
            catch (Exception ex)
            {
                // Display an error if retrieving past messages fails
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBoxResult result = System.Windows.MessageBox.Show(ex.Message, "Message Retrieval Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    News.AppendText("Message Retrieval Failed: " + ex.Message + Environment.NewLine);
                });
            }
        }

        private async Task DiscordClientMessageReceived(SocketMessage message)
        {
            if (message.Channel.Id == channelId)
            {
                string formattedMessage = FormatDiscordMessage(message);

                // Display the formatted message in the TextBox
                Application.Current.Dispatcher.Invoke(() =>
                {
                    News.AppendText(formattedMessage + Environment.NewLine);
                });
            }
            await Task.CompletedTask;
        }

        private string FormatDiscordMessage(IMessage message)
        {
            string formattedMessage = $"[{message.Author.Username}] {message.Content}";

            // Check if it's a user message to access mentioned users
            if (message is IUserMessage userMessage)
            {
                // Process user mentions
                foreach (var userMention in userMessage.MentionedUserIds)
                {
                    formattedMessage = formattedMessage.Replace($"<@{userMention}>", "@" + userMention);
                }

                // Process role mentions
                foreach (var roleMention in userMessage.MentionedRoleIds)
                {
                    formattedMessage = formattedMessage.Replace($"<@&{roleMention}>", "@" + roleMention);
                }
            }

            // Process links and images (if any)
            foreach (var attachment in message.Attachments)
            {
                if (attachment.Url.EndsWith(".png") || attachment.Url.EndsWith(".jpg") || attachment.Url.EndsWith(".jpeg") || attachment.Url.EndsWith(".gif"))
                {
                    // Create an Image control and load the image from the URL
                    BitmapImage image = new BitmapImage(new Uri(attachment.Url));
                    System.Windows.Controls.Image imageControl = new System.Windows.Controls.Image
                    {
                        Source = image
                    };

                    // Add the image to the TextBox
                    News.AppendText(imageControl + Environment.NewLine);
                }
                else
                {
                    // Handle links to other sites
                    formattedMessage = formattedMessage.Replace(attachment.Url, $"[Link]({attachment.Url})");
                }
            }

            return formattedMessage;
        }
    }


    [SuppressUnmanagedCodeSecurity]
    public static class ConsoleManager
    {
        private const string Kernel32_DllName = "kernel32.dll";

        [DllImport(Kernel32_DllName)]
        private static extern bool AllocConsole();

        [DllImport(Kernel32_DllName)]
        private static extern bool FreeConsole();

        [DllImport(Kernel32_DllName)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport(Kernel32_DllName)]
        private static extern int GetConsoleOutputCP();

        public static bool HasConsole
        {
            get { return GetConsoleWindow() != IntPtr.Zero; }
        }

        /// <summary>
        /// Creates a new console instance if the process is not attached to a console already.
        /// </summary>
        public static void Show()
        {
            //#if DEBUG
            if (!HasConsole)
            {
                AllocConsole();
            }
            //#endif
        }

        /// <summary>
        /// If the process has a console attached to it, it will be detached and no longer visible. Writing to the System.Console is still possible, but no output will be shown.
        /// </summary>
        public static void Hide()
        {
            //#if DEBUG
            if (HasConsole)
            {
                SetOutAndErrorNull();
                FreeConsole();
            }
            //#endif
        }

        public static void Toggle()
        {
            if (HasConsole)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }
        static void SetOutAndErrorNull()
        {
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);
        }
    }
}