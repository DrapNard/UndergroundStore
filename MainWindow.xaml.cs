using Octokit;
using System;
using System.Windows;
using System.Windows.Interop;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Application = System.Windows.Application;
using Hardcodet.Wpf.TaskbarNotification;
using System.Media;
using DiscordRPC;
using Discord.WebSocket;
using Discord;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Threading.Channels;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using static System.Collections.Specialized.BitVector32;

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
        private bool SysTray;
        private TOKEN TOKEN = new TOKEN();

        System.Windows.Controls.ProgressBar progressBarInstance = new System.Windows.Controls.ProgressBar();
        System.Windows.Controls.Button Install_Update_PlayInstance = new System.Windows.Controls.Button();

        public MainWindow()
        {
            InitializeComponent();
            Updater();
            string GamePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GamePath.txt");
            if (File.Exists(GamePath))
            {
                Install_Update_Play.Content = "Play";
                install = false;
                News.Width = 386;
                InstSpritePack.IsEnabled = false;
            }
            else
            {
                FirstInitialisation();
            }

            string CloseModeTxt = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "/CloseMode.txt");
            if (File.Exists(CloseModeTxt))
            {
                string CloseMode = File.ReadAllText(CloseModeTxt);
                if (CloseMode == "SysTray")
                {
                    SysTray = true;
                    MinimizeSysTray.IsChecked = true;
                    CloseApp.IsChecked = false;
                }
                else if(CloseMode == "Close") 
                {
                    SysTray = false;
                    MinimizeSysTray.IsChecked = false;
                    CloseApp.IsChecked = true;
                }
            }

            client = new DiscordRpcClient("1136234506857226410");
            client.Initialize();

            TaskBarInitialize();
            DiscordRpc("On the Launcher of Infinite Fusion", "Idle");
            discordClientInitialize();
        }
        
        private void FirstInitialisation()
        {
            Install_Update_Play.Content = "Install";
            InstSpritePack.IsEnabled = true;
            install = true;
            News.Width = 730;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (SysTray)
            {
                string CloseTxt = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CloseMode.txt");
                File.WriteAllText(CloseTxt, "SysTray");
            }
            else
            {
                string CloseTxt = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CloseMode.txt");
                File.WriteAllText(CloseTxt, "Close");
            }

            // Déconnectez-vous de Discord lorsque l'application est fermée
            discordClient.LogoutAsync();
            discordClient.StopAsync();
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
        private void discordClientInitialize()
        {
            discordClient = new DiscordSocketClient();
            discordClient.Log += LogMessage;
            discordClient.MessageReceived += DiscordClient_MessageReceived;

            // Connectez-vous à Discord en utilisant un token de bot valide
            discordClient = new DiscordSocketClient();
            discordClient.LoginAsync(TokenType.Bot, TOKEN.token);
            discordClient.StartAsync();

            // Attendez que le client Discord soit prêt avant de récupérer les messages passés
            discordClient.Ready += DiscordClient_Ready;
        }

        private async Task DiscordClient_Ready()
        {
            // Le client Discord est prêt, vous pouvez maintenant écouter les messages
            await FetchAndDisplayPastMessages();
        }
        private async Task FetchAndDisplayPastMessages()
        {
            try
            {
                // Récupérez le canal textuel spécifié par son ID
                var channel = discordClient.GetChannel(channelId) as IMessageChannel;

                // Récupérez les messages passés dans le canal (max. 100 messages)
                var messages = await channel.GetMessagesAsync(100).FlattenAsync();

                // Affichez les messages passés dans le contrôle "News"
                Dispatcher.Invoke(() =>
                {
                    foreach (var message in messages)
                    {
                        News.Content += $"{message.Content}{Environment.NewLine}";
                    }
                });
            }
            catch (Exception ex)
            {
                // Affichez une erreur en cas d'échec de récupération des messages passés
                Dispatcher.Invoke(() =>
                {
                    Console.WriteLine( "Erreur lors de la récupération des messages passés : " + ex.Message);
                });
            }
        }

        private Task DiscordClient_MessageReceived(SocketMessage message)
        {
            // Vérifiez si le message provient du canal souhaité
            if (message.Channel.Id == channelId)
            {
                // Affichez le nouveau message dans le contrôle "News"
                Dispatcher.Invoke(() =>
                {
                    News.Content += $"[{message.Author.Username}] {message.Content}{Environment.NewLine}";
                });
            }
            return Task.CompletedTask;
        }

        private Task LogMessage(LogMessage arg)
        {
            // Log des messages du client Discord dans la console
            Console.WriteLine(arg);
            return Task.CompletedTask;
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
            if (SysTray)
            {
                Hide();
            }
            else
            {
                string CloseTxt = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CloseMode.txt");
                File.WriteAllText(CloseTxt, "Close");
                Application.Current.Shutdown();
            }
        }

        private void MinimazeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (OptionGrid.Visibility == Visibility.Visible)
            {
                OptionGrid.Visibility = Visibility.Collapsed;
                // Cacher le menu d'options et appliquer l'animation de flou
                DoubleAnimation blurAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.3));
                BlurEffect.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);

                DoubleAnimation slideAnimation = new DoubleAnimation(0, OptionGrid.ActualHeight, TimeSpan.FromSeconds(0.2));
                slideAnimation.Completed += (s, args) => OptionGrid.Visibility = Visibility.Collapsed;
                OptionGrid.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
            }
            else
            {
                OptionGrid.Visibility = Visibility.Visible;
                // Afficher le menu d'options et appliquer l'animation de flou
                DoubleAnimation blurAnimation = new DoubleAnimation(10, TimeSpan.FromSeconds(0.3));
                BlurEffect.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);

                DoubleAnimation slideAnimation = new DoubleAnimation(OptionGrid.ActualHeight, 0, TimeSpan.FromSeconds(0.2));
                OptionGrid.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
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
            Process.Start(new ProcessStartInfo("https://aegide.github.io/")
            {
                UseShellExecute = true
            });
        }

        private void MenuItemQuit_Click(object sender, RoutedEventArgs e)
        {
            string CloseTxt = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CloseMode.txt");
            File.WriteAllText(CloseTxt, "SysTray");
            Application.Current.Shutdown();
        }

        private void TaskbarIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {

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
            Process.Start(new ProcessStartInfo("https://aegide.github.io/")
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

        public void PlayNotificationSound()
        {
            try
            {
                using (SoundPlayer player = new SoundPlayer("WindowsBackground.wav"))
                {
                    player.Play();
                }
            }
            catch (Exception ex)
            {
                // Gérez toute exception qui pourrait survenir lors de la lecture du son.
                Console.WriteLine($"Erreur lors de la lecture du son : {ex.Message}");
            }
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
                        PlayNotificationSound();
                        await InstallButon();
                    }
                    else
                    {
                        PlayNotificationSound();
                        await InstallerInstance.Install(this);
                    }
                }

                PlayNotificationSound();
                return;
            }
            if (!install)
            {
                string InstallFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GamePath.txt");
               
                InstPath.Content = InstallFolder;
                string GameFolder  = File.ReadAllText(InstallFolder);
                string GameExecution = System.IO.Path.Combine(GameFolder, "InfiniteFusion/Game.exe");
                Hide();

                Process process = new Process();
                process.StartInfo.FileName = GameExecution;

                DiscordRpc("Playing Pokémon Infinite Fusion", "In Game");

                try
                {
                    // Trouver la fenêtre principale de l'application actuelle

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

        private async Task InstallButon()
        {
            await InstallerInstance.Install(this);
            Console.WriteLine($"Le jeu est installé, le téléchargement et l'installation des packs de sprites vont commencer");
            await InstallerInstance.GraphicPackInstall(this);
        }

        private async void Updater()
        {
            await InstallerInstance.UpdateChecker(this,"infinitefusion","infinitefusion-e18");
            await InstallerInstance.LauncherUpdateChecker(this, "DrapNard", "InfiniteFusion-Launcher");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string baseDirectory = System.IO.Path.Combine(exeDirectory, "saved.bat");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c " + baseDirectory,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            // Exécuter le processus en arrière-plan
            Process process = new Process
            {
                StartInfo = startInfo
            };
            process.Start();
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
            SysTray = true;
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
            SysTray = false;
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
    }
}
