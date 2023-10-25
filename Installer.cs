using Octokit;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "GithubReleaseDownloader");
            config = Configuration.Load(configFilePath);
        }

        // Method to get the file size asynchronously
        public async Task<long> GetFileSizeAsync(string url)
        {
            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return response.Content.Headers.ContentLength ?? 0;
        }

        // Method to download the latest release asynchronously
        public async Task ReleaseDownloaderAsync(IProgress<int> progress, string owner, string repo, string tree)
        {
            string archiveFormat = "zip";

            string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                string releaseInfo = await response.Content.ReadAsStringAsync();
                using (JsonDocument document = JsonDocument.Parse(releaseInfo))
                {
                    JsonElement root = document.RootElement;
                    string releaseName = root.GetProperty("name").GetString();
                    Console.WriteLine($"Latest release name: {releaseName}");

                    Stream archiveStream = await httpClient.GetStreamAsync($"https://github.com/{owner}/{repo}/archive/refs/heads/{tree}.{archiveFormat}");

                    // Create the full path of the text file in the executable directory
                    if (owner == "infinitefusion")
                    {
                        config.GameVersion = releaseName;
                        config.Save(configFilePath);
                    }

                    // Download the source code of the latest release with progress handling
                    string archiveFilePath = Path.Combine(exeDirectory, $"{repo}-latest.{archiveFormat}");
                    ZipInstaller = archiveFilePath;
                    Console.WriteLine($"Downloading the latest release source code of {owner} has begun");
                    using (FileStream fileStream = new FileStream(archiveFilePath, System.IO.FileMode.Create))
                    {
                        const int bufferSize = 8192;
                        var buffer = new byte[bufferSize];
                        int bytesRead;
                        long totalBytesRead = 0;

                        while ((bytesRead = await archiveStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);

                            totalBytesRead += bytesRead;

                            // Update the progress via the IProgress<int> object
                            int progressPercentage = (int)((double)totalBytesRead / totalFileSize * 100);
                            progress.Report(progressPercentage);
                        }
                    }

                    Console.WriteLine($"The latest release source code has been downloaded: {archiveFilePath}");
                    Console.WriteLine($"The name of the latest release has been saved in: {releaseName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading the source code: {ex.Message}");
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
                Console.WriteLine($"Error decompressing the .zip file: {ex.Message}");
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

        public async Task Install(MainWindow mainScript, string _path)
        {
            exeDirectory = _path;
            // Disable the Install/Update/Play button, show progress bar, and status message
            mainScript.Install_Update_Play.IsEnabled = false;
            mainScript.progressBar.Visibility = Visibility.Visible;
            mainScript.Statue.Content = "Preparation ...";
            mainScript.Statue.Visibility = Visibility.Visible;

            // Use IProgress<int> to report progress during the download
            IProgress<int> progress = new Progress<int>(value => mainScript.progressBar.Value = value);

            // Get the total file size of the latest release for progress tracking
            totalFileSize = await GetFileSizeAsync($"https://github.com/infinitefusion/infinitefusion-e18/archive/refs/heads/releases.zip");

            mainScript.Statue.Content = "Downloading Game Archive ...";
            // Download the latest release archive
            await ReleaseDownloaderAsync(progress, "infinitefusion", "infinitefusion-e18", "releases");

            mainScript.Statue.Content = "Extracting Game File ...";
            mainScript.progressBar.IsIndeterminate = true;

            // Extract the downloaded archive to the executable directory
            await DecompressZip(ZipInstaller, exeDirectory);

            mainScript.Statue.Content = "Cleaning ...";

            string parentDirectory = exeDirectory;
            string partialName = "infinitefusion-e18";
            string[] matchingFolders = FindFoldersByPartialName(parentDirectory, partialName);

            if (matchingFolders.Length > 0)
            {
                InstallfolderPath = matchingFolders[0];
            }

            // Rename the folder after decompression
            string folderInstallRename = Path.Combine(exeDirectory, InstallfolderPath);
            RenameFolder(folderInstallRename, "InfiniteFusion");

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
            config.GameSpritePack = "None";

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
            System.Windows.Application.Current.Shutdown();
        }

        public async Task GraphicPackInstall(MainWindow mainScript)
        {
            // Install the graphic pack
            mainScript.Install_Update_Play.IsEnabled = false;
            mainScript.progressBar.Visibility = Visibility.Visible;
            mainScript.Statue.Content = "Preparation ...";
            mainScript.Statue.Visibility = Visibility.Visible;

            IProgress<int> progress = new Progress<int>(value => mainScript.progressBar.Value = value);
            string SpritePack = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SpritePack.txt");
            File.WriteAllText(SpritePack, "yes");
            string txtFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GamePath.txt");
            string appDirectory = File.ReadAllText(txtFolder);

            string GameDirectory = Path.Combine(appDirectory, "InfiniteFusion");
            string tempDirectory = Path.Combine(appDirectory, "GraphickPackTemp"); // Temporary folder for downloading

            try
            {
                // Create the temporary folder if it doesn't exist
                if (!Directory.Exists(tempDirectory))
                    Directory.CreateDirectory(tempDirectory);

                mainScript.Statue.Content = "Downloading Sprite Archive ...";
                // Download the sprite pack file
                await ReleaseDownloaderAsync(progress, "DrapNard", "InfiniteFusion-Launcher", "SpritePack");

                mainScript.Statue.Content = "Extracting Sprite File ...";

                // Extract the sprite pack files
                await DecompressZip(ZipInstaller, tempDirectory);

                mainScript.Statue.Content = "Finishing ...";

                // Replace the application files with the new sprite pack files
                string parentDirectory = tempDirectory;
                string partialName = "InfiniteFusion-Launcher-";
                string[] matchingFolders = FindFoldersByPartialName(parentDirectory, partialName);

                if (matchingFolders.Length > 0)
                {
                    InstallfolderPath = matchingFolders[0];
                }

                // Copy the sprite pack files into the installation directory
                CopyFilesRecursively(new DirectoryInfo(InstallfolderPath), new DirectoryInfo(GameDirectory));

                mainScript.Statue.Content = "Graphic pack installed successfully!";
            }
            catch (Exception ex)
            {
                mainScript.Statue.Content = "Error during graphic pack installation: " + ex.Message;
            }
            finally
            {
                mainScript.Statue.Content = "Cleaning ...";
                // Delete the temporary folder
                Directory.Delete(tempDirectory, true);
                await DeleteZipFile(ZipInstaller);

                mainScript.progressBar.IsIndeterminate = false;
                mainScript.Install_Update_Play.IsEnabled = true; // Re-enable the button after download
                mainScript.progressBar.Visibility = Visibility.Collapsed;
                mainScript.Statue.Visibility = Visibility.Collapsed;

                mainScript.progressBar.Value = 0;
                mainScript.Install_Update_Play.Content = "Play";
            }
        }

        public async Task UpdateChecker(MainWindow mainScript, string owner, string repo)
        {
            // Check for updates and prompt the user to install them if available
            string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            try
            {
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

                if (config.GameVersion != null)
                {
                    if (config.GameVersion != releaseName)
                    {
                        MessageBoxResult result = MessageBox.Show("A new update is available", "Do you want to install it?", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        // Check the user's response
                        if (result == MessageBoxResult.Yes)
                        {
                            string gamePath = Path.Combine(config.GamePath, "InfiniteFusion");
                            Directory.Delete(gamePath, true);
                            config.GameVersion = null;
                            config.Save(configFilePath);


                            mainScript.UninstallBtn.IsEnabled = false;
                            mainScript.RepairBtn.IsEnabled = false;
                            mainScript.ExitBlock.Visibility = Visibility.Visible;

                            Install(mainScript, config.GamePath);
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

        private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo destination)
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
                CopyFilesRecursively(subDirectory, new DirectoryInfo(destinationSubDirectoryPath));
            }
        }
    }
}
