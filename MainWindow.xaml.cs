using System;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using Hardcodet.Wpf.TaskbarNotification;
using System.Media;
using DiscordRPC;
using Discord.WebSocket;
using Discord;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Security;
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
        InstallationOptionsDialog InstOptDiag = new InstallationOptionsDialog();
        public bool install;
        private string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        Installer InstallerInstance = new Installer();
        private TaskbarIcon taskbarIcon;
        private DiscordRpcClient client;
        private string InstallFolder;
        private DiscordSocketClient discordClient;
        private ulong channelId = 302158319886532618;
        private TOKEN TOKEN = new TOKEN();
        System.Windows.Controls.ProgressBar progressBarInstance = new System.Windows.Controls.ProgressBar();
        System.Windows.Controls.Button Install_Update_PlayInstance = new System.Windows.Controls.Button();

        string configFilePath = "config.json";
        Configuration config;

        public MainWindow()
        {
            InitializeComponent();
            Initialisation();
            Updater();
        }

        private void FirstInitialisation()
        {
            Install_Update_Play.Style = (Style)FindResource("InstallImageButtonStyle");
            InstSpritePack.IsEnabled = true;
            install = true;
            News.Width = 730;
            GameVer.Content = "Not Installed";
            UninstallBtn.IsEnabled = false;
            RepairBtn.IsEnabled = false;
            GamePathBtn.IsEnabled = false;
            GamePathBtnBlock.Visibility = Visibility.Visible;
            GamePathBtnBlockToolTip.Visibility = Visibility.Visible;
            UnInstallRepairBtnBlock.Visibility = Visibility.Visible;
            UnInstallRepairBtnBlockTooltip.Visibility = Visibility.Visible;
        }

        private void Initialisation()
        {
            config = Configuration.Load(configFilePath);
            LaunchVer.Content = config.Version;
            Main.Visibility = Visibility.Hidden;
            AlternateLauncherCheckBox.IsChecked = config.AlternateLauncherEnable;
            ConsoleCheckBox.IsChecked = config.EnableConsole;
            TabManager(0);

            DiscordBot discordBot = new DiscordBot(channelId, TOKEN.token, this.News);
            discordBot.Initialize();

            if (config.GamePath != null)
            {
                Install_Update_Play.Style = (Style)FindResource("PlayImageButtonStyle");
                Install_Update_Play.Content = "Play";
                GameVer.Content = config.GameVersion;
                install = false;
                News.Width = 640;
                InstSpritePack.IsEnabled = false;
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

            client = new DiscordRpcClient("1136234506857226410");
            client.Initialize();

            TaskBarInitialize();
            DiscordRpc("On the Launcher of Infinite Fusion", "Idle");
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
                UseShellExecute= true
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
            Process.Start(new ProcessStartInfo("https://www.pokecommunity.com/showthread.php?t=347883")
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

        private async void Install_Update_Play_Click(object sender, RoutedEventArgs e)
        {
            
            if (install)
            {
                // Créer une boîte de dialogue personnalisée (peut être un UserControl ou une autre fenêtre WPF)
                InstallationOptionsDialog optionsDialog = new InstallationOptionsDialog();

                // Afficher la boîte de dialogue de manière modale
                bool? result = optionsDialog.ShowDialog();

                // Vérifier si l'utilisateur a cliqué sur le bouton OK
                if (result == true)
                {
                    if (InstSpritePack.IsChecked == true)
                    {
                        string SelectFolder = optionsDialog.selectedFolderPath;
                        SystemSounds.Beep.Play();
                        ExitBlock.Visibility = Visibility.Visible;
                        await InstallButon(SelectFolder);
                    }
                    else
                    {
                        string SelectFolder = optionsDialog.selectedFolderPath;
                        SystemSounds.Beep.Play();
                        ExitBlock.Visibility = Visibility.Visible;
                        await InstallerInstance.Install(this, SelectFolder);
                    }
                }

                SystemSounds.Beep.Play();
                return;
            }
            if (!install)
            {
                string GameExecution;

                if (!config.AlternateLauncherEnable || config.AlternateLauncherEnable == null)
                {
                    GameExecution = Path.Combine(config.GamePath, "InfiniteFusion/Game.exe");
                }
                else
                {
                    GameExecution = Path.Combine(config.GamePath, "InfiniteFusion/Alternate Launcher.exe");
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

                    // Attendre la fin du processus (si nécessaire)
                    process.WaitForExit();
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

        private async Task InstallButon(string _path)
        {
            await InstallerInstance.Install(this, _path);
            await InstallerInstance.GraphicPackInstall(this);
        }

        private async void Updater()
        {
            await InstallerInstance.UpdateChecker(this,"infinitefusion","infinitefusion-e18");            
            StartUpdater();
            //await UD.LauncherUpdateChecker("DrapNard", "InfiniteFusion-Launcher");
        }

        public void StartUpdater()
        {
            Main.Visibility = Visibility.Hidden;

            string baseDirectory = System.IO.Path.Combine(exeDirectory, "Updater.exe");

            // Créez un processus pour exécuter le programme externe
            Process externalProcess = new Process();
            externalProcess.StartInfo.FileName = baseDirectory;

            // Démarrez le processus
            externalProcess.Start();

            // Attendez que le processus externe se termine
            externalProcess.WaitForExit();

            Main.Visibility = Visibility.Visible;
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
            if(CloseApp.IsChecked == true)
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

        private void InstSpritePack_Checked(object sender, RoutedEventArgs e)
        {
            if (InstSpritePack.IsChecked == true)
            {
                InstOptDiag.GraphPackInstall.Visibility = Visibility.Visible;
            }
            else
            {
                InstOptDiag.GraphPackInstall.Visibility = Visibility.Collapsed;
            }
        }

        public void InstPlayButtonStyle(bool Install)
        {
            if (Install)
            {
                Install_Update_Play.Style = (Style)FindResource("PlayImageButtonStyle");
            }else
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
                GeneralTabBtn.Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF3E3E3E"));
                TroubleshootTabBtn.Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF333333"));
            }
            else if (TabIndex == 1)
            {
                TroubleshootTabBtn.IsEnabled = false;
                GeneralTabBtn.IsEnabled = true;
                TroubleshootTabBtn.Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF3E3E3E"));
                GeneralTabBtn.Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF333333"));
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
            if(ConsoleCheckBox.IsChecked == true)
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
            if(AlternateLauncherCheckBox.IsChecked == true)
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
            string gamePath = Path.Combine(config.GamePath, "InfiniteFusion");
            Directory.Delete(gamePath, true);
            config.GameVersion = null;
            config.Save(configFilePath);

            DisiableOtherButton.Visibility = Visibility.Collapsed;
            // Cacher le menu d'options et appliquer l'animation de flou
            DoubleAnimation blurAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.3));
            BlurEffect.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);

            DoubleAnimation fadeOutAnimation = (DoubleAnimation)FindResource("FadeOutAnimation");
            await ApplyAnimationAsync(OptionGrid, fadeOutAnimation, true);

            UninstallBtn.IsEnabled = false;
            RepairBtn.IsEnabled = false;
            ExitBlock.Visibility = Visibility.Visible;
            InstallerInstance.Install(this, config.GamePath);
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

        private void YesUninstallConfirmation_Click(object sender, RoutedEventArgs e)
        {
            string gamePath = Path.Combine(config.GamePath, "InfiniteFusion");
            Directory.Delete(gamePath, true);
            config.GamePath = null;
            config.GameVersion = null;
            config.GameSpritePack = null;
            config.Save(configFilePath);

            MessageBoxResult result = System.Windows.MessageBox.Show("The launcher will restart when this window is closed", "Uninstall Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            string applicationPath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(applicationPath);
            System.Windows.Application.Current.Shutdown();
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
        public string GameSpritePack { get; set; }
        public bool AlternateLauncherEnable { get; set; }
        public bool EnableConsole { get; set; }

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
