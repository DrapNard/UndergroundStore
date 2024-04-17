using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace UndergroundShop.Management
{
    public class Downloader
    {
        static readonly HttpClient client = new();

        private readonly string url;

        public Downloader(string url)
        {
            this.url = url;
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
                    Console.WriteLine($"File Length: {contentLength} bytes");
                    return contentLength;
                }
                else
                {
                    Console.WriteLine($"Failed to get file length: {response.StatusCode}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
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
                Console.WriteLine(MessageManagement.DirNotEx);
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

        public static string[] FindFoldersByPartialName(string parentDirectory, string partialName)
        {
            if (!Directory.Exists(parentDirectory))
            {
                Console.WriteLine(MessageManagement.DirNotEx);
                return new string[0];
            }

            try
            {
                string[] matchingFolders = Directory.GetDirectories(parentDirectory, "*" + partialName + "*");
                return matchingFolders;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching for folders: {ex.Message}");
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
                Console.WriteLine("The .zip file does not exist.");
                return Task.CompletedTask;
            }

            if (!Directory.Exists(extractPath))
            {
                Console.WriteLine(MessageManagement.DirNotEx);
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
                Console.WriteLine("Decompression completed.");

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
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
                        Console.WriteLine($"The file {file} has been deleted.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting files: {ex.Message}");
            }
        }
    }
}
