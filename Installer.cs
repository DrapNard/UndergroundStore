using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Pokémon_Infinite_Fusion_Launcher
{
    internal class Installer
    {
        private HttpClient httpClient;
        public string exeDirectory;
        private string ZipInstaller;
        private string InstallfolderPath;
        private long totalFileSize;

        string configFilePath = "config.json";
        Configuration config;

        public Installer()
        {
            config = Configuration.Load(configFilePath);
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", config.UID.ToString());
            config = Configuration.Load(configFilePath);
        }

        // Method to get the file size asynchronously
        public async Task<long> GetFileSizeAsync(string url)
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return response.Content.Headers.ContentLength ?? 0;
            }
            catch
            {
                return totalFileSize = 563200000;
            }
        }

        // Method to download the latest release asynchronously
        public async Task ReleaseDownloaderAsync(IProgress<int> progress, string owner, string repo, string tree, string Service)
        {
            string archiveFormat = "zip";

            try
            {
                string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

                try
                {
                    string releaseName = null;

                    if (Service == "GitHub")
                    {
                        HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                        response.EnsureSuccessStatusCode();

                        string releaseInfo = await response.Content.ReadAsStringAsync();
                        using (JsonDocument document = JsonDocument.Parse(releaseInfo))
                        {
                            JsonElement root = document.RootElement;
                            releaseName = root.GetProperty("name").GetString();
                            Console.WriteLine($"Latest release name: {releaseName}");
                        }
                    }

                    // Choose the appropriate URL based on the service
                    string archiveUrl = (Service == "GitHub")
                        ? $"https://github.com/{owner}/{repo}/archive/refs/heads/{tree}.{archiveFormat}"
                        : $"https://gitlab.com/{owner}/{repo}/-/archive/{tree}/{repo}-{tree}.{archiveFormat}";

                    // Create the full path of the archive file in the executable directory
                    string archiveFilePath = Path.Combine(exeDirectory, $"{repo}-latest.{archiveFormat}");
                    ZipInstaller = archiveFilePath;

                    Console.WriteLine($"Downloading the latest release source code of {owner} has begun");

                    // Use WebClient for downloading with progress handling
                    using (var client = new WebClient())
                    {
                        // Report progress from WebClient
                        client.DownloadProgressChanged += (s, e) => progress.Report(e.ProgressPercentage);

                        // Download the file asynchronously
                        await client.DownloadFileTaskAsync(archiveUrl, archiveFilePath);
                    }

                    config.GameVersion = releaseName;

                    Console.WriteLine($"The latest release source code has been downloaded: {archiveFilePath}");
                    Console.WriteLine($"The name of the latest release has been saved in: {releaseName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error downloading the Game: {ex.Message}", "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The Server returned an error: {ex.Message}", "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        // Method to decompress a zip file asynchronously
        public async Task DecompressZip(string zipFilePath, string extractPath)
        {
            // Ensure the .zip file exists
            if (!File.Exists(zipFilePath))
            {
                Console.WriteLine("The .zip file does not exist.");
                return;
            }

            // Ensure the destination folder exists
            if (!Directory.Exists(extractPath))
            {
                Console.WriteLine("The destination folder does not exist.");
                return;
            }

            // Decompress the .zip file
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        // Create the full output path for each entry in the .zip
                        string entryOutputPath = Path.Combine(extractPath, entry.FullName);

                        // If the entry is a directory, ensure the directory exists
                        if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                        {
                            Directory.CreateDirectory(entryOutputPath);
                        }
                        else // Otherwise, it's a file, extract it
                        {
                            entry.ExtractToFile(entryOutputPath, true);
                        }
                    }
                }
                string extractedDirectoryName = Path.GetFileName(extractPath);
                Console.WriteLine("Decompression completed.");

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting the Game: {ex.Message}", "Extraction Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Method to rename a folder
        public void RenameFolder(string currentFolderPath, string newFolderName)
        {
            // Ensure the current folder exists
            if (!Directory.Exists(currentFolderPath))
            {
                Console.WriteLine("The current folder does not exist.");
                return;
            }

            // Ensure the new folder name is not empty or null
            if (string.IsNullOrWhiteSpace(newFolderName))
            {
                Console.WriteLine("The new folder name is not valid.");
                return;
            }

            // Get the full path of the new folder by combining the parent directory with the new name
            string newFolderPath = Path.Combine(Path.GetDirectoryName(currentFolderPath), newFolderName);

            // Check if a folder with the new name already exists
            if (Directory.Exists(newFolderPath))
            {
                Console.WriteLine("A folder with the new name already exists.");
                return;
            }

            // Rename the folder
            try
            {
                Directory.Move(currentFolderPath, newFolderPath);
                Console.WriteLine("Folder renamed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error renaming the folder: {ex.Message}");
            }
        }

        public string[] FindFoldersByPartialName(string parentDirectory, string partialName)
        {
            // Assurez-vous que le répertoire parent existe
            if (!Directory.Exists(parentDirectory))
            {
                Console.WriteLine("Le répertoire parent n'existe pas.");
                return new string[0];
            }

            // Recherchez les dossiers correspondant à la partie du nom spécifiée
            try
            {
                string[] matchingFolders = Directory.GetDirectories(parentDirectory, "*" + partialName + "*");
                return matchingFolders;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la recherche des dossiers : {ex.Message}");
                return new string[0];
            }
        }

        public async Task DeleteZipFile(string zipFilePath)
        {
            // Assurez-vous que le fichier .zip existe avant de le supprimer
            if (File.Exists(zipFilePath))
            {
                try
                {
                    // Supprimez le fichier .zip
                    File.Delete(zipFilePath);
                    Console.WriteLine("Le fichier .zip a été supprimé avec succès.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la suppression du fichier .zip : {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Le fichier .zip n'existe pas.");
            }
        }

        public async Task Install(MainWindow mainScript, string _path, bool Update)
        {
            exeDirectory = _path;
            // Disable the Install/Update/Play button, show progress bar, and status message
            mainScript.Install_Update_Play.IsEnabled = false;
            mainScript.progressBar.Visibility = Visibility.Visible;

            // Download the latest release archive
            try
            {
                mainScript.progressBar.IsIndeterminate = true;
                mainScript.Statue.Content = "Preparation ...";
                mainScript.Statue.Visibility = Visibility.Visible;

                // Use IProgress<int> to report progress during the download
                IProgress<int> progress = new Progress<int>(value => mainScript.progressBar.Value = value);

                // Get the total file size of the latest release for progress tracking
                totalFileSize = await GetFileSizeAsync($"https://github.com/infinitefusion/infinitefusion-e18/archive/refs/heads/releases.zip");

                mainScript.Statue.Content = "Downloading Game Archive ...";
                mainScript.progressBar.IsIndeterminate = false;

                await ReleaseDownloaderAsync(progress, "infinitefusion", "infinitefusion-e18", "releases", "GitHub");
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Error downloading the Game: {ex.Message}", "Donwload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            mainScript.Statue.Content = "Extracting Game File ...";
            mainScript.progressBar.IsIndeterminate = true;

            // Extract the downloaded archive to the executable directory
            try
            {
                await Task.Run(() => DecompressZip(ZipInstaller, exeDirectory));
            }
            catch
            {
                return;
            }

            mainScript.Statue.Content = "Cleaning ...";

            string parentDirectory = exeDirectory;
            string partialName = "infinitefusion-e18";
            string[] matchingFolders = FindFoldersByPartialName(parentDirectory, partialName);

            if (matchingFolders.Length > 0)
            {
                InstallfolderPath = matchingFolders[0];
            }

            if (!Update)
            {
                // Rename the folder after decompression
                string folderInstallRename = Path.Combine(exeDirectory, InstallfolderPath);
                RenameFolder(folderInstallRename, "InfiniteFusion");
            }
            else if (Update) 
            {
                await Task.Run(() => CopyFilesRecursively(new DirectoryInfo(Path.Combine(exeDirectory, InstallfolderPath)), new DirectoryInfo(Path.Combine(config.GamePath, "InfiniteFusion"))));
                Directory.Delete(Path.Combine(exeDirectory, InstallfolderPath), true);
            }

            // Clean up by deleting the downloaded archive
            await DeleteZipFile(ZipInstaller);

            mainScript.Statue.Content = "Finishing ...";

            // Decompress the species.zip data file
            string decompressData = Path.Combine(exeDirectory, "InfiniteFusion/Data/species.zip");
            string decompressDataPath = Path.Combine(exeDirectory, "InfiniteFusion/Data");
            await DecompressZip(decompressData, decompressDataPath);

            mainScript.progressBar.IsIndeterminate = false;
            mainScript.Install_Update_Play.IsEnabled = true;
            mainScript.progressBar.Visibility = Visibility.Collapsed;
            mainScript.Statue.Visibility = Visibility.Collapsed;

            // Update the Config.ini with the game installation path and graphic pack status
            config.GamePath = exeDirectory;

            config.Save(configFilePath);

            // Update the main window's status
            mainScript.install = false;
            mainScript.progressBar.Value = 0;
            mainScript.Install_Update_Play.Content = "Play";
            mainScript.InstPlayButtonStyle(true);
            mainScript.InstSpritePack.IsEnabled = false;

            SystemSounds.Beep.Play();

            string applicationPath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(applicationPath);
            Application.Current.Shutdown();
        }

        public async Task GraphicPackInstall(MainWindow mainScript)
        {
            mainScript.Install_Update_Play.Style = (Style)mainScript.FindResource("UpdateImageButtonStyle");
            exeDirectory = Path.Combine(config.GamePath, "InfiniteFusion/Graphics");
            mainScript.UninstallBtn.IsEnabled = false;
            mainScript.RepairBtn.IsEnabled = false;
            mainScript.ExitBlock.Visibility = Visibility.Visible;

            // Disable the Install/Update/Play button, show progress bar, and status message
            mainScript.Install_Update_Play.IsEnabled = false;
            mainScript.progressBar.Visibility = Visibility.Visible;

            // Download the latest release archive
            try
            {
                mainScript.progressBar.IsIndeterminate = true;
                mainScript.Statue.Content = "Preparation ...";
                mainScript.Statue.Visibility = Visibility.Visible;

                // Use IProgress<int> to report progress during the download
                IProgress<int> progress = new Progress<int>(value => mainScript.progressBar.Value = value);

                // Get the total file size of the latest release for progress tracking
                totalFileSize = await GetFileSizeAsync($"https://gitlab.com/pokemoninfinitefusion/customsprites/-/archive/master/customsprites-master.zip");

                mainScript.Statue.Content = "Downloading Sprite Pack Archive ...";
                mainScript.progressBar.IsIndeterminate = false;

                await ReleaseDownloaderAsync(progress, "pokemoninfinitefusion", "customsprites", "master", "GitLab");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading the Sprite Pack: {ex.Message}", "Donwload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            mainScript.Statue.Content = "Extracting Sprite File ...";
            mainScript.progressBar.IsIndeterminate = true;

            // Extract the downloaded archive to the executable directory
            try
            {
                await Task.Run(() => DecompressZip(ZipInstaller, exeDirectory));
            }
            catch
            {
                return;
            }

            mainScript.Statue.Content = "Cleaning ...";

            string parentDirectory = exeDirectory;
            string partialName = "customsprites-master";
            string[] matchingFolders = FindFoldersByPartialName(parentDirectory, partialName);

            if (matchingFolders.Length > 0)
            {
                InstallfolderPath = matchingFolders[0];
            }

            // Clean up by deleting the downloaded archive
            await DeleteZipFile(ZipInstaller);

            mainScript.Statue.Content = "Finishing ...";

            await Task.Run(() => CopyFilesRecursively(new DirectoryInfo(Path.Combine(exeDirectory, InstallfolderPath)), new DirectoryInfo(Path.Combine(config.GamePath, "InfiniteFusion/Graphics"))));
            await Task.Run(() => CopyFilesRecursively(new DirectoryInfo(Path.Combine(exeDirectory, InstallfolderPath, "Other")), new DirectoryInfo(Path.Combine(config.GamePath, "InfiniteFusion/Graphics"))));
            Directory.Delete(Path.Combine(exeDirectory, InstallfolderPath), true);

            mainScript.progressBar.IsIndeterminate = false;
            mainScript.Install_Update_Play.IsEnabled = true;
            mainScript.progressBar.Visibility = Visibility.Collapsed;
            mainScript.Statue.Visibility = Visibility.Collapsed;


            config.Save(configFilePath);

            // Update the main window's status
            mainScript.install = false;
            mainScript.progressBar.Value = 0;
            mainScript.Install_Update_Play.Content = "Play";
            mainScript.InstPlayButtonStyle(true);
            mainScript.InstSpritePack.IsEnabled = false;

            SystemSounds.Beep.Play();

            string applicationPath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(applicationPath);
            Application.Current.Shutdown();
        }

        public async Task UpdateChecker(MainWindow mainScript, string owner, string repo, bool ignoredUpdateCheck)
        {
            try
            {
                string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
                string releaseName;
                string releaseInfo = await response.Content.ReadAsStringAsync();
                using (JsonDocument document = JsonDocument.Parse(releaseInfo))
                {
                    JsonElement root = document.RootElement;
                    releaseName = root.GetProperty("name").GetString();
                    Console.WriteLine($"Latest release name: {releaseName}");
                }
                if (ignoredUpdateCheck)
                {
                    mainScript.Install_Update_Play.Style = (Style)mainScript.FindResource("UpdateImageButtonStyle");
                    string gamePath = Path.Combine(config.GamePath, "InfiniteFusion");
                    foreach (string SubFolder in Directory.GetDirectories(gamePath))
                    {
                        if (Path.GetFileName(SubFolder) != "Graphics")
                        {
                            Directory.Delete(SubFolder, true); // true indique de supprimer récursivement

                            foreach (string fichier in Directory.GetFiles(gamePath))
                            {
                                File.Delete(fichier);
                            }
                        }
                    }
                    config.GameVersion = null;
                    config.Save(configFilePath);


                    mainScript.UninstallBtn.IsEnabled = false;
                    mainScript.RepairBtn.IsEnabled = false;
                    mainScript.ExitBlock.Visibility = Visibility.Visible;

                    Install(mainScript, config.GamePath, true);
                    return;
                }
                if (config.GameVersion != null)
                {
                    if (config.GameVersion != releaseName)
                    {
                        MessageBoxResult result = MessageBox.Show("A new update is available", "Do you want to install it?", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        // Check the user's response
                        if (result == MessageBoxResult.Yes)
                        {
                            mainScript.Install_Update_Play.Style = (Style)mainScript.FindResource("UpdateImageButtonStyle");
                            string gamePath = Path.Combine(config.GamePath, "InfiniteFusion");
                            foreach (string SubFolder in Directory.GetDirectories(gamePath))
                            {
                                if (Path.GetFileName(SubFolder) != "Graphics")
                                {
                                    Directory.Delete(SubFolder, true); // true indique de supprimer récursivement

                                    foreach (string fichier in Directory.GetFiles(gamePath))
                                    {
                                        File.Delete(fichier);
                                    }
                                }
                            }
                            config.GameVersion = null;
                            config.Save(configFilePath);


                            mainScript.UninstallBtn.IsEnabled = false;
                            mainScript.RepairBtn.IsEnabled = false;
                            mainScript.ExitBlock.Visibility = Visibility.Visible;

                            Install(mainScript, config.GamePath, true);
                        }
                        else if (result == MessageBoxResult.No)
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for updates: {ex.Message}");
            }
        }

        public async Task UpdaterUpdater(MainWindow mainScript)
        {
            try
            {
                string LauncherDir = AppDomain.CurrentDomain.BaseDirectory;

                exeDirectory = LauncherDir;

                mainScript.Install_Update_Play.Style = (Style)mainScript.FindResource("UpdateImageButtonStyle");
                    
                    
                mainScript.UninstallBtn.IsEnabled = false;
                mainScript.RepairBtn.IsEnabled = false;
                mainScript.ExitBlock.Visibility = Visibility.Visible;

                await DelLauncherFile();

                // Disable the Install/Update/Play button, show progress bar, and status message
                mainScript.Install_Update_Play.IsEnabled = false;
                mainScript.progressBar.Visibility = Visibility.Visible;

                mainScript.progressBar.IsIndeterminate = true;
                mainScript.Statue.Content = "Preparation ...";
                mainScript.Statue.Visibility = Visibility.Visible;

                // Download the latest release archive
                try
                { 
                    // Use IProgress<int> to report progress during the download
                    IProgress<int> progress = new Progress<int>(value => mainScript.progressBar.Value = value);

                    // Get the total file size of the latest release for progress tracking
                    totalFileSize = await GetFileSizeAsync($"https://github.com/DrapNard/InfiniteFusion-Launcher/archive/refs/heads/Updater(Binairies).zip");

                    mainScript.Statue.Content = "Downloading Launcher Archive ...";
                    mainScript.progressBar.IsIndeterminate = false;

                    await ReleaseDownloaderAsync(progress, "DrapNard", "InfiniteFusion-Launcher", "Updater(Binairies)", "GitHub");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error downloading the Updater: {ex.Message}", "Donwload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                mainScript.Statue.Content = "Extracting Launcher File ...";
                mainScript.progressBar.IsIndeterminate = true;

                // Extract the downloaded archive to the executable directory
                try
                {
                    await Task.Run(() => DecompressZip(ZipInstaller, exeDirectory));
                }
                catch
                {
                    return;
                }

                mainScript.Statue.Content = "Cleaning ...";

                string parentDirectory = LauncherDir;
                string partialName = "InfiniteFusion-Launcher-Updater-Binairies-";
                string[] matchingFolders = FindFoldersByPartialName(parentDirectory, partialName);

                if (matchingFolders.Length > 0)
                {
                    InstallfolderPath = matchingFolders[0];
                }

                await Task.Run(() => CopyFilesRecursively(new DirectoryInfo(Path.Combine(exeDirectory, InstallfolderPath)), new DirectoryInfo(LauncherDir)));
                Directory.Delete(Path.Combine(exeDirectory, InstallfolderPath), true);
                

                // Clean up by deleting the downloaded archive
                await DeleteZipFile(ZipInstaller);

                mainScript.Statue.Content = "Finishing ...";

                mainScript.progressBar.IsIndeterminate = false;
                mainScript.Install_Update_Play.IsEnabled = true;
                mainScript.progressBar.Visibility = Visibility.Collapsed;
                mainScript.Statue.Visibility = Visibility.Collapsed;

                // Update the main window's status
                mainScript.install = false;
                mainScript.progressBar.Value = 0;
                mainScript.Install_Update_Play.Content = "Play";
                mainScript.InstPlayButtonStyle(true);
                mainScript.InstSpritePack.IsEnabled = false;

                SystemSounds.Beep.Play();

                string applicationPath = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(applicationPath);
                Application.Current.Shutdown();
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for updates: {ex.Message}");
            }
        }

        private async Task DelLauncherFile()
        {
            string LauncherDir = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                string[] files = Directory.GetFiles(LauncherDir);

                string[] UpdaterFiles = { "Updater.exe", "Updater.dll", "Updater.deps.json", "Updater.runtimeconfig.json" };

                foreach (string file in files)
                {
                    // Vérifiez si le nom du fichier est dans la liste des fichiers à supprimer
                    if (Array.IndexOf(UpdaterFiles, Path.GetFileName(file)) != -1)
                    {
                        // Supprimez le fichier
                        File.Delete(file);
                        Console.WriteLine($"Le fichier {file} a été supprimé.");
                    }
                }
            }
            catch (Exception ex )
            {
                MessageBox.Show($"Error Updating the launcher: {ex.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private static async Task CopyFilesRecursively(DirectoryInfo source, DirectoryInfo destination)
        {
            if (!destination.Exists)
            {
                destination.Create();
            }

            // Copier les fichiers
            foreach (FileInfo file in source.GetFiles())
            {
                string destinationFilePath = Path.Combine(destination.FullName, file.Name);
                file.CopyTo(destinationFilePath, true);
            }

            // Copier les sous-dossiers
            foreach (DirectoryInfo subDirectory in source.GetDirectories())
            {
                string destinationSubDirectoryPath = Path.Combine(destination.FullName, subDirectory.Name);
                await Task.Run(() => CopyFilesRecursively(subDirectory, new DirectoryInfo(destinationSubDirectoryPath)));
            }
        }
    }
}
