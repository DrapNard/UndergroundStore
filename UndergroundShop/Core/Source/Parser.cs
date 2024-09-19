using Newtonsoft.Json;
using System.Threading.Tasks;
using UndergroundShop.Management;

namespace UndergroundShop.Core.Source
{
    internal class Parser
    {
        public static GameList ParseGamesJson(string json)
        {
            var gameList = JsonConvert.DeserializeObject<GameList>(json);

            // Handle potential parsing errors...

            foreach (var gameRef in gameList.Games)
            {
                try
                {
                    dynamic gameData = JsonConvert.DeserializeObject(gameRef.ToString());
                    Task.Run( () => ParseIndividualGameJson(WebConnection.LoadJsonFromWeb(gameData)));
                }
                catch (JsonSerializationException ex)
                {
                    MessageManagement.ConsoleMessage($"Error parsing JSON from GameList | Error: {ex} \n", 3);
                }
            }

            return gameList;
        }

        private static void ParseIndividualGameJson(dynamic gameData)
        {
            var gameInfo = gameData.GameInfo;
            var functionalities = gameData.Functionalities;
            var languageSupport = gameData.LanguageSupport;
            var socialLink = gameData.SocialLink;
            var picture = gameData.Picture;
            var store = gameData.Store;
        }
    }
}
