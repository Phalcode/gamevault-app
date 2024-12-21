using gamevault.Models;
using gamevault.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace gamevault.Helper
{
    internal class SteamHelper
    {
        #region SteamShortcutUtilities
        public enum VdfMapItemType
        {
            Map = 0x00,
            String = 0x01,
            Number = 0x02,
            MapEnd = 0x08,
        }

        public class VdfMapItem
        {
            public VdfMapItemType Type { get; set; }
            public string Name { get; set; }
            public object Value { get; set; }
        }

        public class VdfMap : Dictionary<string, object> { }

        public static class VdfUtilities
        {
            public static VdfMap ReadVdf(byte[] buffer, int offset = 0)
            {
                using (var memoryStream = new MemoryStream(buffer))
                {
                    memoryStream.Seek(offset, SeekOrigin.Begin);
                    using (var reader = new BinaryReader(memoryStream))
                    {
                        return NextMap(reader);
                    }
                }
            }

            private static VdfMap NextMap(BinaryReader reader)
            {
                var map = new VdfMap();

                while (true)
                {
                    var mapItem = NextMapItem(reader);
                    if (mapItem.Type == VdfMapItemType.MapEnd)
                    {
                        break;
                    }

                    map[mapItem.Name] = mapItem.Value;
                }

                return map;
            }

            private static VdfMapItem NextMapItem(BinaryReader reader)
            {
                var typeByte = reader.ReadByte();

                if (typeByte == (byte)VdfMapItemType.MapEnd)
                {
                    return new VdfMapItem { Type = VdfMapItemType.MapEnd };
                }

                var name = ReadString(reader);
                object value = typeByte switch
                {
                    (byte)VdfMapItemType.Map => NextMap(reader),
                    (byte)VdfMapItemType.String => ReadString(reader),
                    (byte)VdfMapItemType.Number => reader.ReadUInt32(),
                    _ => throw new Exception($"Unexpected VdfMapItemType: {typeByte}")
                };

                return new VdfMapItem { Type = (VdfMapItemType)typeByte, Name = name, Value = value };
            }

            private static string ReadString(BinaryReader reader)
            {
                var bytes = new List<byte>();
                byte b;
                while ((b = reader.ReadByte()) != 0)
                {
                    bytes.Add(b);
                }
                return Encoding.UTF8.GetString(bytes.ToArray());
            }

            public static byte[] WriteVdf(VdfMap map)
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(memoryStream))
                    {
                        AddMap(map, writer);
                        return memoryStream.ToArray();
                    }
                }
            }

            private static void AddMap(VdfMap map, BinaryWriter writer)
            {
                foreach (var keyValuePair in map)
                {
                    if (keyValuePair.Value is int intValue)
                    {
                        writer.Write((byte)VdfMapItemType.Number);
                        AddString(keyValuePair.Key, writer);
                        writer.Write(intValue);
                    }
                    else if (keyValuePair.Value is uint uintValue)
                    {
                        writer.Write((byte)VdfMapItemType.Number);
                        AddString(keyValuePair.Key, writer);
                        writer.Write(uintValue);
                    }
                    else if (keyValuePair.Value is string stringValue)
                    {
                        writer.Write((byte)VdfMapItemType.String);
                        AddString(keyValuePair.Key, writer);
                        AddString(stringValue, writer);
                    }
                    else if (keyValuePair.Value is VdfMap nestedMap)
                    {
                        writer.Write((byte)VdfMapItemType.Map);
                        AddString(keyValuePair.Key, writer);
                        AddMap(nestedMap, writer);
                    }
                    else
                    {
                        string type = keyValuePair.Value.GetType().ToString();
                        throw new Exception($"Unsupported VDF value type for key '{keyValuePair.Key}'.");
                    }
                }

                writer.Write((byte)VdfMapItemType.MapEnd);
            }

            private static void AddString(string value, BinaryWriter writer)
            {
                if (value.Contains('\0'))
                {
                    throw new Exception("Strings in VDF files cannot have null characters ('\\0').");
                }

                writer.Write(Encoding.UTF8.GetBytes(value));
                writer.Write((byte)0);
            }

            public static string GetShortcutHash(string input)
            {
                uint crc32 = Crc32.Compute(input);
                long full64 = (crc32 | 0x80000000) | ((long)0x02000000 << 32);
                return full64.ToString();
            }

            public static string GetShortcutUrl(string appName, string exe)
            {
                return $"steam://rungameid/{GetShortcutHash(exe + appName)}";
            }
            private static int GenerateShortcutVDFAppId(string input)
            {
                // Create MD5 hash from the input string
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                    // Convert the first 4 bytes of the hash to a hex string
                    string seedHex = BitConverter.ToString(hash, 0, 4).Replace("-", "");
                    // Convert the hex string to an integer and modulo to keep it within signed 32-bit integer range
                    return -(int)(Convert.ToUInt32(seedHex, 16) % 1000000000);
                }
            }
            private static uint GenerateSteamShortID(int signedId)
            {
                return (uint)signedId & 0xFFFFFFFF;
            }
            public static uint GenerateSteamGridID(string input)
            {
                return GenerateSteamShortID(GenerateShortcutVDFAppId(input));
            }
        }

        public static class Crc32
        {
            private const uint Polynomial = 0x04C11DB7;
            private const uint DefaultSeed = 0xFFFFFFFF;
            private static readonly uint[] Table;

            static Crc32()
            {
                Table = new uint[256];
                for (uint i = 0; i < 256; i++)
                {
                    uint crc = i << 24;
                    for (int j = 0; j < 8; j++)
                    {
                        crc = (crc & 0x80000000) != 0 ? (crc << 1) ^ Polynomial : (crc << 1);
                    }
                    Table[i] = crc;
                }
            }

            public static uint Compute(string input, uint seed = DefaultSeed)
            {
                uint crc = seed;
                foreach (byte b in Encoding.UTF8.GetBytes(input))
                {
                    byte tableIndex = (byte)((crc >> 24) ^ b);
                    crc = (crc << 8) ^ Table[tableIndex];
                }
                return ~crc;
            }
        }
        #endregion

        internal static async Task SyncGamesWithSteamShortcuts(Dictionary<Game, string> games)
        {
            await Task.Run(async () =>
            {
                try
                {
                    if (!SettingsViewModel.Instance.License.IsActive())
                        return;

                    string shortcutsDirectory = GetMostRecentUserUserDirectory();
                    if (!Directory.Exists(shortcutsDirectory))
                        return;

                    EnsureBackup(shortcutsDirectory);//In case something went wrong.
                    VdfMap shortcutFileMap = null;
                    string shortcutFile = Path.Combine(shortcutsDirectory, "shortcuts.vdf");
                    List<string> steamGridImageIDsToRemove = null;
                    Dictionary<Game, uint> steamGridGamesToAdd = new Dictionary<Game, uint>();
                    if (File.Exists(shortcutFile))
                    {
                        try
                        {
                            //Try catch here, because if something goes wrong, the file is corrupted and will be overwritten in a clean way by the new games
                            shortcutFileMap = VdfUtilities.ReadVdf(File.ReadAllBytes(Path.Combine(shortcutsDirectory, "shortcuts.vdf")));
                            shortcutFileMap = TrimFirstOffsetEntry(shortcutFileMap);//Making sure to have a clean game list
                            shortcutFileMap = RemoveUninstalledGames(shortcutFileMap, games, out steamGridImageIDsToRemove);//Checks for gamevault games and compares them to the current installed game list. Extracts also the steam grid image ids for clean removal later on
                            games = TrimExistingGames(games, shortcutFileMap);//No need for double entries. So they will be removed
                        }
                        catch { }
                    }
                    else
                    {
                        File.Create(shortcutFile).Close();
                    }
                    shortcutFileMap = AddGamesToShortcuts(shortcutFileMap, games, shortcutsDirectory, steamGridGamesToAdd);

                    VdfMap root = new VdfMap();
                    root.Add("shortcuts", shortcutFileMap);//Adds back the first offset entry a vdf file needs to work

                    File.WriteAllBytes(shortcutFile, VdfUtilities.WriteVdf(root));
                    //The steam grid image is only processed after it has been written. Because if it did not work to manipulate the VDF file, there is no point in changing anything in the images
                    string steamGridDirectory = Path.Combine(shortcutsDirectory, "grid");
                    if (!Directory.Exists(steamGridDirectory))
                    {
                        Directory.CreateDirectory(steamGridDirectory);
                    }
                    if (steamGridImageIDsToRemove?.Count > 0)
                    {
                        foreach (string steamGridImageToRemove in steamGridImageIDsToRemove)
                        {
                            RemoveSteamGridImages(steamGridDirectory, steamGridImageToRemove);
                        }
                    }
                    foreach (KeyValuePair<Game, uint> steamGridGame in steamGridGamesToAdd)
                    {
                        uint steamGridID = steamGridGame.Value;
                        await CacheHelper.EnsureImageCacheForGame(steamGridGame.Key);
                        SetSteamGridImages(steamGridDirectory, steamGridGame.Key, steamGridID);
                    }
                }
                catch (Exception e)
                {
                }
            });
        }
        internal static void RemoveGameVaultGamesFromSteamShortcuts()
        {
            try
            {
                string shortcutsDirectory = GetMostRecentUserUserDirectory();
                if (!Directory.Exists(shortcutsDirectory))
                    return;


                string shortcutFile = Path.Combine(shortcutsDirectory, "shortcuts.vdf");
                VdfMap shortcutFileMap = VdfUtilities.ReadVdf(File.ReadAllBytes(shortcutFile));
                shortcutFileMap = TrimFirstOffsetEntry(shortcutFileMap);//Making sure to have a clean game list
                List<string> steamGridImageIDsToRemove = new List<string>();
                for (int count = 0; count < shortcutFileMap.Count; count++)
                {
                    string currentGameExe = ((VdfMap)shortcutFileMap.Values.ElementAt(count)).ElementAt(2).Value.ToString();
                    string currentGameTitle = ((VdfMap)shortcutFileMap.Values.ElementAt(count)).ElementAt(1).Value.ToString();
                    //Check for gamevault protocol
                    if (currentGameExe.Contains("gamevault://", StringComparison.OrdinalIgnoreCase))
                    {
                        steamGridImageIDsToRemove.Add(((VdfMap)shortcutFileMap.Values.ElementAt(count)).ElementAt(0).Value.ToString());
                        shortcutFileMap.Remove(shortcutFileMap.ElementAt(count).Key);
                        count--;
                    }
                }
                VdfMap root = new VdfMap();
                root.Add("shortcuts", shortcutFileMap);//Adds back the first offset entry a vdf file needs to work

                File.WriteAllBytes(shortcutFile, VdfUtilities.WriteVdf(root));

                string steamGridDirectory = Path.Combine(shortcutsDirectory, "grid");
                if (steamGridImageIDsToRemove?.Count > 0)
                {
                    foreach (string steamGridImageToRemove in steamGridImageIDsToRemove)
                    {
                        RemoveSteamGridImages(steamGridDirectory, steamGridImageToRemove);
                    }
                }
            }
            catch { }
        }
        private static void RemoveSteamGridImages(string steamGridDirectory, string ID)
        {
            if (File.Exists(Path.Combine(steamGridDirectory, $"{ID}p.png")))
            {
                File.Delete(Path.Combine(steamGridDirectory, $"{ID}p.png"));
            }
            if (File.Exists(Path.Combine(steamGridDirectory, $"{ID}_hero.png")))
            {
                File.Delete(Path.Combine(steamGridDirectory, $"{ID}_hero.png"));
            }
        }
        private static void SetSteamGridImages(string steamGridDirectory, Game game, uint steamGridID)
        {
            Dictionary<string, string> imageCache = CacheHelper.GetImageCacheForGame(game);
            if (File.Exists(imageCache["gbox"]) && !File.Exists(Path.Combine(steamGridDirectory, $"{steamGridID}p.png")))
            {
                File.Copy(imageCache["gbox"], Path.Combine(steamGridDirectory, $"{steamGridID}p.png"));
            }
            if (File.Exists(imageCache["gbg"]) && !File.Exists(Path.Combine(steamGridDirectory, $"{steamGridID}_hero.png")))
            {
                File.Copy(imageCache["gbg"], Path.Combine(steamGridDirectory, $"{steamGridID}_hero.png"));
            }
        }
        private static VdfMap RemoveUninstalledGames(VdfMap shortcutFileMap, Dictionary<Game, string> games, out List<string> steamGridImagesToRemove)
        {
            steamGridImagesToRemove = new List<string>();
            for (int count = 0; count < shortcutFileMap.Count; count++)
            {
                string currentGameExe = ((VdfMap)shortcutFileMap.Values.ElementAt(count)).ElementAt(2).Value.ToString();
                string currentGameTitle = ((VdfMap)shortcutFileMap.Values.ElementAt(count)).ElementAt(1).Value.ToString();
                //Check for gamevault protocol
                if (currentGameExe!.Contains("gamevault://", StringComparison.OrdinalIgnoreCase) && !games.Keys.Any(g => g?.Title == currentGameTitle))
                {
                    steamGridImagesToRemove.Add(((VdfMap)shortcutFileMap.Values.ElementAt(count)).ElementAt(0).Value.ToString());
                    shortcutFileMap.Remove(shortcutFileMap.ElementAt(count).Key);
                    count--;
                }
            }
            return shortcutFileMap;
        }
        private static VdfMap AddGamesToShortcuts(VdfMap shortcutFileMap, Dictionary<Game, string> games, string shortcutDir, Dictionary<Game, uint> steamGridGamesToAdd)
        {
            if (shortcutFileMap == null)
            {
                shortcutFileMap = new VdfMap();
            }
            int indexCount = shortcutFileMap.Count;
            foreach (KeyValuePair<Game, string> game in games)
            {
                VdfMap newGameMap = GenerateVdfShortcutEntryFromGame(game.Key, shortcutDir, steamGridGamesToAdd);
                shortcutFileMap.Add(indexCount.ToString(), newGameMap);
                indexCount++;
            }
            return shortcutFileMap;
        }
        private static VdfMap GenerateVdfShortcutEntryFromGame(Game game, string shortcutDir, Dictionary<Game, uint> steamGridGamesToAdd)
        {
            string idInput = game.Title + game.ID;
            uint steamGridId = VdfUtilities.GenerateSteamGridID(idInput);
            steamGridGamesToAdd.Add(game, steamGridId);
            string steamGridIconPath = Path.Combine(shortcutDir, "grid", $"{steamGridId}p.png");
            string launchUrl = $"gamevault://start?gameid={game.ID}";
            var newGame = new VdfMap
{
    { "appid", steamGridId },
    { "AppName", game.Title },
    { "Exe", launchUrl },
    { "StartDir","" },
    { "icon", steamGridIconPath },
    { "ShortcutPath", "" },
    { "LaunchOptions", "" },
    { "IsHidden", 0 },
    { "AllowDesktopConfig", 1 },
    { "AllowOverlay", 1 },
    { "OpenVR", 0 },
    { "Devkit", 0 },
    { "DevkitGameID", "" },
    { "DevkitOverrideAppID", 0 },
    { "LastPlayTime", 0 },
    { "FlatpakAppID", "" },
    { "tags", new VdfMap() } // Representing tags as an empty nested map
};
            return newGame;
        }
        private static VdfMap TrimFirstOffsetEntry(VdfMap shortcutFileMap)
        {
            if (shortcutFileMap.FirstOrDefault().Key == "shortcuts")
            {
                shortcutFileMap = shortcutFileMap.Values.First() as VdfMap;
            }
            return shortcutFileMap;
        }
        private static Dictionary<Game, string> TrimExistingGames(Dictionary<Game, string> games, VdfMap shortcutFileMap)
        {
            for (int count = 0; count < shortcutFileMap.Values.Count; count++)
            {
                string currentGameExe = ((VdfMap)shortcutFileMap.Values.ElementAt(count)).ElementAt(2).Value.ToString();
                if (currentGameExe!.Contains("gamevault://", StringComparison.OrdinalIgnoreCase))
                {
                    string shortcutAppName = ((VdfMap)shortcutFileMap.Values.ElementAt(count)).ElementAt(1).Value as string;
                    KeyValuePair<Game, string> foundGame = games.Where(g => g.Key.Title == shortcutAppName).FirstOrDefault();
                    if (foundGame.Key != null)
                    {
                        games.Remove(foundGame.Key);
                    }
                }
            }
            return games;
        }
        private static void EnsureBackup(string shortcutsDirectory)
        {
            string backupFile = Path.Combine(shortcutsDirectory, "shortcuts.vdf.backup");
            string shortcutFile = Path.Combine(shortcutsDirectory, "shortcuts.vdf");
            if (!File.Exists(backupFile) && File.Exists(shortcutFile))
            {
                File.Copy(shortcutFile, backupFile);
            }
        }
        public static void RestoreBackup()
        {
            string shortcutsDirectory = GetMostRecentUserUserDirectory();
            if (!Directory.Exists(shortcutsDirectory))
                return;

            string shortcutFile = Path.Combine(shortcutsDirectory, "shortcuts.vdf");
            string backupFile = Path.Combine(shortcutsDirectory, "shortcuts.vdf.backup");
            if (File.Exists(backupFile))
            {
                if (File.Exists(shortcutFile))
                { File.Delete(shortcutFile); }
                File.Copy(backupFile, shortcutFile);
            }
        }
        private static string GetMostRecentUserUserDirectory()
        {
            try
            {
                RegistryKey? rk = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Valve\\Steam", true);
                if (rk == null)
                {
                    rk = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Valve\\Steam", true);//32 bit version
                }
                string steamInstallDir = (string)rk.GetValue("InstallPath");
                string userId = GetMostRecentUserID(steamInstallDir);
                string userDir = Path.Combine(steamInstallDir, "userdata", userId, "config");
                return userDir;
            }
            catch { return ""; }
        }
        private static string GetMostRecentUserID(string steamBaseDir)
        {
            string steamid = null;
            string steamConfigFile = Path.Combine(steamBaseDir, "config", "loginusers.vdf");

            if (!File.Exists(steamConfigFile))
            {
                throw new Exception("Couldn't find logged in user");
            }

            var lines = File.ReadAllLines(steamConfigFile);

            foreach (var line in lines)
            {
                if (line.Contains("7656119") && !line.Contains("PersonaName"))
                {
                    // Found line with id
                    var match = Regex.Match(line, "7656119[0-9]+");
                    if (match.Success)
                    {
                        steamid = match.Value;
                    }
                }
                else if ((line.Contains("mostrecent") || line.Contains("MostRecent")) && line.Contains("\"1\""))
                {
                    // Found line mostrecent
                    if (steamid != null)
                    {
                        ulong steamidLongLong = ulong.Parse(steamid) - 76561197960265728UL;
                        steamid = steamidLongLong.ToString();
                        break;
                    }
                }
            }

            if (steamid == null)
            {
                throw new Exception("Couldn't find logged in user");
            }

            return steamid;
        }
    }
}