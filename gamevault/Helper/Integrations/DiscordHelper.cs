using DiscordRPC;
using DiscordRPC.Logging;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.Helper
{
    internal class DiscordHelper
    {
        #region Singleton
        private static DiscordHelper instance = null;
        private static readonly object padlock = new object();

        public static DiscordHelper Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new DiscordHelper();
                    }
                    return instance;
                }
            }
        }
        #endregion
        private DiscordRpcClient client;
        private int currentGameId = -1;
        internal DiscordHelper()
        {
            try
            {
                client = new DiscordRpcClient(Encoding.UTF8.GetString(Convert.FromBase64String("MTMxMzIyNTUxNjUwOTMwMjgxNQ==")));
            }
            catch { }
        }
        internal void SyncGameWithDiscordPresence(List<int> trackedGameIds, Dictionary<int, string> installedGames)
        {
            try
            {
                if (!SettingsViewModel.Instance.SyncDiscordPresence || !SettingsViewModel.Instance.License.IsActive())
                    return;

                if (!client.IsInitialized)
                {
                    client.Initialize();
                }
                if (currentGameId != -1 && !trackedGameIds.Contains(currentGameId))
                {
                    client.ClearPresence();
                    currentGameId = -1;
                }
                if (trackedGameIds.Count <= 0)
                    return;

                KeyValuePair<int, string> firstGame = installedGames.First(g => g.Key == trackedGameIds.First());
                string gameTitle = Path.GetFileName(firstGame.Value.TrimEnd(Path.DirectorySeparatorChar));
                gameTitle = gameTitle.Replace($"({firstGame.Key})", "");
                client.SetPresence(new RichPresence()
                {
                    Details = gameTitle,
                    Buttons = new Button[] { new Button() { Label = "What's GameVault?", Url = "https://gamevau.lt/" } }
                });
                currentGameId = firstGame.Key;
            }
            catch { }
        }
    }
}
