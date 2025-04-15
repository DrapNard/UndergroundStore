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

        /// <summary>
        /// Static constructor to initialize the HTTP client with custom certificate validation.
        /// </summary>
        static WebFile()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                    {
                        return true; // Certificate is valid
                    }

                    if (sslPolicyErrors.HasFlag(System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors))
                    {
                        MessageManagement.ConsoleMessage("3ss0001", 4);
                    }
                    else if (sslPolicyErrors.HasFlag(System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch))
                    {
                        MessageManagement.ConsoleMessage("3ss0002", 4);
                    }
                    else
                    {
                        MessageManagement.ConsoleMessage("3ss0003", 4);
                    }

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

        /// <summary>
        /// Initializes a new instance of the <see cref="WebFile"/> class.
        /// </summary>
        /// <param name="url">The URL of the web file to manage.</param>
        /// <exception cref="ArgumentException">Thrown if the URL is not HTTPS.</exception>
        public WebFile(string url)
        {
            uri = new Uri(url);

            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                MessageManagement.ConsoleMessage("1fr0003", 4);
            }
        }

        /// <summary>
        /// Downloads the file from the specified URL to the given path.
        /// </summary>
        /// <param name="path">The destination directory to save the downloaded file.</param>
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

                    MessageManagement.ConsoleMessage("DownloadedFile", 2, filePath);
                }
                else
                {
                    MessageManagement.ConsoleMessage("0fc0002", 4);
                }
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage("GenericError", 4, ex.Message);
            }
        }

        /// <summary>
        /// Updates the progress of the download.
        /// </summary>
        /// <param name="percentage">The percentage of the download completed.</param>
        /// <param name="downloadedBytes">The total bytes downloaded so far.</param>
        private void UpdateDownloadProgress(double percentage, long downloadedBytes)
        {
            MessageManagement.ConsoleMessage("DownloadedFile", 1, $"{percentage:F2}% ({downloadedBytes} bytes downloaded)");
        }

        /// <summary>
        /// Extracts the file name from the given URI.
        /// </summary>
        /// <param name="uri">The URI of the file.</param>
        /// <returns>The file name extracted from the URI.</returns>
        private static string GetFileNameFromUrl(Uri uri)
        {
            return Path.GetFileName(uri.LocalPath);
        }

        /// <summary>
        /// Gets the length of the file from the specified URL.
        /// </summary>
        /// <returns>The length of the file in bytes, or 0 if an error occurs.</returns>
        public async Task<long> GetFileLengthAsync()
        {
            try
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));

                if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength.HasValue)
                {
                    var contentLength = response.Content.Headers.ContentLength.Value;
                    MessageManagement.ConsoleMessage("2fc0005", 2, contentLength);
                    return contentLength;
                }
                else
                {
                    MessageManagement.ConsoleMessage("0fc0002", 4);
                    return 0;
                }
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage("GenericError", 4, ex.Message);
                return 0;
            }
        }
    }

    internal class FileManagement
    {
        /// <summary>
        /// Renames a folder to a new name.
        /// </summary>
        /// <param name="currentFolderPath">The current folder path.</param>
        /// <param name="newFolderName">The new folder name.</param>
        public static void RenameFolder(string currentFolderPath, string newFolderName)
        {
            if (!Directory.Exists(currentFolderPath))
            {
                MessageManagement.ConsoleMessage("0fc0001", 4);
                return;
            }

            if (string.IsNullOrWhiteSpace(newFolderName))
            {
                MessageManagement.ConsoleMessage("1fr0003", 4);
                return;
            }

            string newFolderPath = Path.Combine(Path.GetDirectoryName(currentFolderPath) ?? string.Empty, newFolderName);

            if (Directory.Exists(newFolderPath))
            {
                MessageManagement.ConsoleMessage("1fr0004", 3);
                return;
            }

            try
            {
                Directory.Move(currentFolderPath, newFolderPath);
                MessageManagement.ConsoleMessage("FolderRenamed", 2, newFolderName);
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage("ErrorRenamingFolder", 4, ex.Message);
            }
        }

        /// <summary>
        /// Finds folders matching a partial name within a parent directory.
        /// </summary>
        /// <param name="parentDirectory">The parent directory to search in.</param>
        /// <param name="partialName">The partial name to search for.</param>
        /// <returns>An array of matching folder paths.</returns>
        public static string[] FindFoldersByPartialName(string parentDirectory, string partialName)
        {
            if (!Directory.Exists(parentDirectory))
            {
                MessageManagement.ConsoleMessage("0fc0001", 4);
                return Array.Empty<string>();
            }

            try
            {
                string[] matchingFolders = Directory.GetDirectories(parentDirectory, "*" + partialName + "*");
                return matchingFolders;
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage("GenericError", 4, ex.Message);
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Copies files and subdirectories from a source directory to a destination directory recursively.
        /// </summary>
        /// <param name="source">The source directory.</param>
        /// <param name="destination">The destination directory.</param>
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

        /// <summary>
        /// Decompresses a .zip file to the specified extraction path.
        /// </summary>
        /// <param name="zipFilePath">The path to the .zip file.</param>
        /// <param name="extractPath">The destination directory for extraction.</param>
        public static Task DecompressZip(string zipFilePath, string extractPath)
        {
            if (!File.Exists(zipFilePath))
            {
                MessageManagement.ConsoleMessage("0fc0002", 3);
                return Task.CompletedTask;
            }

            if (!Directory.Exists(extractPath))
            {
                MessageManagement.ConsoleMessage("0fc0001", 4);
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
                MessageManagement.ConsoleMessage("DecompressionCompleted", 2);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage("2fc0005", 4, ex.Message);
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Deletes files in a folder while excluding specific files or subdirectories.
        /// </summary>
        /// <param name="folder">The folder to delete files from.</param>
        /// <param name="folderToExclude">Subdirectories to exclude from deletion.</param>
        /// <param name="fileToExclude">Files to exclude from deletion.</param>
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
                        MessageManagement.ConsoleMessage("FileDeleted", 2, file);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage("GenericError", 4, ex.Message);
            }
        }
    }
}
