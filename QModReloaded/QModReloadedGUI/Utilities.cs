using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using Microsoft.Win32;

namespace QModReloadedGUI
{
    internal static class Utilities
    {

        public static string CalculateMd5(string file)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(file);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static void WriteLog(string message, string gameLocation)
        {
            using var streamWriter = new StreamWriter(Path.Combine(gameLocation, "qmod_reloaded_log.txt"),
                true);
            streamWriter.WriteLine(message);
        }

        public static void ReadLibraryVdf()
        {
            using var registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Valve\\Steam");
            var value = registryKey?.GetValue("InstallPath");
            var vdfFile = File.ReadAllLines(value + "\\steamapps\\libraryfolders.vdf");
            List<string> libList = new List<string>();
            foreach (var line in vdfFile)
            {
                if (line.Contains("path"))
                {
                    foreach (var s in line.Split('"'))
                    {
                        if (string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s)) continue;
                        var t = s.Replace("\\\\", "\\").Trim();
                       if (s.Contains("path")) continue;
                        libList.Add(t);
                       Console.WriteLine(t);
                        
                    }
                }
            }
        }

        internal static (string location,bool found) GetGameDirectory()
        {
            try
            {
                //  string gd = null;
                using var registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Valve\\Steam");
                var value = registryKey?.GetValue("InstallPath");
                var vdfFile = File.ReadAllLines(value + "\\steamapps\\libraryfolders.vdf");
                var gameDirectories = (from line in vdfFile where line.Contains("path") from s in line.Split('"') where !string.IsNullOrEmpty(s) && !string.IsNullOrWhiteSpace(s) let t = s.Replace("\\\\", "\\").Trim() where !s.Contains("path") select t).ToList();
                foreach (var gdFile in gameDirectories.Select(gameDirectory => new FileInfo(gameDirectory + "\\steamapps\\common\\Graveyard Keeper\\Graveyard Keeper.exe")).Where(gdFile => gdFile.Exists))
                {
                    return (gdFile.Directory?.ToString(), true);
                }
            }
            catch (Exception)
            {
                //Console.WriteLine(ex.Message);
            }
            return (null, false);
        }


    }
}
