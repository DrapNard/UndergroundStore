using Newtonsoft.Json;
using System;
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
            if (gameList?.Games == null)
            {
                MessageManagement.ConsoleMessage("Error: Failed to parse game list or games collection is null", 3);
                return new GameList { Games = [], Info = [] };
            }

            foreach (var gameRef in gameList.Games)
            {
                if (gameRef == null) continue;
                
                try
                {
                    string gameRefString = gameRef?.ToString() ?? string.Empty;
                    if (string.IsNullOrEmpty(gameRefString)) continue;
                    
                    dynamic? gameData = JsonConvert.DeserializeObject(gameRefString);
                    if (gameData != null)
                    {
                        Task.Run(async () => {
                            string? jsonData = await WebConnection.LoadJsonFromWeb(gameData?.ToString() ?? "");
                            if (!string.IsNullOrEmpty(jsonData))
                            {
                                ParseIndividualGameJson(JsonConvert.DeserializeObject(jsonData));
                            }
                        });
                    }
                }
                catch (JsonSerializationException ex)
                {
                    MessageManagement.ConsoleMessage($"Error parsing JSON from GameList | Error: {ex} \n", 3);
                }
            }

            return gameList;
        }

        private static void ParseIndividualGameJson(dynamic? gameData)
        {
            if (gameData == null)
            {
                MessageManagement.ConsoleMessage("Error: Game data is null", 3);
                return;
            }

            try
            {
                var gameInfo = gameData.GameInfo;
                var functionalities = gameData.Functionalities;
                var languageSupport = gameData.LanguageSupport;
                var socialLink = gameData.SocialLink;
                var picture = gameData.Picture;
                var store = gameData.Store;

                // Traitement des données du jeu...
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage($"Error processing game data: {ex.Message}", 3);
            }
        }
    }
}
