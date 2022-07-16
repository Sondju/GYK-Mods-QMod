using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Helper
{
    public static class Tools
    {
        internal static readonly List<string> LoadedMods = new();

        public static bool IsModLoaded(string mod)
        {
            return LoadedMods.Contains(mod);
        }

        public static void Log(string caller, string message, bool error = false)
        {
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
            if (error)
            {
                Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
                Debug.LogError($"[{caller}][ERROR]: {message}");
                return;
            }
            Debug.LogError($"[{caller}]: {message}");
        }
    }

    public static class MainPatcher
    {
        private const string DisablePath = "./QMods/disable";
        private static bool _disableMods;

        public static void Patch()
        {
            try
            {
                var harmony = new Harmony("p1xel8ted.GraveyardKeeper.QModHelper");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                _disableMods = File.Exists(DisablePath);
                if (_disableMods)
                {
                    File.Delete(DisablePath);
                }
            }
            catch (Exception ex)
            {
                Tools.Log("QModHelper", $"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
            }
        }

        [HarmonyPatch(typeof(MainMenuGUI), nameof(MainMenuGUI.Open))]
        public static class MainMenuGuiOpenPatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                try
                {
                    var mods = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => a.Location.ToLowerInvariant().Contains("qmods"));
                    foreach (var m in mods)
                    {
                        File.AppendAllText("./qmods/loaded.assemblie.txt", $"Location: {m.Location}, Name: {m.FullName}\n");
                    }
                    Tools.LoadedMods.Clear();
                    foreach (var mod in mods)
                    {
                        var modInfo = FileVersionInfo.GetVersionInfo(mod.Location);
                        if (!string.IsNullOrEmpty(modInfo.Comments))
                        {
                            Tools.LoadedMods.Add(modInfo.Comments);
                        }
                    }
                }
                catch (Exception)
                {
                   //
                }
            }

            [HarmonyPostfix]
            public static void Postfix(ref MainMenuGUI __instance)
            {
                if (__instance == null) return;

                foreach (var comp in __instance.GetComponentsInChildren<UILabel>()
                             .Where(x => x.name.Contains("credits")))
                {
                    comp.text = "[F7B000]QMod Reloaded[-] by [F7B000]p1xel8ted[-]\r\ngame by: [F7B000]Lazy Bear Games[-]\r\npublished by: [F7B000]tinyBuild[-]";
                    comp.overflowMethod = UILabel.Overflow.ResizeFreely;
                    comp.multiLine = true;
                    comp.MakePixelPerfect();
                }

                foreach (var comp in __instance.GetComponentsInChildren<UILabel>()
                             .Where(x => x.name.Contains("ver txt")))
                {
                    if (_disableMods)
                    {
                        comp.text += ", [F7B000] QMod Reloaded[-] [F70000]Disabled[-]";
                    }
                    else
                    {
                        comp.text += ", [F7B000] QMod Reloaded[-] [2BFF00]Enabled[-]";
                    }
                    comp.overflowMethod = UILabel.Overflow.ResizeFreely;
                    comp.multiLine = true;
                    comp.MakePixelPerfect();
                }
            }
        }
    }
}