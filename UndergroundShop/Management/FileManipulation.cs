using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace UndergroundShop.Management
{
    public class WebFile
    {
        private static readonly HttpClient client;
        private readonly Uri uri;

        static WebFile()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                {
                    // Log and verify SSL certificate here
                    if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                    {
                        return true; // Certificate is valid
                    }

                    // Log or handle invalid certificates here
                    MessageManagement.ConsoleMessage($"SSL certificate error: {sslPolicyErrors}", 4);
                    return false; // Reject invalid certificates
                },
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
            };

            client = new HttpClient(handler)
            {
                DefaultRequestVersion = HttpVersion.Version20,
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public WebFile(string url)
        {
            uri = new Uri(url);

            if (uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Only HTTPS protocol is allowed.");
        }

        public async Task DownloadAsync(string path)
        {
            try
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 6.1; WOW64; rv:40.0) Gecko/20100101 Firefox/40.1");

                using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                {
                    var fileName = GetFileNameFromUrl(uri);
                    var filePath = Path.Combine(path, fileName);

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    await using var contentStream = await response.Content.ReadAsStreamAsync();
                    await using var fileStream = new FileStream(filePath, FileMode.CreateNew);

                    var buffer = new byte[8192];
                    long downloadedBytes = 0;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        downloadedBytes += bytesRead;
                        await fileStream.WriteAsync(buffer, 0, bytesRead);

                        if (totalBytes != -1)
                        {
                            double percentage = (double)downloadedBytes / totalBytes * 100;
                            UpdateDownloadProgress(percentage, downloadedBytes);
                        }
                    }

                    MessageManagement.ConsoleMessage($"Downloaded file to: {filePath}", 2);
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
            MessageManagement.ConsoleMessage($"Download progress: {percentage:F2}% ({downloadedBytes} bytes downloaded)", 1);
        }

        private static string GetFileNameFromUrl(Uri uri)
        {
            return Path.GetFileName(uri.LocalPath);
        }

        public async Task<long> GetFileLengthAsync()
        {
            try
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 6.1; WOW64; rv:40.0) Gecko/20100101 Firefox/40.1");

                using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));

                if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength.HasValue)
                {
                    var contentLength = response.Content.Headers.ContentLength.Value;
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
