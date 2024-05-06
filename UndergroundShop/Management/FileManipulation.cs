using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace UndergroundShop.Management
{
    public class WebFile
    {
        static readonly HttpClient client = new();

        private readonly string url;

        public WebFile(string url)
        {
            this.url = url;
        }

        public async Task Download(string path)
        {
            try
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 6.1; WOW64; rv:40.0) Gecko/20100101 Firefox/40.1");

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                using var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var fileName = GetFileNameFromUrl(url);
                    var filePath = Path.Combine(path, fileName);

                    long totalBytes = response.Content.Headers.ContentLength ?? 0; // Handle missing ContentLength
                    long downloadedBytes = 0;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(filePath, FileMode.CreateNew))
                    {
                        var buffer = new byte[4096]; // Efficient buffer size
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            downloadedBytes += bytesRead;
                            double percentage = (double)downloadedBytes / totalBytes * 100;

                            // Update percentage and counter using preferred logging mechanism (replace with your implementation)
                            UpdateDownloadProgress(percentage, downloadedBytes);

                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                        }
                    }

                    MessageManagement.ConsoleMessage($"Downloaded file to: {path}", 2);
                }
                else
                {
                    MessageManagement.ConsoleMessage($"Failed to download file: {response.StatusCode}", 4);
                }
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage($"Error downloading file: {ex.Message}", 4);
            }
        }

        private void UpdateDownloadProgress(double percentage, long downloadedBytes)
        {
            // Example using Console:
            MessageManagement.ConsoleMessage($"Download progress: {percentage:F2}% ({downloadedBytes} bytes downloaded)", 1);
        }

        private string GetFileNameFromUrl(string url)
        {
            Uri uri = new Uri(url);
            return Path.GetFileName(uri.LocalPath);
        }

        public async Task<long> GetFileLengthAsync()
        {
            try
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 6.1; WOW64; rv:40.0) Gecko/20100101 Firefox/40.1");

                var request = new HttpRequestMessage(HttpMethod.Head, url);

                using var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    long contentLength = (long)response.Content.Headers.ContentLength;
                    MessageManagement.ConsoleMessage($"File Length: {contentLength} bytes", 2);
                    return contentLength;
                }
                else
                {
                    MessageManagement.ConsoleMessage($"Failed to get file length: {response.StatusCode}", 4);
                    return 0;
                }
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage($"Error: {ex.Message}", 4);
                return 0;
            }
        }
    }

    internal class FileManagement
    {
        public static void RenameFolder(string currentFolderPath, string newFolderName)
        {
            // Ensure the current folder exists
            if (!Directory.Exists(currentFolderPath))
            {
                MessageManagement.ConsoleMessage(MessageManagement.DirNotEx, 4);
                return;
            }

            // Ensure the new folder name is not empty or null
            if (string.IsNullOrWhiteSpace(newFolderName))
            {
                Console.WriteLine("The new folder name is not valid.");
                return;
            }

            // Get the full path of the new folder by combining the parent directory with the new name
            string newFolderPath = Path.Combine(path1: Path.GetDirectoryName(currentFolderPath), path2: newFolderName);

            // Check if a folder with the new name already exists
            if (Directory.Exists(newFolderPath))
            {
                MessageManagement.ConsoleMessage("A folder with the new name already exists.", 3);
                return;
            }

            // Rename the folder
            try
            {
                Directory.Move(currentFolderPath, newFolderPath);
                MessageManagement.ConsoleMessage("Folder renamed successfully.", 2);
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage($"Error renaming the folder: {ex.Message}", 4);
            }
        }

        public static string[] FindFoldersByPartialName(string parentDirectory, string partialName)
        {
            if (!Directory.Exists(parentDirectory))
            {
                MessageManagement.ConsoleMessage(MessageManagement.DirNotEx, 4);
                return new string[0];
            }

            try
            {
                string[] matchingFolders = Directory.GetDirectories(parentDirectory, "*" + partialName + "*");
                return matchingFolders;
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage($"Error searching for folders: {ex.Message}", 4);
                return new string[0];
            }
        }

        public static async Task CopyFilesRecursively(DirectoryInfo source, DirectoryInfo destination)
        {
            if (!destination.Exists)
            {
                destination.Create();
            }

            foreach (FileInfo file in source.GetFiles())
            {
                string destinationFilePath = Path.Combine(destination.FullName, file.Name);
                file.CopyTo(destinationFilePath, true);
            }

            foreach (DirectoryInfo subDirectory in source.GetDirectories())
            {
                string destinationSubDirectoryPath = Path.Combine(destination.FullName, subDirectory.Name);
                await Task.Run(() => CopyFilesRecursively(subDirectory, new DirectoryInfo(destinationSubDirectoryPath)));
            }
        }

        public static Task DecompressZip(string zipFilePath, string extractPath)
        {
            if (!File.Exists(zipFilePath))
            {
                MessageManagement.ConsoleMessage("The .zip file does not exist.", 3);
                return Task.CompletedTask;
            }

            if (!Directory.Exists(extractPath))
            {
                MessageManagement.ConsoleMessage(MessageManagement.DirNotEx, 4);
                return Task.CompletedTask;
            }

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string entryOutputPath = Path.Combine(extractPath, entry.FullName);

                        if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                        {
                            Directory.CreateDirectory(entryOutputPath);
                        }
                        else
                        {
                            entry.ExtractToFile(entryOutputPath, true);
                        }
                    }
                }
                string extractedDirectoryName = Path.GetFileName(extractPath);
                MessageManagement.ConsoleMessage("Decompression completed.", 2);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage($"Error when decompress the .zip: {ex.Message}", 4);
            }
            return Task.CompletedTask;
        }

        public static void DeleteFiles(string folder, string[]? folderToExclude = null, string[]? fileToExclude = null)
        {
            try
            {
                string[] files = Directory.GetFiles(folder);

                foreach (string file in files)
                {
                    bool isExcluded = false;

                    if (folderToExclude != null)
                    {
                        foreach (string excludedFolder in folderToExclude)
                        {
                            if (file.StartsWith(Path.Combine(folder, excludedFolder)))
                            {
                                isExcluded = true;
                                break;
                            }
                        }
                    }

                    if (!isExcluded && fileToExclude != null)
                    {
                        if (Array.IndexOf(fileToExclude, Path.GetFileName(file)) != -1)
                        {
                            isExcluded = true;
                        }
                    }

                    if (!isExcluded)
                    {
                        File.Delete(file);
                        MessageManagement.ConsoleMessage($"The file {file} has been deleted.", 2);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage($"Error deleting files: {ex.Message}", 4);
            }
        }
    }
}
