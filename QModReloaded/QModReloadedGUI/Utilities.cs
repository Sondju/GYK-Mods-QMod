using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gameloop.Vdf;
using Gameloop.Vdf.JsonConverter;
using Microsoft.Win32;

namespace QModReloadedGUI
{
    internal static class Utilities
    {

        public static void WriteLog(string message, string gameLocation)
        {
            using var streamWriter = new StreamWriter(Path.Combine(gameLocation, "qmod_reloaded_log.txt"),
                true);
            streamWriter.WriteLine(message);
        }

        internal static (string location,bool found) GetGameDirectory()
        {
            try
            {
                var gameDirectories = new List<string>();
                string gd = null;
                using var registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Valve\\Steam");
                var value = registryKey?.GetValue("InstallPath");
                var dir = VdfConvert.Deserialize(File.ReadAllText(value + "\\steamapps\\libraryfolders.vdf"));
                var json = dir.ToJson();
                foreach (var child in json.Value.Children())
                {
                    foreach (var child2 in child.Children())
                    {
                        gd = child2.Children().ElementAt(0).ToString();
                        gd = gd.Substring(9, gd.Length - 10);
                        gd = gd.Replace("\\\\","\\").Trim();
                        gameDirectories.Add(gd);
                    }
                }
                if (gd != null)
                    foreach (var gdFile in gameDirectories.Select(gameDirectory => new FileInfo(gameDirectory + "\\steamapps\\common\\Graveyard Keeper\\Graveyard Keeper.exe")).Where(gdFile => gdFile.Exists))
                    {
                        return (gdFile.Directory?.ToString(), true);
                    }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return (null, false);
        }


    }
}
