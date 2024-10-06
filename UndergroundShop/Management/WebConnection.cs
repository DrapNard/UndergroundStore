using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace UndergroundShop.Management
{
    internal class WebConnection
    {
        private static readonly HttpClient _httpClient;

        static WebConnection()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = ValidateServerCertificate,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30), // Set a timeout to avoid hanging
                DefaultRequestVersion = HttpVersion.Version20 // Use HTTP/2 by default
            };
        }

        public static async Task<string> LoadJsonFromWeb(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                if (uri.Scheme != Uri.UriSchemeHttps)
                {
                    throw new ArgumentException("Only HTTPS URLs are allowed.");
                }

                var response = await _httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode(); // Throws exception if status code is not success
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                MessageManagement.ConsoleMessage($"Error fetching JSON from server: {url}, Error: {ex.Message} \n", 3);
                return null;
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage($"Unexpected error: {ex.Message} \n", 4);
                return null;
            }
        }

        private static bool ValidateServerCertificate(HttpRequestMessage requestMessage, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                // Certificate is valid
                return true;
            }

            // Log or handle invalid certificates here
            MessageManagement.ConsoleMessage($"SSL certificate error: {sslPolicyErrors}", 4);
            return false; // Reject invalid certificates
        }
    }
}
