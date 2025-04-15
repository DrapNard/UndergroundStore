using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace UndergroundShop.Management
{
    /// <summary>
    /// Provides utility methods for managing secure web connections and fetching data from web servers.
    /// </summary>
    internal class WebConnection
    {
        /// <summary>
        /// The static <see cref="HttpClient"/> instance used for making web requests.
        /// </summary>
        private static readonly HttpClient _httpClient;

        /// <summary>
        /// Static constructor to initialize the <see cref="HttpClient"/> with custom server certificate validation.
        /// </summary>
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

        /// <summary>
        /// Fetches JSON data from the specified HTTPS URL.
        /// </summary>
        /// <param name="url">The HTTPS URL to fetch JSON data from.</param>
        /// <returns>A JSON string if successful; otherwise, null.</returns>
        /// <exception cref="ArgumentException">Thrown if the URL is not HTTPS.</exception>
        public static async Task<string> LoadJsonFromWeb(string url)
        {
            try
            {
                Uri uri = new(url);
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
                return string.Empty;
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage($"Unexpected error: {ex.Message} \n", 4);
                return string.Empty;
            }
        }

        /// <summary>
        /// Validates the server certificate for HTTPS requests.
        /// </summary>
        /// <param name="requestMessage">The HTTP request message being sent.</param>
        /// <param name="certificate">The server certificate presented during the SSL handshake.</param>
        /// <param name="chain">The certificate chain used to validate the server certificate.</param>
        /// <param name="sslPolicyErrors">The SSL policy errors encountered during validation.</param>
        /// <returns><c>true</c> if the certificate is valid; otherwise, <c>false</c>.</returns>
        private static bool ValidateServerCertificate(HttpRequestMessage requestMessage, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
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
