using System.Net.Http;
using System.Threading.Tasks;

namespace UndergroundShop.Management
{
    internal class WebConnection
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> LoadJsonFromWeb(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                MessageManagement.ConsoleMessage($"Error fetching JSON from server: {url}, Error: {ex} \n", 3);
                return null;
            }
        }
    }
}
